using OpenCvSharp;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 配方页面 : Form
    {
        public event Action<int, string> LogAdded;

        private Mat currentImage;
        private Mat currentAnnotatedImage;
        private ModuleRegionResult currentRegionResult;
        private string currentImagePath;
        private bool showingAnnotatedImage;

        public 配方页面()
        {
            InitializeComponent();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "选择配方参考图片",
                Filter = "所有支持图片|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|BMP图片|*.bmp|JPEG图片|*.jpg;*.jpeg|PNG图片|*.png|TIFF图片|*.tif;*.tiff"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    currentImage?.Dispose();
                    currentAnnotatedImage?.Dispose();

                    currentImagePath = dialog.FileName;
                    currentImage = OpenCvImageHelper.LoadImage(dialog.FileName);
                    currentAnnotatedImage = null;
                    currentRegionResult = null;
                    showingAnnotatedImage = false;

                    ShowOpenCvImage(currentImage);
                    button3.Enabled = true;
                    button6.Enabled = false;
                    UpdateToggleDisplayButton();
                    UpdateStatus("已加载：" + Path.GetFileName(dialog.FileName));
                    LogAdded?.Invoke(1, "参考图片加载成功。");
                }
                catch (Exception ex)
                {
                    UpdateStatus("参考图片加载失败");
                    LogAdded?.Invoke(0, "参考图片加载失败：" + ex.Message);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (currentImage == null || currentImage.Empty())
            {
                UpdateStatus("请先加载参考图片");
                LogAdded?.Invoke(0, "请先加载参考图片。");
                return;
            }

            try
            {
                currentAnnotatedImage?.Dispose();
                currentRegionResult = ModuleRegionLocator.Locate(currentImage);
                currentAnnotatedImage = ModuleRegionLocator.DrawRegions(currentImage, currentRegionResult);
                showingAnnotatedImage = true;

                ShowOpenCvImage(currentAnnotatedImage);
                button6.Enabled = true;
                UpdateToggleDisplayButton();
                UpdateStatus("模板区域已生成，可切换原图对比或保存模板图");
                LogAdded?.Invoke(1, "检测模板区域生成成功，已显示标注图。");
            }
            catch (Exception ex)
            {
                UpdateStatus("检测模板区域生成失败");
                LogAdded?.Invoke(0, "检测模板区域生成失败：" + ex.Message);
            }
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            if (currentAnnotatedImage == null || currentAnnotatedImage.Empty())
            {
                UpdateStatus("请先生成检测模板区域");
                LogAdded?.Invoke(0, "请先生成检测模板区域。");
                return;
            }

            try
            {
                string savePath = CreateTemplateImageSavePath();
                OpenCvImageHelper.SaveImage(currentAnnotatedImage, savePath);
                保存参考图路径(comboBox1.SelectedIndex, savePath);
                UpdateStatus("已保存：" + savePath);
                LogAdded?.Invoke(1, "配方模板图片保存成功：" + savePath);
            }
            catch (Exception ex)
            {
                UpdateStatus("模板图片保存失败");
                LogAdded?.Invoke(0, "模板图片保存失败：" + ex.Message);
            }
        }

        private void toggleDisplayButton_Click(object sender, EventArgs e)
        {
            if (currentImage == null || currentImage.Empty() ||
                currentAnnotatedImage == null || currentAnnotatedImage.Empty())
                return;

            showingAnnotatedImage = !showingAnnotatedImage;
            ShowOpenCvImage(showingAnnotatedImage ? currentAnnotatedImage : currentImage);
            UpdateToggleDisplayButton();
            UpdateStatus(showingAnnotatedImage ? "当前显示：标注图" : "当前显示：原图");
        }

        private void ShowOpenCvImage(Mat image)
        {
            if (image == null || image.Empty() || imagePictureBox == null)
                return;

            Bitmap bitmap = OpenCvImageHelper.ConvertMatToBitmap(image);
            Image oldImage = imagePictureBox.Image;
            imagePictureBox.Image = bitmap;
            oldImage?.Dispose();
        }

        private void ClearOpenCvDisplay()
        {
            if (imagePictureBox == null)
                return;

            Image oldImage = imagePictureBox.Image;
            imagePictureBox.Image = null;
            oldImage?.Dispose();
        }

        private void UpdateToggleDisplayButton()
        {
            bool canToggle = currentImage != null && !currentImage.Empty() &&
                currentAnnotatedImage != null && !currentAnnotatedImage.Empty();
            toggleDisplayButton.Enabled = canToggle;
            toggleDisplayButton.Text = showingAnnotatedImage ? "显示原图" : "显示标注";
        }

        private void UpdateStatus(string message)
        {
            statusLabel.Text = message;
        }

        private string CreateTemplateImageSavePath()
        {
            string templateFolder = comboBox1.SelectedIndex == 1 ? "分类模板" : "检测模板";
            string saveDirectory = Path.Combine(Application.StartupPath, templateFolder);
            Directory.CreateDirectory(saveDirectory);
            return Path.Combine(saveDirectory, DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".bmp");
        }

        private static string CleanFileName(string name)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar, '_');
            return name;
        }

        private static void 保存参考图路径(int index, string path)
        {
            switch (index)
            {
                case 0: 数据变量.相机1模板保存路径 = path; Properties.Settings.Default.相机1配方地址 = path; break;
                case 1: 数据变量.相机2模板保存路径 = path; Properties.Settings.Default.相机2配方地址 = path; break;
                case 2: 数据变量.相机3模板保存路径 = path; Properties.Settings.Default.相机3配方地址 = path; break;
                case 3: 数据变量.相机4模板保存路径 = path; Properties.Settings.Default.相机4配方地址 = path; break;
            }
            Properties.Settings.Default.Save();
        }

        public void 相机数量设定(int num)
        {
            num = Math.Max(1, Math.Min(2, num));
            comboBox1.Items.Clear();
            for (int i = 1; i <= num; i++)
                comboBox1.Items.Add(i == 1 ? "检测相机" : "分类相机");
            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
        }

        private void 配方页面_Load(object sender, EventArgs e)
        {
            if (int.TryParse(Properties.Settings.Default.相机数量设置, out int num))
                相机数量设定(num);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ClearOpenCvDisplay();
            currentAnnotatedImage?.Dispose();
            currentImage?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
