using OpenCvSharp;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 料号设置页面 : Form
    {
        public event Action<int, string> LogAdded;

        private Mat currentImage;
        private Mat currentAnnotatedImage;
        private MaterialRegionProfile currentProfile;
        private bool loadingControls;

        public 料号设置页面()
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
                return;
            BuildRegionControls();
        }

        private void 料号设置页面_Load(object sender, EventArgs e)
        {
            ReloadMaterials();
        }

        public void ReloadMaterials()
        {
            loadingControls = true;
            try
            {
                string current = string.IsNullOrWhiteSpace(数据变量.料号名称)
                    ? Properties.Settings.Default.当前料号
                    : 数据变量.料号名称;
                var materials = 料号切换读取文件.获取所有料号();
                if (materials.Count == 0)
                    materials.Add("默认料号");

                materialComboBox.Items.Clear();
                materialComboBox.Items.AddRange(materials.Cast<object>().ToArray());
                int selected = materials.FindIndex(x => string.Equals(x, current, StringComparison.OrdinalIgnoreCase));
                materialComboBox.SelectedIndex = selected >= 0 ? selected : 0;
            }
            finally
            {
                loadingControls = false;
            }
            LoadSelectedMaterial();
        }

        private void materialComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loadingControls)
                LoadSelectedMaterial();
        }

        private void LoadSelectedMaterial()
        {
            string material = materialComboBox.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(material)) return;

            currentProfile = VisionParameterStore.LoadMaterial(material).Clone();
            BindRegionControls();
            ClearSelectedImage();
            statusLabel.Text = $"当前料号：{material}。请先点击“选择检测图”。";
        }

        private void selectImageButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "选择当前料号的检测参考图",
                Filter = "图片|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                LoadImage(dialog.FileName);
                statusLabel.Text = "检测图已加载，调节右侧参数可实时查看框位置。";
            }
        }

        private void LoadImage(string path)
        {
            try
            {
                currentImage?.Dispose();
                currentImage = OpenCvImageHelper.LoadImage(path);
                RefreshPreview();
            }
            catch (Exception ex)
            {
                LogAdded?.Invoke(0, "检测图加载失败：" + ex.Message);
                statusLabel.Text = "检测图加载失败";
            }
        }

        private void ClearSelectedImage()
        {
            currentAnnotatedImage?.Dispose();
            currentAnnotatedImage = null;
            currentImage?.Dispose();
            currentImage = null;
            Image old = imagePictureBox.Image;
            imagePictureBox.Image = null;
            old?.Dispose();
        }

        private void saveDetectionImageButton_Click(object sender, EventArgs e)
        {
            if (currentProfile == null)
            {
                statusLabel.Text = "请先选择料号";
                return;
            }

            try
            {
                currentProfile.Regions.Validate();
                if (currentAnnotatedImage != null && !currentAnnotatedImage.Empty())
                {
                    string directory = Path.Combine(Application.StartupPath, "视觉参数", "料号参考图");
                    Directory.CreateDirectory(directory);
                    string path = Path.Combine(directory, CleanFileName(currentProfile.MaterialNumber) + ".bmp");
                    OpenCvImageHelper.SaveImage(currentAnnotatedImage, path);
                    currentProfile.ReferenceImagePath = path;
                }

                VisionParameterStore.SaveMaterialProfile(currentProfile);
                statusLabel.Text = $"已保存料号 {currentProfile.MaterialNumber} 的框比例参数和检测图。";
                LogAdded?.Invoke(1, statusLabel.Text);
            }
            catch (Exception ex)
            {
                statusLabel.Text = "保存失败：" + ex.Message;
                LogAdded?.Invoke(0, statusLabel.Text);
            }
        }

        private void resetRegionButton_Click(object sender, EventArgs e)
        {
            if (currentProfile == null) return;
            currentProfile.Regions = new RegionParameters();
            BindRegionControls();
            RefreshPreview();
            statusLabel.Text = "已恢复默认框参数，点击“保存检测图”后生效。";
        }

        private void RefreshPreview()
        {
            if (currentImage == null || currentImage.Empty() || currentProfile == null)
                return;

            try
            {
                currentAnnotatedImage?.Dispose();
                ModuleRegionResult regions;
                using (Mat aligned = ModuleRegionLocator.AlignToModule(
                    currentImage, currentProfile.Regions, out double _))
                {
                    regions = ModuleRegionLocator.Locate(aligned, currentProfile.Regions);
                    currentAnnotatedImage = ModuleRegionLocator.DrawRegions(aligned, regions);
                }
                ShowImage(currentAnnotatedImage);
                statusLabel.Text = $"产品框：X={regions.ModuleBox.X}, Y={regions.ModuleBox.Y}, " +
                    $"W={regions.ModuleBox.Width}, H={regions.ModuleBox.Height}";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "框预览失败：" + ex.Message;
            }
        }

        private void ShowImage(Mat image)
        {
            Bitmap bitmap = OpenCvImageHelper.ConvertMatToBitmap(image);
            Image old = imagePictureBox.Image;
            imagePictureBox.Image = bitmap;
            old?.Dispose();
        }

        private void BuildRegionControls()
        {
            AddSectionLabel(mainRegionFlowPanel, "银面与中间区域");
            AddPercent(mainRegionFlowPanel, "银面外边界", p => p.SilverOuterEdgeRatio, (p, v) => p.SilverOuterEdgeRatio = v, 0, 45);
            AddPercent(mainRegionFlowPanel, "银面内边界", p => p.SilverInnerEdgeRatio, (p, v) => p.SilverInnerEdgeRatio = v, 1, 49);
            AddPercent(mainRegionFlowPanel, "中间区域宽度", p => p.MiddleWidthRatio, (p, v) => p.MiddleWidthRatio = v, 1, 90);
            AddPercent(mainRegionFlowPanel, "银面顶部缩进", p => p.SilverTopRatio, (p, v) => p.SilverTopRatio = v, 0, 45);
            AddPercent(mainRegionFlowPanel, "银面底部缩进", p => p.SilverBottomRatio, (p, v) => p.SilverBottomRatio = v, 0, 45);
            AddPercent(mainRegionFlowPanel, "顶边检查高度", p => p.TopInspectHeightRatio, (p, v) => p.TopInspectHeightRatio = v, 0.1m, 45);
            AddPercent(mainRegionFlowPanel, "底边检查高度", p => p.BottomInspectHeightRatio, (p, v) => p.BottomInspectHeightRatio = v, 0.1m, 45);
            AddPercent(mainRegionFlowPanel, "侧边检查宽度", p => p.SideInspectWidthRatio, (p, v) => p.SideInspectWidthRatio = v, 0.1m, 45);
            AddPercent(mainRegionFlowPanel, "中间左右缩进", p => p.MiddleInspectHorizontalInsetRatio, (p, v) => p.MiddleInspectHorizontalInsetRatio = v, 0, 45);
            AddPercent(mainRegionFlowPanel, "银面上下分割", p => p.SilverVerticalSplitRatio, (p, v) => p.SilverVerticalSplitRatio = v, 10, 90);
            AddPercent(mainRegionFlowPanel, "银面内部横缩进", p => p.SilverInnerHorizontalInsetRatio, (p, v) => p.SilverInnerHorizontalInsetRatio = v, 0, 45);
            AddPercent(mainRegionFlowPanel, "银面内部纵缩进", p => p.SilverInnerVerticalInsetRatio, (p, v) => p.SilverInnerVerticalInsetRatio = v, 0, 45);
            AddPercent(mainRegionFlowPanel, "边缘检查裁剪", p => p.EdgeInspectTrimRatio, (p, v) => p.EdgeInspectTrimRatio = v, 0, 45);

            AddSectionLabel(locatorFlowPanel, "产品自动定位");
            AddInteger(locatorFlowPanel, "产品暗像素阈值", p => p.ModuleDarkThreshold, (p, v) => p.ModuleDarkThreshold = v, 0, 255);
            AddPercent(locatorFlowPanel, "列有效高度比例", p => p.ColumnActivationRatio, (p, v) => p.ColumnActivationRatio = v, 0.1m, 50);
            AddInteger(locatorFlowPanel, "列有效像素下限", p => p.ColumnActivationMinimum, (p, v) => p.ColumnActivationMinimum = v, 1, 5000);
            AddPercent(locatorFlowPanel, "行有效宽度比例", p => p.RowActivationRatio, (p, v) => p.RowActivationRatio = v, 0.1m, 50);
            AddInteger(locatorFlowPanel, "行有效像素下限", p => p.RowActivationMinimum, (p, v) => p.RowActivationMinimum = v, 1, 5000);
            AddInteger(locatorFlowPanel, "列平滑半径", p => p.ColumnSmoothRadius, (p, v) => p.ColumnSmoothRadius = v, 0, 100);
            AddInteger(locatorFlowPanel, "行平滑半径", p => p.RowSmoothRadius, (p, v) => p.RowSmoothRadius = v, 0, 100);
            AddInteger(locatorFlowPanel, "列支持半径", p => p.ColumnSupportRadius, (p, v) => p.ColumnSupportRadius = v, 0, 100);
            AddInteger(locatorFlowPanel, "列最少支持点", p => p.ColumnMinimumSupport, (p, v) => p.ColumnMinimumSupport = v, 1, 200);
            AddInteger(locatorFlowPanel, "行支持半径", p => p.RowSupportRadius, (p, v) => p.RowSupportRadius = v, 0, 100);
            AddInteger(locatorFlowPanel, "行最少支持点", p => p.RowMinimumSupport, (p, v) => p.RowMinimumSupport = v, 1, 200);
        }

        private void AddPercent(
            FlowLayoutPanel parent,
            string text,
            Func<RegionParameters, double> getter,
            Action<RegionParameters, double> setter,
            decimal minimum,
            decimal maximum)
        {
            AddNumericRow(parent, text, minimum, maximum, 3, 0.1m,
                () => (decimal)(getter(currentProfile.Regions) * 100.0),
                value => setter(currentProfile.Regions, (double)value / 100.0), "%");
        }

        private void AddInteger(
            FlowLayoutPanel parent,
            string text,
            Func<RegionParameters, int> getter,
            Action<RegionParameters, int> setter,
            int minimum,
            int maximum)
        {
            AddNumericRow(parent, text, minimum, maximum, 0, 1,
                () => getter(currentProfile.Regions),
                value => setter(currentProfile.Regions, Decimal.ToInt32(value)), string.Empty);
        }

        private void AddNumericRow(
            FlowLayoutPanel parent,
            string text,
            decimal minimum,
            decimal maximum,
            int decimals,
            decimal increment,
            Func<decimal> getter,
            Action<decimal> setter,
            string unit)
        {
            var panel = new Panel { Width = 390, Height = 48, Margin = new Padding(3) };
            var label = new Label
            {
                Text = text,
                Location = new System.Drawing.Point(4, 10),
                Size = new System.Drawing.Size(185, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var numeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(194, 9),
                Size = new System.Drawing.Size(130, 28),
                Minimum = minimum,
                Maximum = maximum,
                DecimalPlaces = decimals,
                Increment = increment,
                Tag = getter
            };
            var unitLabel = new Label
            {
                Text = unit,
                Location = new System.Drawing.Point(330, 10),
                Size = new System.Drawing.Size(45, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
            numeric.ValueChanged += (s, e) =>
            {
                if (loadingControls || currentProfile == null) return;
                setter(numeric.Value);
                currentProfile.Regions.Validate();
                RefreshPreview();
            };
            panel.Controls.Add(label);
            panel.Controls.Add(numeric);
            panel.Controls.Add(unitLabel);
            parent.Controls.Add(panel);
        }

        private static void AddSectionLabel(FlowLayoutPanel parent, string text)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 95, 150),
                Width = 390,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft
            });
        }

        private void BindRegionControls()
        {
            if (currentProfile == null) return;
            loadingControls = true;
            try
            {
                BindPanel(mainRegionFlowPanel);
                BindPanel(locatorFlowPanel);
            }
            finally
            {
                loadingControls = false;
            }
        }

        private static void BindPanel(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is NumericUpDown numeric && numeric.Tag is Func<decimal> getter)
                {
                    decimal value = getter();
                    numeric.Value = Math.Max(numeric.Minimum, Math.Min(numeric.Maximum, value));
                }
                if (child.HasChildren) BindPanel(child);
            }
        }

        private static string CleanFileName(string value)
        {
            string safe = string.IsNullOrWhiteSpace(value) ? "默认料号" : value;
            foreach (char invalid in Path.GetInvalidFileNameChars()) safe = safe.Replace(invalid, '_');
            return safe;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            currentAnnotatedImage?.Dispose();
            currentImage?.Dispose();
            imagePictureBox.Image?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
