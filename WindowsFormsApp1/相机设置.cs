using MvCamCtrl.NET;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 相机设置 : Form
    {
        Dictionary<string, string> 相机数据;
        public event Action<int, string> LogAdded;

        // ===================== 核心变量 =====================
        /// <summary>
        /// 当前选中的相机
        /// </summary>
        private MVS变量 _curCam = null;

        /// <summary>
        /// 实时预览是否开启
        /// </summary>
        private bool _isRealOpen = false;

        
     

        public 相机设置()
        {
            InitializeComponent();
            相机数据= 账号密码的读取.账号密码文本读取();
            Control.CheckForIllegalCrossThreadCalls = false;
            数据上传();
        }
        private void 数据上传()
        {
            foreach (var item in 相机数据) 
            {
                if(item.Key == "曝光时间1") { 数据变量.相机1曝光时间=item.Value; }
                if (item.Key == "增益1") { 数据变量.相机1增益 = item.Value; }
                if(item.Key == "曝光时间2") { 数据变量.相机2曝光时间=item.Value; }
                if (item.Key == "增益2"){ 数据变量.相机2增益 = item.Value; }
                if (item.Key == "曝光时间3") { 数据变量.相机3曝光时间 = item.Value; }
                if (item.Key == "增益3") { 数据变量.相机3增益 = item.Value; }
                if (item.Key == "曝光时间4") { 数据变量.相机4曝光时间 = item.Value; }
                if (item.Key == "增益4") { 数据变量.相机4增益 = item.Value; }

            }
            if (comboBox1.SelectedIndex == 0)
            {
                // 换成 numericUpDown
                numericUpDown1.Value = Convert.ToDecimal(数据变量.相机1曝光时间);
                numericUpDown2.Value = Convert.ToDecimal(数据变量.相机1增益);
                return;
            }
            if (comboBox1.SelectedIndex == 1)
            {
                numericUpDown1.Value = Convert.ToDecimal(数据变量.相机2曝光时间);
                numericUpDown2.Value = Convert.ToDecimal(数据变量.相机2增益);
                return;
            }
            if (comboBox1.SelectedIndex == 2)
            {
                numericUpDown1.Value = Convert.ToDecimal(数据变量.相机3曝光时间);
                numericUpDown2.Value = Convert.ToDecimal(数据变量.相机3增益);
                return;
            }
            if (comboBox1.SelectedIndex == 3)
            {
                numericUpDown1.Value = Convert.ToDecimal(数据变量.相机4曝光时间);
                numericUpDown2.Value = Convert.ToDecimal(数据变量.相机4增益);
                return;
            }


        }

        private void button5_Click(object sender, EventArgs e)
        {
            // 从 NumericUpDown 获取值（直接安全获取）
            double 相机增益 = (double)numericUpDown2.Value;
            double 曝光时间 = (double)numericUpDown1.Value;

            // 先校验增益不能大于24
            if (相机增益 > 24)
            {
                LogAdded?.Invoke(0, "增益不可大于24!");
                // 恢复原值
                恢复当前相机参数显示();
                return;
            }

            // 校验曝光必须大于0
            if (曝光时间 <= 0)
            {
                LogAdded?.Invoke(0, "曝光时间必须大于0！");
                return;
            }

            // 根据选中相机，保存数据 + 实时修改相机参数
            int camIndex = comboBox1.SelectedIndex;
            bool saveSuccess = false;

            switch (camIndex)
            {
                case 0:
                    数据变量.相机1曝光时间 = numericUpDown1.Value.ToString();
                    数据变量.相机1增益 = numericUpDown2.Value.ToString();
                    相机数据["曝光时间1"] = 数据变量.相机1曝光时间;
                    相机数据["增益1"] = 数据变量.相机1增益;
                    saveSuccess = true;
                    break;

                case 1:
                    数据变量.相机2曝光时间 = numericUpDown1.Value.ToString();
                    数据变量.相机2增益 = numericUpDown2.Value.ToString();
                    相机数据["曝光时间2"] = 数据变量.相机2曝光时间;
                    相机数据["增益2"] = 数据变量.相机2增益;
                    saveSuccess = true;
                    break;

                case 2:
                    数据变量.相机3曝光时间 = numericUpDown1.Value.ToString();
                    数据变量.相机3增益 = numericUpDown2.Value.ToString();
                    相机数据["曝光时间3"] = 数据变量.相机3曝光时间;
                    相机数据["增益3"] = 数据变量.相机3增益;
                    saveSuccess = true;
                    break;

                case 3:
                    数据变量.相机4曝光时间 = numericUpDown1.Value.ToString();
                    数据变量.相机4增益 = numericUpDown2.Value.ToString();
                    相机数据["曝光时间4"] = 数据变量.相机4曝光时间;
                    相机数据["增益4"] = 数据变量.相机4增益;
                    saveSuccess = true;
                    break;
            }

            // 保存文件
            if (saveSuccess)
            {
                账号密码的读取.账号密码文本保存(相机数据);
                LogAdded?.Invoke(1, $"相机{camIndex + 1}参数保存成功！");

                // 实时修改相机曝光和增益（立即生效）
                实时更新相机参数(camIndex, 曝光时间, 相机增益);
            }
        }
        /// <summary>
        /// 【核心功能】实时更新相机曝光与增益（立即生效）
        /// </summary>
        private void 实时更新相机参数(int camIndex, double exposure, double gain)
        {
            try
            {
                // ✅ 正确逻辑：如果相机存在，才去修改
                if (相机变量.CameraList != null && camIndex < 相机变量.CameraList.Count)
                {
                    // 拿到正在运行的真实相机对象
                    MVS变量 cam = 相机变量.CameraList[camIndex];
                    cam.实时设置曝光增益(exposure, gain);
                }
            }
            catch { }
        }
        /// <summary>
        /// 恢复当前相机的原始参数
        /// </summary>
        private void 恢复当前相机参数显示()
        {
            int index = comboBox1.SelectedIndex;
            switch (index)
            {
                case 0:
                    numericUpDown1.Value = Convert.ToDecimal(数据变量.相机1曝光时间);
                    numericUpDown2.Value = Convert.ToDecimal(数据变量.相机1增益);
                    break;
                case 1:
                    numericUpDown1.Value = Convert.ToDecimal(数据变量.相机2曝光时间);
                    numericUpDown2.Value = Convert.ToDecimal(数据变量.相机2增益);
                    break;
                case 2:
                    numericUpDown1.Value = Convert.ToDecimal(数据变量.相机3曝光时间);
                    numericUpDown2.Value = Convert.ToDecimal(数据变量.相机3增益);
                    break;
                case 3:
                    numericUpDown1.Value = Convert.ToDecimal(数据变量.相机4曝光时间);
                    numericUpDown2.Value = Convert.ToDecimal(数据变量.相机4增益);
                    break;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 切换相机时，如果开启了实时，先关闭
            if (_isRealOpen)
            {
                关闭实时预览();
                button2.Text = "开启实时";
                button2.BackColor = Color.IndianRed;
                comboBox1.Enabled = true;
            }

            if (comboBox1.SelectedIndex == 0)
            {
                numericUpDown1.Value = Convert.ToDecimal(数据变量.相机1曝光时间);
                numericUpDown2.Value = Convert.ToDecimal(数据变量.相机1增益);
                return;
            }
            if (comboBox1.SelectedIndex == 1)
            {
                numericUpDown1.Value = Convert.ToDecimal(数据变量.相机2曝光时间);
                numericUpDown2.Value = Convert.ToDecimal(数据变量.相机2增益);
                return;
            }
            if (comboBox1.SelectedIndex == 2)
            {
                numericUpDown1.Value = Convert.ToDecimal(数据变量.相机3曝光时间);
                numericUpDown2.Value = Convert.ToDecimal(数据变量.相机3增益);
                return;
            }
            if (comboBox1.SelectedIndex == 3)
            {
                numericUpDown1.Value = Convert.ToDecimal(数据变量.相机4曝光时间);
                numericUpDown2.Value = Convert.ToDecimal(数据变量.相机4增益);
                return;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 判断相机是否连接
            if (相机变量.CameraList == null || 相机变量.CameraList.Count == 0)
            {
                MessageBox.Show("请先在自动页面连接相机！");
                return;
            }

            int 选中索引 = comboBox1.SelectedIndex;
            if (选中索引 >= 相机变量.CameraList.Count)
            {
                MessageBox.Show("该相机未连接！");
                return;
            }

            // 如果已经开启 → 关闭
            if (_isRealOpen)
            {
                关闭实时预览();
                button2.Text = "开启实时";
                button2.BackColor = Color.IndianRed;
                comboBox1.Enabled = true;
                状态.实时状态=false;
                return;
            }

            // 开启实时预览
            _curCam = 相机变量.CameraList[选中索引];
            _curCam.设置连续采集模式(); // ✅ 实时模式
            _curCam.实时预览开启 = true;
            _curCam.实时图像到达 += 实时图像事件;
            状态.实时状态 = true;

            _isRealOpen = true;
            button2.Text = "关闭实时";
            button2.BackColor = Color.LimeGreen;
            comboBox1.Enabled = false;
        }
        private void 实时图像事件(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX info, int index)
        {
            try
            {
                using (Mat image = OpenCvImageHelper.ConvertPtrToMat(
                    pData, info.nWidth, info.nHeight, 1))
                {
                    halcon1.SetImage(image);
                }
            }
            catch { }
        }
        public void 关闭实时预览()
        {
            if (_curCam != null)
            {
                _curCam.实时预览开启 = false;
                _curCam.实时图像到达 -= 实时图像事件;
                // 关闭实时，自动回到硬触发
                _curCam.设置硬触发();
            }

            halcon1.ClearDisplay();
            _isRealOpen = false;
        }
        public void 相机数量设定(int num)
        {
            num = Math.Max(1, Math.Min(2, num));
            comboBox1.Items.Clear();
            comboBox1.Text = string.Empty;

            for (int i = 1; i <= num; i++)
            {
                comboBox1.Items.Add(i == 1 ? "检测相机" : "分类相机");
            }

            // 默认选中第一个
            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
        }

        private void 相机设置_Load(object sender, EventArgs e)
        {
            if (int.TryParse(Properties.Settings.Default.相机数量设置, out int num))
            {
                相机数量设定(num);
            }
        }
    }
    
}
