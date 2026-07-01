using OpenCvSharp;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 调试页面 : Form
    {
        public event Action<int, string> LogAdded;
        private string 当前图片路径;
        private string 当前图片文件夹;
        private string[] 文件夹图片列表 = new string[0];
        private int 当前图片索引 = -1;
        private bool 自动检测中;

        public 调试页面()
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
                return;
            if (comboBox1.Items.Count > 0 && comboBox1.SelectedIndex < 0)
                comboBox1.SelectedIndex = 0;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "选择待检测图片文件夹"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                当前图片文件夹 = dialog.SelectedPath;
                文件夹图片列表 = Directory.GetFiles(当前图片文件夹)
                    .Where(IsSupportedImage)
                    .OrderBy(x => x)
                    .ToArray();
                当前图片索引 = -1;
            }

            if (文件夹图片列表.Length == 0)
            {
                nextImageButton.Enabled = false;
                SetResultLabel(ModuleInspectionDecision.Review, "NA");
                LogAdded?.Invoke(0, "所选文件夹内没有支持的图片。");
                return;
            }

            if (autoDetectCheckBox.Checked)
            {
                nextImageButton.Enabled = false;
                DetectAllImages();
            }
            else
            {
                nextImageButton.Enabled = true;
                DetectNextImage();
            }
        }

        private void nextImageButton_Click(object sender, EventArgs e)
        {
            if (autoDetectCheckBox.Checked)
                return;

            DetectNextImage();
        }

        private void autoDetectCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            nextImageButton.Enabled = !autoDetectCheckBox.Checked &&
                文件夹图片列表 != null &&
                文件夹图片列表.Length > 0 &&
                当前图片索引 < 文件夹图片列表.Length - 1;

            if (autoDetectCheckBox.Checked &&
                文件夹图片列表 != null &&
                文件夹图片列表.Length > 0 &&
                !自动检测中)
            {
                当前图片索引 = -1;
                DetectAllImages();
            }
        }

        private void DetectAllImages()
        {
            if (文件夹图片列表 == null || 文件夹图片列表.Length == 0)
            {
                LogAdded?.Invoke(0, "请先点击图片检测并选择图片文件夹。");
                return;
            }

            自动检测中 = true;
            nextImageButton.Enabled = false;
            try
            {
                while (autoDetectCheckBox.Checked && 当前图片索引 < 文件夹图片列表.Length - 1)
                {
                    DetectNextImage();
                    Application.DoEvents();
                }
            }
            finally
            {
                自动检测中 = false;
                nextImageButton.Enabled = !autoDetectCheckBox.Checked &&
                    文件夹图片列表 != null &&
                    文件夹图片列表.Length > 0 &&
                    当前图片索引 < 文件夹图片列表.Length - 1;
            }
        }

        private void DetectNextImage()
        {
            if (文件夹图片列表 == null || 文件夹图片列表.Length == 0)
            {
                LogAdded?.Invoke(0, "请先点击图片检测并选择图片文件夹。");
                return;
            }

            当前图片索引++;
            if (当前图片索引 >= 文件夹图片列表.Length)
            {
                当前图片索引 = 文件夹图片列表.Length - 1;
                nextImageButton.Enabled = false;
                LogAdded?.Invoke(1, "文件夹图片已检测完成。");
                return;
            }

            当前图片路径 = 文件夹图片列表[当前图片索引];
            InspectCurrentImage(null, copyToResultFolder: true, writeLog: true);
        }

        public bool 预览当前图片(InspectionParameters previewParameters)
        {
            return InspectCurrentImage(previewParameters, copyToResultFolder: false, writeLog: false);
        }

        private bool InspectCurrentImage(
            InspectionParameters previewParameters,
            bool copyToResultFolder,
            bool writeLog)
        {
            if (string.IsNullOrWhiteSpace(当前图片路径) || !File.Exists(当前图片路径))
                return false;

            bool previewMode = previewParameters != null;
            try
            {
                using (Mat image = OpenCvImageHelper.LoadImage(当前图片路径))
                {
                    ModuleInspectionResult result = previewParameters == null
                        ? ModuleInspector.Inspect(image)
                        : ModuleInspector.Inspect(
                            image,
                            VisionParameterStore.CurrentMaterialProfile.Regions,
                            previewParameters);
                    using (result.AnnotatedImage)
                    using (result.ErrorImage)
                    {
                        Mat display = result.Decision == ModuleInspectionDecision.Ok
                            ? result.AnnotatedImage
                            : result.ErrorImage;
                        halcon1.SetImage(display);

                        string resultName = GetDecisionName(result.Decision);
                        SetResultLabel(result.Decision, resultName);
                        previewModeLabel.Visible = previewMode;
                        if (copyToResultFolder)
                            SaveResultImage(display, 当前图片路径, result.Decision);
                        if (writeLog)
                        {
                            int logType = result.Decision == ModuleInspectionDecision.Ok ? 1 : 0;
                            LogAdded?.Invoke(logType,
                                $"{Path.GetFileName(当前图片路径)} 检测完成：{resultName}，{result.ReasonText}");
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                SetResultLabel(ModuleInspectionDecision.Ng, "NG");
                if (writeLog)
                    LogAdded?.Invoke(0, "图片检测失败：" + ex.Message);
                return false;
            }
        }

        private void SetResultLabel(ModuleInspectionDecision decision, string text)
        {
            label1.Text = text;
            label1.BackColor = decision == ModuleInspectionDecision.Ok
                ? Color.LimeGreen
                : decision == ModuleInspectionDecision.Ng
                    ? Color.Red
                    : Color.DarkOrange;
            label1.ForeColor = Color.White;
        }

        private void SaveResultImage(
            Mat resultImage,
            string sourceImagePath,
            ModuleInspectionDecision decision)
        {
            CameraRoleParameters storage = VisionParameterStore.ApplicationParameters.DetectionCamera;
            string targetDirectory = decision == ModuleInspectionDecision.Ok
                ? storage.OkImagePath
                : storage.NgImagePath;

            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new InvalidOperationException("请先在参数界面设置调试结果保存路径。");

            Directory.CreateDirectory(targetDirectory);
            string targetPath = CreateUniquePath(Path.Combine(
                targetDirectory,
                Path.GetFileName(sourceImagePath)));
            OpenCvImageHelper.SaveImage(resultImage, targetPath);
        }

        private static string CreateUniquePath(string path)
        {
            if (!File.Exists(path))
                return path;

            string directory = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            int index = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(directory, name + "_" + index + extension);
                index++;
            } while (File.Exists(candidate));
            return candidate;
        }

        private static bool IsSupportedImage(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!InspectCurrentImage(null, copyToResultFolder: false, writeLog: true))
                LogAdded?.Invoke(0, "请先通过“图片检测”加载一张图片。");
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void halcon1_Load(object sender, EventArgs e) { }
        private void button4_Click(object sender, EventArgs e) { halcon1.ClearDisplay(); }
        private static string GetDecisionName(ModuleInspectionDecision decision)
        {
            return decision == ModuleInspectionDecision.Ok ? "OK" :
                decision == ModuleInspectionDecision.Ng ? "NG" : "待复检";
        }

    }
}
