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

        public event Action<int, string> LogAdded;
        private readonly List<MVS变量> 相机列表 = new List<MVS变量>();
        private readonly object 计数锁 = new object();
        private readonly halcon[] 图像显示控件;
        private volatile bool 检测中;

        public int[] 拍照总数 = new int[最大相机数量];
        public int[] OK = new int[最大相机数量];
        public int[] NG = new int[最大相机数量];


        public 自动页面()
        {
            InitializeComponent();
            图像显示控件 = new[] { halcon1, halcon2 };
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
                return;
            button1.Text = "开始检测";
            button1.BackColor = Color.IndianRed;
            button1.ForeColor = Color.White;

            try
            {
                调整控件布局();
            }
            catch (InvalidOperationException)
            {
                // 构造阶段 SplitContainer 句柄和尺寸可能还没稳定，显示后会重新布局。
            }
        }

        private bool _isAligningCameraLayout = false;

        private void AlignCameraTopAndImageLayout()
        {
            if (_isAligningCameraLayout)
                return;

            _isAligningCameraLayout = true;

            try
            {
                // 1. 先固定右侧按钮区宽度
                int buttonPanelWidth = 230; // 可根据你按钮实际宽度调，图二大概 220~240 合适

                if (splitContainer1 != null && splitContainer1.ClientSize.Width > splitContainer1.SplitterWidth + 40)
                {
                    splitContainer1.Orientation = Orientation.Vertical;
                    splitContainer1.FixedPanel = FixedPanel.Panel2;
                    splitContainer1.IsSplitterFixed = true;

                    int availableWidth = splitContainer1.ClientSize.Width - splitContainer1.SplitterWidth;
                    int panel2Width = Math.Min(buttonPanelWidth, Math.Max(80, availableWidth / 3));
                    int panel1MinSize = Math.Min(200, Math.Max(1, availableWidth - panel2Width));

                    splitContainer1.Panel2MinSize = Math.Max(1, Math.Min(panel2Width, availableWidth - 1));
                    splitContainer1.Panel1MinSize = panel1MinSize;

                    int targetDistance =
                        splitContainer1.ClientSize.Width
                        - panel2Width
                        - splitContainer1.SplitterWidth;

                    int maxDistance =
                        splitContainer1.ClientSize.Width
                        - splitContainer1.SplitterWidth
                        - splitContainer1.Panel2MinSize;

                    targetDistance = Math.Max(splitContainer1.Panel1MinSize, Math.Min(targetDistance, maxDistance));

                    if (targetDistance > 0 && targetDistance <= maxDistance)
                        splitContainer1.SplitterDistance = targetDistance;
                }

                // 2. 关键：让上方统计区分割线对齐下方 Halcon 分割线
                // 下面两个名字请换成你自己 Designer 里的真实控件名
                if (splitContainerStats != null &&
                    splitContainerImages != null &&
                    !splitContainerImages.Panel2Collapsed &&
                    splitContainerStats.ClientSize.Width > 0 &&
                    splitContainerImages.ClientSize.Width > 0)
                {
                    int imageSplitterX = splitContainerImages.SplitterDistance;

                    int maxStatsDistance =
                        splitContainerStats.ClientSize.Width
                        - splitContainerStats.SplitterWidth
                        - splitContainerStats.Panel2MinSize;

                    int minStatsDistance = splitContainerStats.Panel1MinSize;

                    if (maxStatsDistance < minStatsDistance)
                        return;

                    imageSplitterX = Math.Max(minStatsDistance, Math.Min(imageSplitterX, maxStatsDistance));

                    if (imageSplitterX > 0 && imageSplitterX <= maxStatsDistance)
                        splitContainerStats.SplitterDistance = imageSplitterX;
                }
            }
            finally
            {
                _isAligningCameraLayout = false;
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            BeginAlignCameraLayout();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            BeginAlignCameraLayout();
        }

        private void BeginAlignCameraLayout()
        {
            if (!IsHandleCreated || IsDisposed || Disposing)
                return;

            try
            {
                BeginInvoke(new Action(AlignCameraTopAndImageLayout));
            }
            catch (InvalidOperationException)
            {
                // 窗体句柄正在创建/销毁时忽略本次布局，下一次 SizeChanged/Load 会重新调整。
            }
        }

        public void 电脑设备连接()
        {
            LogAdded?.Invoke(1, "OpenCV规则检测已就绪，复检结果不会自动放行为OK。");
        }

        public ModuleInspectionResult 检测图片文件(string imagePath)
        {
            using (Mat image = OpenCvImageHelper.LoadImage(imagePath))
                return ModuleInspector.Inspect(image);
        }

        public void 相机连接()
        {
            try
            {
                释放已连接相机();
                const int 相机数量 = 最大相机数量;

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
                        throw new InvalidOperationException($"{获取相机名称(i)}打开失败，错误码：0x{result:X8}");
                    }

                    result = 相机.设置曝光增益();
                    if (result != 0)
                    {
                        相机.关闭相机();
                        throw new InvalidOperationException($"{获取相机名称(i)}曝光或增益设置失败，错误码：0x{result:X8}");
                    }

                    相机.图像已到达 += 相机图像已到达;
                    result = 相机.开始采集(true);
                    if (result != 0)
                    {
                        相机.图像已到达 -= 相机图像已到达;
                        相机.关闭相机();
                        throw new InvalidOperationException($"{获取相机名称(i)}硬触发采集失败，错误码：0x{result:X8}");
                    }
                    相机列表.Add(相机);
                }

                相机变量.CameraList = 相机列表;
                LogAdded?.Invoke(1, "检测相机和分类相机已连接，正在等待硬触发。");
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
                    using (ModuleInspectionResultScope detection =
                        new ModuleInspectionResultScope(ModuleInspector.Inspect(original)))
                    {
                        ModuleInspectionResult result = detection.Result;
                        bool finalIsOk = result.Decision == ModuleInspectionDecision.Ok;
                        登记检测结果(cameraIndex, result.Decision);
                        resultRecorded = true;
                        int outputCode = 相机列表[cameraIndex].输出检测结果(finalIsOk);
                        if (outputCode != 0)
                            LogAdded?.Invoke(0, $"{获取相机名称(cameraIndex)}结果输出失败：0x{outputCode:X8}");

                        保存图片(original, cameraIndex, 0);
                        Mat display = finalIsOk ? result.AnnotatedImage : result.ErrorImage;
                        保存图片(display, cameraIndex, finalIsOk ? 1 : 2);
                        更新检测界面(cameraIndex, display);
                        LogAdded?.Invoke(finalIsOk ? 1 : 0,
                            $"{获取相机名称(cameraIndex)}判断{获取判定名称(result.Decision)}，{result.ReasonText}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!resultRecorded)
                {
                    登记检测结果(cameraIndex, ModuleInspectionDecision.Ng);
                    int code = 相机列表[cameraIndex].输出检测结果(false);
                    if (code != 0)
                        LogAdded?.Invoke(0, $"{获取相机名称(cameraIndex)}故障NG输出失败：0x{code:X8}");
                }
                LogAdded?.Invoke(0, $"{获取相机名称(cameraIndex)}检测异常，已按NG处理：{ex.Message}");
            }
        }

        private void 登记检测结果(int cameraIndex, ModuleInspectionDecision decision)
        {
            bool isOk = decision == ModuleInspectionDecision.Ok;
            lock (计数锁)
            {
                拍照总数[cameraIndex]++;
                if (isOk) OK[cameraIndex]++; else NG[cameraIndex]++;
            }
            更新计数与状态界面(cameraIndex, decision);
        }

        private void 更新检测界面(int cameraIndex, Mat image)
        {
            Bitmap bitmap = OpenCvImageHelper.ConvertMatToBitmap(image);
            if (IsDisposed || !IsHandleCreated)
            {
                bitmap.Dispose();
                return;
            }
            BeginInvoke(new Action(() => 图像显示控件[cameraIndex].SetImage(bitmap)));
        }

        private void 更新计数与状态界面(int cameraIndex, ModuleInspectionDecision decision)
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke(new Action(() =>
            {
                string decisionText = decision == ModuleInspectionDecision.Ok
                    ? "OK"
                    : decision == ModuleInspectionDecision.Ng ? "NG" : "待复检";
                Color decisionColor = decision == ModuleInspectionDecision.Ok
                    ? Color.LimeGreen
                    : decision == ModuleInspectionDecision.Ng ? Color.Red : Color.DarkOrange;
                switch (cameraIndex)
                {
                    case 0:
                        textBox5.Text = 拍照总数[0].ToString();
                        textBox4.Text = OK[0].ToString();
                        textBox6.Text = NG[0].ToString();
                        label1.Text = decisionText;
                        label1.BackColor = decisionColor;
                        break;
                    case 1:
                        textBox2.Text = 拍照总数[1].ToString();
                        textBox3.Text = OK[1].ToString();
                        textBox1.Text = NG[1].ToString();
                        label2.Text = decisionText;
                        label2.BackColor = decisionColor;
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
                    $"{获取相机名称(cameraIndex)}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
                OpenCvImageHelper.SaveImage(image, file);
            }
            catch (Exception ex)
            {
                LogAdded?.Invoke(0, $"{获取相机名称(cameraIndex)}图片保存失败：{ex.Message}");
            }
        }

        private static string 获取保存根目录(int cameraIndex, int type)
        {
            ApplicationParameters parameters = VisionParameterStore.ApplicationParameters;
            CameraRoleParameters camera = cameraIndex == 0
                ? parameters.DetectionCamera
                : parameters.ClassificationCamera;
            return type == 0 ? camera.RawImagePath : type == 1 ? camera.OkImagePath : camera.NgImagePath;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
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

        public void 调整控件布局()
        {
            const int cameraCount = 最大相机数量;
            foreach (halcon view in 图像显示控件) view.Visible = false;

            EnsureImageViewParent(halcon1, splitContainerImages.Panel1);
            EnsureImageViewParent(halcon2, splitContainerImages.Panel2);
            splitContainerImages.Panel2Collapsed = cameraCount < 2;

            for (int i = 0; i < cameraCount; i++)
                图像显示控件[i].Visible = true;

            Control[][] counterControls =
            {
                new Control[] { textBox5, textBox4, textBox6 },
                new Control[] { textBox2, textBox3, textBox1 }
            };
            for (int i = 0; i < counterControls.Length; i++)
            foreach (Control control in counterControls[i]) control.Visible = i < cameraCount;
        }

        private static void EnsureImageViewParent(halcon view, SplitterPanel panel)
        {
            if (view.Parent != panel)
            {
                view.Parent?.Controls.Remove(view);
                panel.Controls.Add(view);
            }

            view.Dock = DockStyle.Fill;
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
            Properties.Settings.Default.Save();
        }

        private static string 获取相机名称(int cameraIndex)
        {
            return cameraIndex == 0 ? "检测相机" : "分类相机";
        }

        private static string 获取判定名称(ModuleInspectionDecision decision)
        {
            return decision == ModuleInspectionDecision.Ok ? "OK" :
                decision == ModuleInspectionDecision.Ng ? "NG" : "待复检";
        }

        private sealed class ModuleInspectionResultScope : IDisposable
        {
            public ModuleInspectionResultScope(ModuleInspectionResult result)
            {
                Result = result;
            }

            public ModuleInspectionResult Result { get; }

            public void Dispose()
            {
                Result?.AnnotatedImage?.Dispose();
                Result?.ErrorImage?.Dispose();
            }
        }
    }
}
