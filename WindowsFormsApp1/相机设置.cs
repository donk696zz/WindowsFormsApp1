using MvCamCtrl.NET;
using OpenCvSharp;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 相机设置 : Form
    {
        public event Action<int, string> LogAdded;

        private MVS变量 currentCamera;
        private bool realTimeOpen;

        public 相机设置()
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
                return;
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void 相机设置_Load(object sender, EventArgs e)
        {
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            if (comboBox1.Items.Count != 2)
            {
                comboBox1.Items.Clear();
                comboBox1.Items.Add("检测相机");
                comboBox1.Items.Add("分类相机");
            }
            comboBox1.SelectedIndex = 0;
            ShowSelectedCameraParameters();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            double exposure = (double)numericUpDown1.Value;
            double gain = (double)numericUpDown2.Value;
            if (exposure <= 0 || gain < 0 || gain > 24)
            {
                LogAdded?.Invoke(0, "曝光必须大于0，增益范围为0～24。");
                ShowSelectedCameraParameters();
                return;
            }

            ApplicationParameters parameters = VisionParameterStore.ApplicationParameters;
            CameraRoleParameters target = comboBox1.SelectedIndex == 0
                ? parameters.DetectionCamera
                : parameters.ClassificationCamera;
            target.Exposure = exposure;
            target.Gain = gain;
            VisionParameterStore.SaveApplicationParameters(parameters);
            数据变量.从参数模型同步();

            int index = comboBox1.SelectedIndex;
            if (index >= 0 && index < 相机变量.CameraList.Count)
                相机变量.CameraList[index].实时设置曝光增益(exposure, gain);

            string role = index == 0 ? "检测相机" : "分类相机";
            LogAdded?.Invoke(1, role + "曝光和增益已保存并应用。");
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (realTimeOpen) 关闭实时预览();
            ShowSelectedCameraParameters();
        }

        private void ShowSelectedCameraParameters()
        {
            if (comboBox1.SelectedIndex < 0) return;
            CameraRoleParameters target = comboBox1.SelectedIndex == 0
                ? VisionParameterStore.ApplicationParameters.DetectionCamera
                : VisionParameterStore.ApplicationParameters.ClassificationCamera;
            numericUpDown1.Value = Clamp((decimal)target.Exposure, numericUpDown1);
            numericUpDown2.Value = Clamp((decimal)target.Gain, numericUpDown2);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (realTimeOpen)
            {
                关闭实时预览();
                return;
            }

            int index = comboBox1.SelectedIndex;
            if (index < 0 || index >= 相机变量.CameraList.Count)
            {
                MessageBox.Show("所选相机未连接。", "相机设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            currentCamera = 相机变量.CameraList[index];
            currentCamera.设置连续采集模式();
            currentCamera.实时预览开启 = true;
            currentCamera.实时图像到达 += 实时图像事件;
            realTimeOpen = true;
            状态.实时状态 = true;
            button2.Text = "关闭实时";
            button2.BackColor = Color.LimeGreen;
            comboBox1.Enabled = false;
        }

        private void 实时图像事件(IntPtr data, ref MyCamera.MV_FRAME_OUT_INFO_EX info, int index)
        {
            try
            {
                using (Mat image = OpenCvImageHelper.ConvertPtrToMat(data, info.nWidth, info.nHeight, 1))
                    halcon1.SetImage(image);
            }
            catch (Exception ex)
            {
                LogAdded?.Invoke(0, "实时图像显示失败：" + ex.Message);
            }
        }

        public void 关闭实时预览()
        {
            if (currentCamera != null)
            {
                currentCamera.实时预览开启 = false;
                currentCamera.实时图像到达 -= 实时图像事件;
                currentCamera.设置硬触发();
                currentCamera = null;
            }
            halcon1.ClearDisplay();
            realTimeOpen = false;
            状态.实时状态 = false;
            button2.Text = "开启实时";
            button2.BackColor = Color.IndianRed;
            comboBox1.Enabled = true;
        }

        private static decimal Clamp(decimal value, NumericUpDown control)
        {
            return Math.Max(control.Minimum, Math.Min(control.Maximum, value));
        }
    }
}
