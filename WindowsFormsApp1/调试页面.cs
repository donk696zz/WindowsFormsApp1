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

        public 调试页面()
        {
            InitializeComponent();
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
                SetResultLabel(false, "NA");
                LogAdded?.Invoke(0, "所选文件夹内没有支持的图片。");
                return;
            }

            nextImageButton.Enabled = true;
            DetectNextImage();
        }

        private void nextImageButton_Click(object sender, EventArgs e)
        {
            DetectNextImage();
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
            try
            {
                using (Mat image = OpenCvImageHelper.LoadImage(当前图片路径))
                {
                    ModuleInspectionResult result = ModuleInspector.Inspect(image);
                    using (result.AnnotatedImage)
                    using (result.ErrorImage)
                        StoreInspectionImages(result);

                    string resultName = result.IsOk ? "OK" : "NG";
                    SetResultLabel(result.IsOk, resultName);
                    CopyImageToResultFolder(当前图片路径, resultName);
                    LogAdded?.Invoke(result.IsOk ? 1 : 0,
                        $"{Path.GetFileName(当前图片路径)} 检测完成：{resultName}，{result.ReasonText}");
                }
            }
            catch (Exception ex)
            {
                SetResultLabel(false, "NG");
                LogAdded?.Invoke(0, "图片检测失败：" + ex.Message);
            }
        }

        private void SetResultLabel(bool isOk, string text)
        {
            label1.Text = text;
            label1.BackColor = isOk ? Color.LimeGreen : Color.Red;
            label1.ForeColor = Color.White;
        }

        private void CopyImageToResultFolder(string imagePath, string resultName)
        {
            string targetDirectory = Path.Combine(当前图片文件夹, resultName);
            Directory.CreateDirectory(targetDirectory);
            string targetPath = CreateUniquePath(Path.Combine(targetDirectory, Path.GetFileName(imagePath)));
            File.Copy(imagePath, targetPath);
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
            int index = comboBox1.SelectedIndex;
            if (index < 0) index = 0;
            string count = numericUpDown1.Value.ToString();
            string contrast = numericUpDown2.Value.ToString();
            string threshold = numericUpDown3.Value.ToString();

            switch (index)
            {
                case 0:
                    数据变量.相机1模板数量 = count;
                    数据变量.相机1对比度 = contrast;
                    数据变量.相机1匹配度 = threshold;
                    break;
                case 1:
                    数据变量.相机2模板数量 = count;
                    数据变量.相机2对比度 = contrast;
                    数据变量.相机2匹配度 = threshold;
                    break;
                case 2:
                    数据变量.相机3模板数量 = count;
                    数据变量.相机3对比度 = contrast;
                    数据变量.相机3匹配度 = threshold;
                    break;
                case 3:
                    数据变量.相机4模板数量 = count;
                    数据变量.相机4对比度 = contrast;
                    数据变量.相机4匹配度 = threshold;
                    break;
            }
            LogAdded?.Invoke(1, $"相机{index + 1}调试参数已更新。");
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            string count = "0", contrast = "0", threshold = "0";
            switch (index)
            {
                case 0: count = 数据变量.相机1模板数量; contrast = 数据变量.相机1对比度; threshold = 数据变量.相机1匹配度; break;
                case 1: count = 数据变量.相机2模板数量; contrast = 数据变量.相机2对比度; threshold = 数据变量.相机2匹配度; break;
                case 2: count = 数据变量.相机3模板数量; contrast = 数据变量.相机3对比度; threshold = 数据变量.相机3匹配度; break;
                case 3: count = 数据变量.相机4模板数量; contrast = 数据变量.相机4对比度; threshold = 数据变量.相机4匹配度; break;
            }
            decimal value;
            if (decimal.TryParse(count, out value)) numericUpDown1.Value = Clamp(value, numericUpDown1.Minimum, numericUpDown1.Maximum);
            if (decimal.TryParse(contrast, out value)) numericUpDown2.Value = Clamp(value, numericUpDown2.Minimum, numericUpDown2.Maximum);
            if (decimal.TryParse(threshold, out value)) numericUpDown3.Value = Clamp(value, numericUpDown3.Minimum, numericUpDown3.Maximum);
        }

        public void 相机数量设定(int num)
        {
            num = Math.Max(1, Math.Min(2, num));
            comboBox1.Items.Clear();
            for (int i = 1; i <= num; i++)
                comboBox1.Items.Add(i == 1 ? "检测相机" : "分类相机");
            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;
        }

        private void halcon1_Load(object sender, EventArgs e) { }
        private void button4_Click(object sender, EventArgs e) { halcon1.ClearDisplay(); }
        private void checkBox1_CheckedChanged(object sender, EventArgs e) { }

        private static decimal Clamp(decimal value, decimal minimum, decimal maximum)
        {
            return Math.Min(maximum, Math.Max(minimum, value));
        }
    }
}
