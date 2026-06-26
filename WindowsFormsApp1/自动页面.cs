using MvCamCtrl.NET;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 自动页面 : Form
    {
        private const int 最大相机数量 = 2;
        private const string 默认模型路径 = @"E:\source\DEIM\checkpoints\deim_dfine_hgnetv2_n_coco_160e.onnx";

        public event Action<int, string> LogAdded;
        private readonly List<MVS变量> 相机列表 = new List<MVS变量>();
        private readonly object 计数锁 = new object();
        private readonly object 推理锁 = new object();
        private readonly halcon[] 图像显示控件;
        private volatile bool 检测中;
        private DeimOnnxDetector 检测器;

        public int[] 拍照总数 = new int[最大相机数量];
        public int[] OK = new int[最大相机数量];
        public int[] NG = new int[最大相机数量];

        public 自动页面()
        {
            InitializeComponent();
            图像显示控件 = new[] { halcon1, halcon2, halcon3, halcon4 };
            button1.Text = "开始检测";
            button1.BackColor = Color.IndianRed;
            button1.ForeColor = Color.White;

            int 相机数量 = 1;
            int.TryParse(Properties.Settings.Default.相机数量设置, out 相机数量);
            调整控件布局(Math.Max(1, Math.Min(最大相机数量, 相机数量)));
        }

        public void 深度学习数据上传()
        {
            string modelPath = Environment.GetEnvironmentVariable("DEIM_ONNX_MODEL");
            if (string.IsNullOrWhiteSpace(modelPath) &&
                File.Exists(Properties.Settings.Default.相机1hdl文件路径) &&
                string.Equals(Path.GetExtension(Properties.Settings.Default.相机1hdl文件路径), ".onnx", StringComparison.OrdinalIgnoreCase))
                modelPath = Properties.Settings.Default.相机1hdl文件路径;
            if (string.IsNullOrWhiteSpace(modelPath))
                modelPath = 默认模型路径;

            try
            {
                检测器?.Dispose();
                检测器 = new DeimOnnxDetector(modelPath, 0.4f);
                LogAdded?.Invoke(1, "DEIM ONNX模型加载成功：" + modelPath);
            }
            catch (Exception ex)
            {
                检测器 = null;
                LogAdded?.Invoke(0, "DEIM ONNX模型加载失败：" + ex.Message);
            }
        }

        public void 电脑设备连接()
        {
            LogAdded?.Invoke(检测器 == null ? 0 : 1,
                检测器 == null ? "ONNX检测器尚未就绪。" : "ONNX Runtime CPU推理已就绪。");
        }

        public DetectionResult 检测图片文件(string imagePath)
        {
            if (检测器 == null)
                深度学习数据上传();
            if (检测器 == null)
                throw new InvalidOperationException("DEIM ONNX模型未加载。");

            using (Mat image = OpenCvImageHelper.LoadImage(imagePath))
            {
                lock (推理锁)
                    return 检测器.Detect(image);
            }
        }

        public void 相机连接()
        {
            try
            {
                释放已连接相机();
                if (!int.TryParse(Properties.Settings.Default.相机数量设置, out int 相机数量) ||
                    相机数量 < 1 || 相机数量 > 最大相机数量)
                    throw new InvalidOperationException("相机数量必须在1到4之间。");

                var 设备列表 = new MyCamera.MV_CC_DEVICE_INFO_LIST();
                int 枚举结果 = MyCamera.MV_CC_EnumDevices_NET(
                    MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref 设备列表);
                if (枚举结果 != 0)
                    throw new InvalidOperationException($"枚举相机失败，错误码：0x{枚举结果:X8}");
                if (设备列表.nDeviceNum < 相机数量)
                    throw new InvalidOperationException($"相机数量不足：配置{相机数量}台，发现{设备列表.nDeviceNum}台。");

                for (int i = 0; i < 相机数量; i++)
                {
                    var 设备信息 = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                        设备列表.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                    var 相机 = new MVS变量(i);

                    int result = 相机.打开相机(ref 设备信息);
                    if (result != 0)
                    {
                        相机.关闭相机();
                        throw new InvalidOperationException($"相机{i + 1}打开失败，错误码：0x{result:X8}");
                    }

                    result = 相机.设置曝光增益();
                    if (result != 0)
                    {
                        相机.关闭相机();
                        throw new InvalidOperationException($"相机{i + 1}曝光或增益设置失败，错误码：0x{result:X8}");
                    }

                    相机.图像已到达 += 相机图像已到达;
                    result = 相机.开始采集(true);
                    if (result != 0)
                    {
                        相机.图像已到达 -= 相机图像已到达;
                        相机.关闭相机();
                        throw new InvalidOperationException($"相机{i + 1}硬触发采集失败，错误码：0x{result:X8}");
                    }
                    相机列表.Add(相机);
                }

                相机变量.CameraList = 相机列表;
                LogAdded?.Invoke(1, $"已连接{相机数量}台相机，正在等待硬触发。");
            }
            catch (Exception ex)
            {
                释放已连接相机();
                LogAdded?.Invoke(0, "相机启动失败：" + ex.Message);
            }
        }

        private void 相机图像已到达(IntPtr imagePointer,
            ref MyCamera.MV_FRAME_OUT_INFO_EX frameInfo, int cameraIndex)
        {
            if (!检测中)
                return;
            if (cameraIndex < 0 || cameraIndex >= 相机列表.Count)
            {
                LogAdded?.Invoke(0, "收到非法相机编号：" + cameraIndex);
                return;
            }

            bool resultRecorded = false;
            try
            {
                // 当前相机配置按Mono8处理；彩色/Bayer相机后续需按像素格式增加转换。
                using (Mat original = OpenCvImageHelper.ConvertPtrToMat(
                    imagePointer, frameInfo.nWidth, frameInfo.nHeight, 1))
                {
                    DetectionResult detection;
                    if (检测器 == null)
                        throw new InvalidOperationException("DEIM ONNX模型未加载。");
                    lock (推理锁)
                        detection = 检测器.Detect(original);

                    using (detection.AnnotatedImage)
                    {
                        登记检测结果(cameraIndex, detection.IsOk);
                        resultRecorded = true;
                        int outputCode = 相机列表[cameraIndex].输出检测结果(detection.IsOk);
                        if (outputCode != 0)
                            LogAdded?.Invoke(0, $"相机{cameraIndex + 1}结果输出失败：0x{outputCode:X8}");

                        保存图片(original, cameraIndex, 0);
                        保存图片(detection.AnnotatedImage, cameraIndex, detection.IsOk ? 1 : 2);
                        更新检测界面(cameraIndex, detection);
                        LogAdded?.Invoke(detection.IsOk ? 1 : 0,
                            $"相机{cameraIndex + 1}判断{(detection.IsOk ? "OK" : "NG")}，" +
                            $"目标{detection.Detections.Count}个，耗时{detection.ElapsedMilliseconds}ms。");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!resultRecorded)
                {
                    登记检测结果(cameraIndex, false);
                    int code = 相机列表[cameraIndex].输出检测结果(false);
                    if (code != 0)
                        LogAdded?.Invoke(0, $"相机{cameraIndex + 1}故障NG输出失败：0x{code:X8}");
                }
                LogAdded?.Invoke(0, $"相机{cameraIndex + 1}检测异常，已按NG处理：{ex.Message}");
            }
        }

        private void 登记检测结果(int cameraIndex, bool isOk)
        {
            lock (计数锁)
            {
                拍照总数[cameraIndex]++;
                if (isOk) OK[cameraIndex]++; else NG[cameraIndex]++;
            }
            更新计数与状态界面(cameraIndex, isOk);
        }

        private void 更新检测界面(int cameraIndex, DetectionResult detection)
        {
            Bitmap bitmap = OpenCvImageHelper.ConvertMatToBitmap(detection.AnnotatedImage);
            if (IsDisposed || !IsHandleCreated)
            {
                bitmap.Dispose();
                return;
            }
            BeginInvoke(new Action(() => 图像显示控件[cameraIndex].SetImage(bitmap)));
        }

        private void 更新计数与状态界面(int cameraIndex, bool isOk)
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke(new Action(() =>
            {
                switch (cameraIndex)
                {
                    case 0:
                        textBox5.Text = Properties.Settings.Default.相机1总数 = 拍照总数[0].ToString();
                        textBox4.Text = Properties.Settings.Default.相机1OK数 = OK[0].ToString();
                        textBox6.Text = Properties.Settings.Default.相机1NG数 = NG[0].ToString();
                        label1.Text = isOk ? "OK" : "NG";
                        break;
                    case 1:
                        textBox2.Text = Properties.Settings.Default.相机2总数 = 拍照总数[1].ToString();
                        textBox3.Text = Properties.Settings.Default.相机2OK数 = OK[1].ToString();
                        textBox1.Text = Properties.Settings.Default.相机2NG数 = NG[1].ToString();
                        label2.Text = isOk ? "OK" : "NG";
                        break;
                    case 2:
                        textBox9.Text = Properties.Settings.Default.相机3总数 = 拍照总数[2].ToString();
                        textBox8.Text = Properties.Settings.Default.相机3OK数 = OK[2].ToString();
                        textBox7.Text = Properties.Settings.Default.相机3NG数 = NG[2].ToString();
                        label3.Text = isOk ? "OK" : "NG";
                        break;
                    case 3:
                        textBox12.Text = Properties.Settings.Default.相机4总数 = 拍照总数[3].ToString();
                        textBox11.Text = Properties.Settings.Default.相机4OK数 = OK[3].ToString();
                        textBox10.Text = Properties.Settings.Default.相机4NG数 = NG[3].ToString();
                        label4.Text = isOk ? "OK" : "NG";
                        break;
                }
            }));
        }

        private void 保存图片(Mat image, int cameraIndex, int type)
        {
            try
            {
                string rootPath = 获取保存根目录(cameraIndex, type);
                if (string.IsNullOrWhiteSpace(rootPath)) return;
                string directory = Path.Combine(rootPath, DateTime.Now.ToString("yyyyMMdd"));
                string file = Path.Combine(directory,
                    $"相机{cameraIndex + 1}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
                OpenCvImageHelper.SaveImage(image, file);
            }
            catch (Exception ex)
            {
                LogAdded?.Invoke(0, $"相机{cameraIndex + 1}图片保存失败：{ex.Message}");
            }
        }

        private static string 获取保存根目录(int cameraIndex, int type)
        {
            switch (cameraIndex)
            {
                case 0: return type == 0 ? Properties.Settings.Default.相机1图片保存路径 :
                    type == 1 ? Properties.Settings.Default.相机1OK图保存路径 : Properties.Settings.Default.相机1NG图保存路径;
                case 1: return type == 0 ? Properties.Settings.Default.相机2图片保存路径 :
                    type == 1 ? Properties.Settings.Default.相机2OK图保存路径 : Properties.Settings.Default.相机2NG图保存路径;
                case 2: return type == 0 ? Properties.Settings.Default.相机3图片保存路径 :
                    type == 1 ? Properties.Settings.Default.相机3OK图保存路径 : Properties.Settings.Default.相机3NG图保存路径;
                case 3: return type == 0 ? Properties.Settings.Default.相机4图片保存路径 :
                    type == 1 ? Properties.Settings.Default.相机4OK图保存路径 : Properties.Settings.Default.相机4NG图保存路径;
                default: return null;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (检测器 == null)
            {
                LogAdded?.Invoke(0, "DEIM模型未加载，不能开始检测。");
                return;
            }
            if (相机列表.Count == 0)
            {
                LogAdded?.Invoke(0, "没有已连接的相机。");
                return;
            }

            检测中 = !检测中;
            状态.自动状态 = 检测中;
            button1.Text = 检测中 ? "停止自动" : "开始检测";
            button1.BackColor = 检测中 ? Color.LimeGreen : Color.IndianRed;
            LogAdded?.Invoke(1, 检测中 ? "已开启自动检测，等待触发。" : "已停止自动检测。");
        }

        public void 调整控件布局(int cameraCount)
        {
            cameraCount = Math.Max(1, Math.Min(最大相机数量, cameraCount));
            tableLayoutPanel1.Controls.Clear();
            foreach (halcon view in 图像显示控件) view.Visible = false;

            if (cameraCount == 1)
            {
                tableLayoutPanel1.Controls.Add(halcon1, 0, 0);
                tableLayoutPanel1.SetColumnSpan(halcon1, 2);
                tableLayoutPanel1.SetRowSpan(halcon1, 2);
            }
            else if (cameraCount == 2)
            {
                tableLayoutPanel1.Controls.Add(halcon1, 0, 0);
                tableLayoutPanel1.Controls.Add(halcon2, 1, 0);
                tableLayoutPanel1.SetRowSpan(halcon1, 2);
                tableLayoutPanel1.SetRowSpan(halcon2, 2);
            }
            else
            {
                for (int i = 0; i < cameraCount; i++)
                    tableLayoutPanel1.Controls.Add(图像显示控件[i], i % 2, i / 2);
            }

            for (int i = 0; i < cameraCount; i++) 图像显示控件[i].Visible = true;
            Control[][] counterControls =
            {
                new Control[] { textBox5, textBox4, textBox6, textBox13 },
                new Control[] { textBox2, textBox3, textBox1, textBox14 },
                new Control[] { textBox9, textBox8, textBox7, textBox15 },
                new Control[] { textBox12, textBox11, textBox10, textBox16 }
            };
            for (int i = 0; i < counterControls.Length; i++)
            foreach (Control control in counterControls[i]) control.Visible = i < cameraCount;
        }

        private void 释放已连接相机()
        {
            foreach (MVS变量 camera in 相机列表)
            {
                camera.图像已到达 -= 相机图像已到达;
                camera.关闭相机();
            }
            相机列表.Clear();
            相机变量.CameraList = 相机列表;
        }

        public void 关闭相机()
        {
            检测中 = false;
            状态.自动状态 = false;
            释放已连接相机();
            检测器?.Dispose();
            检测器 = null;
            Properties.Settings.Default.Save();
        }
    }
}
