using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 参数界面 : Form
    {
        public event Action<int, string> LogAdded;
        public event Func<InspectionParameters, bool> PreviewRequested;

        private ApplicationParameters parameters;
        private readonly List<Action> bindActions = new List<Action>();
        private bool loading;
        private Timer previewTimer;

        private TextBox detectionOkPath;
        private TextBox detectionNgPath;
        private NumericUpDown permissionTimeout;
        private NumericUpDown retentionDays;

        public 参数界面()
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime)
            {
                parameters = new ApplicationParameters();
                return;
            }
            previewTimer = new Timer { Interval = 250 };
            previewTimer.Tick += previewTimer_Tick;
            parameters = VisionParameterStore.ApplicationParameters.Clone();
            BuildDecisionPage();
            BuildAdvancedPage();
            BuildCameraStoragePage();
            BuildSystemPage();
        }

        private void 参数界面_Load(object sender, EventArgs e)
        {
            第一次数据加载();
        }

        public void 第一次数据加载()
        {
            parameters = VisionParameterStore.ApplicationParameters.Clone();
            刷新当前料号显示();
            BindAll();
        }

        public void 刷新当前料号显示()
        {
            currentMaterialLabel.Text = "当前料号：" +
                (string.IsNullOrWhiteSpace(数据变量.料号名称) ? "未选择" : 数据变量.料号名称);
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            try
            {
                parameters.Inspection.Validate();
                ApplicationParameters snapshot = VisionParameterStore.ApplicationParameters;
                snapshot.Inspection = parameters.Inspection.Clone();
                snapshot.DetectionCamera.OkImagePath = detectionOkPath.Text.Trim();
                snapshot.DetectionCamera.NgImagePath = detectionNgPath.Text.Trim();
                snapshot.PermissionTimeoutMinutes = Decimal.ToInt32(permissionTimeout.Value);
                snapshot.ImageRetentionDays = Decimal.ToInt32(retentionDays.Value);

                VisionParameterStore.SaveApplicationParameters(snapshot);
                parameters = snapshot;
                数据变量.从参数模型同步();
                statusLabel.Text = "参数已保存并应用。";
                LogAdded?.Invoke(1, statusLabel.Text);
                PreviewRequested?.Invoke(null);
            }
            catch (Exception ex)
            {
                statusLabel.Text = "参数保存失败：" + ex.Message;
                LogAdded?.Invoke(0, statusLabel.Text);
            }
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定恢复全部检测参数默认值？", "恢复默认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            parameters.Inspection = new InspectionParameters();
            BindAll();
            statusLabel.Text = "已恢复检测参数默认值；相机与保存路径保持不变，点击“保存参数”后生效。";
            SchedulePreview();
        }

        private void BuildAdaptiveDecisionPage()
        {
            GroupBox coverage = CreateGroup("银面上下半区覆盖率", 630, 300);
            FlowLayoutPanel coverageFlow = CreateVerticalFlow();
            coverage.Controls.Add(coverageFlow);
            AddRatio(coverageFlow, "低于此值判NG", p => p.SilverCoverageNgRatio,
                (p, v) => p.SilverCoverageNgRatio = v, 0, 100);
            AddRatio(coverageFlow, "高于此值判OK", p => p.SilverCoverageOkRatio,
                (p, v) => p.SilverCoverageOkRatio = v, 0, 100);
            AddHelp(coverageFlow, "左右银面分别拆成上、下半区；NG线与OK线之间判复查。");
            decisionFlowPanel.Controls.Add(coverage);

            GroupBox inner = CreateGroup("银面内部缺陷", 630, 280);
            FlowLayoutPanel innerFlow = CreateVerticalFlow();
            inner.Controls.Add(innerFlow);
            AddInteger(innerFlow, "复查面积(px)", p => p.InnerDefectReviewArea,
                (p, v) => p.InnerDefectReviewArea = v, 1, 100000);
            AddInteger(innerFlow, "NG面积(px)", p => p.MinimumDefectArea,
                (p, v) => p.MinimumDefectArea = v, 1, 100000);
            AddHelp(innerFlow, "面积为开运算后单个内部连通缺陷的像素数。");
            decisionFlowPanel.Controls.Add(inner);

            GroupBox edge = CreateGroup("银面边缘凹陷", 630, 400);
            FlowLayoutPanel edgeFlow = CreateVerticalFlow();
            edge.Controls.Add(edgeFlow);
            AddInteger(edgeFlow, "复查面积(px)", p => p.EdgeConcavityReviewArea,
                (p, v) => p.EdgeConcavityReviewArea = v, 1, 100000);
            AddInteger(edgeFlow, "NG面积(px)", p => p.EdgeConcavityNgArea,
                (p, v) => p.EdgeConcavityNgArea = v, 1, 100000);
            AddNumber(edgeFlow, "面积形状换算系数", p => p.EdgeConcavityShapeScale,
                (p, v) => p.EdgeConcavityShapeScale = v, 0.1m, 100, 1);
            AddNumber(edgeFlow, "最小平均厚度(px)", p => p.EdgeConcavityMinimumThickness,
                (p, v) => p.EdgeConcavityMinimumThickness = v, 0.1m, 100, 1);
            AddHelp(edgeFlow, "有效面积=实际面积×深度÷开口宽度×换算系数；开口越宽或深度越浅，越难达到NG。平均厚度过小的细长区域会被忽略。");
            decisionFlowPanel.Controls.Add(edge);

            GroupBox middle = CreateGroup("中间框残银", 630, 260);
            FlowLayoutPanel middleFlow = CreateVerticalFlow();
            middle.Controls.Add(middleFlow);
            AddRatioPair(middleFlow, "高亮像素占比",
                p => p.MiddleSilverReviewRatio,
                (p, v) => p.MiddleSilverReviewRatio = v,
                p => p.MiddleSilverNgRatio,
                (p, v) => p.MiddleSilverNgRatio = v);
            AddHelp(middleFlow, "低于复查线为OK，达到NG线直接判NG。");
            decisionFlowPanel.Controls.Add(middle);
        }

        private void BuildAdaptiveAdvancedPage()
        {
            GroupBox silverMask = CreateGroup("银面掩码", 630, 420);
            FlowLayoutPanel maskFlow = CreateVerticalFlow();
            silverMask.Controls.Add(maskFlow);
            AddRatio(maskFlow, "闭运算核/短边", p => p.AdaptiveMaskCloseRatio,
                (p, v) => p.AdaptiveMaskCloseRatio = v, 0.5m, 25);
            AddRatio(maskFlow, "开运算核/短边", p => p.AdaptiveMaskOpenRatio,
                (p, v) => p.AdaptiveMaskOpenRatio = v, 0.5m, 25);
            AddRatio(maskFlow, "覆盖率圆角/短边", p => p.AdaptiveRoundedCornerRatio,
                (p, v) => p.AdaptiveRoundedCornerRatio = v, 2, 50);
            AddInteger(maskFlow, "内部缺陷开运算核", p => p.DefectMorphKernelSize,
                (p, v) => p.DefectMorphKernelSize = v, 3, 31);
            AddHelp(maskFlow, "银面先闭运算再开运算；形态学核保存时自动校验。");
            advancedFlowPanel.Controls.Add(silverMask);

            GroupBox visibility = CreateGroup("边缘框与物料完整性", 630, 300);
            FlowLayoutPanel visibilityFlow = CreateVerticalFlow();
            visibility.Controls.Add(visibilityFlow);
            AddInteger(visibilityFlow, "画面边界检查宽度", p => p.ModuleVisibleMargin,
                (p, v) => p.ModuleVisibleMargin = v, 0, 100);
            AddRatio(visibilityFlow, "边界深色占比上限", p => p.ModuleBorderDarkRatio,
                (p, v) => p.ModuleBorderDarkRatio = v, 0, 100);
            AddHelp(visibilityFlow, "保留现有边缘框定位与物料完整显示检测。");
            advancedFlowPanel.Controls.Add(visibility);

            GroupBox middle = CreateGroup("中间框检测", 630, 360);
            FlowLayoutPanel middleFlow = CreateVerticalFlow();
            middle.Controls.Add(middleFlow);
            AddInteger(middleFlow, "灰度阈值偏移", p => p.MiddleSilverThresholdOffset,
                (p, v) => p.MiddleSilverThresholdOffset = v, 0, 255);
            AddInteger(middleFlow, "阈值下限", p => p.MiddleSilverThresholdMinimum,
                (p, v) => p.MiddleSilverThresholdMinimum = v, 0, 255);
            AddInteger(middleFlow, "阈值上限", p => p.MiddleSilverThresholdMaximum,
                (p, v) => p.MiddleSilverThresholdMaximum = v, 0, 255);
            advancedFlowPanel.Controls.Add(middle);
        }

        private void BuildDecisionPage()
        {
            BuildAdaptiveDecisionPage();
        }

        private void BuildAdvancedPage()
        {
            BuildAdaptiveAdvancedPage();
        }

        private void BuildCameraStoragePage()
        {
            GroupBox storage = CreateGroup("调试图片保存路径", 900, 240);
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 3,
                RowCount = 2
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
            detectionOkPath = AddPathRow(table, 0, "OK结果图路径");
            detectionNgPath = AddPathRow(table, 1, "NG/复检结果图路径");
            storage.Controls.Add(table);
            cameraStorageFlowPanel.Controls.Add(storage);
        }

        private void BuildSystemPage()
        {
            GroupBox group = CreateGroup("系统维护", 630, 250);
            FlowLayoutPanel flow = CreateVerticalFlow();
            group.Controls.Add(flow);
            permissionTimeout = CreateStandaloneIntegerRow(flow, "管理员无操作退出(分钟)", 1, 1440);
            retentionDays = CreateStandaloneIntegerRow(flow, "图片保留天数", 1, 3650);
            AddHelp(flow, "框比例参数已移到“料号设置”页面，并按当前料号分别保存。系统固定使用检测相机和分类相机两台设备。");
            systemFlowPanel.Controls.Add(group);
        }

        private TextBox AddPathRow(TableLayoutPanel table, int row, string label)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            table.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
            var text = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Margin = new Padding(3, 16, 3, 10) };
            var button = new Button { Text = "浏览", Dock = DockStyle.Fill, Margin = new Padding(4, 10, 4, 10) };
            button.Click += (s, e) => BrowseFolder(text);
            table.Controls.Add(text, 1, row);
            table.Controls.Add(button, 2, row);
            return text;
        }

        private void BrowseFolder(TextBox target)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (Directory.Exists(target.Text)) dialog.SelectedPath = target.Text;
                if (dialog.ShowDialog() == DialogResult.OK) target.Text = dialog.SelectedPath;
            }
        }

        private void AddRatioPair(
            FlowLayoutPanel parent,
            string title,
            Func<InspectionParameters, double> reviewGetter,
            Action<InspectionParameters, double> reviewSetter,
            Func<InspectionParameters, double> ngGetter,
            Action<InspectionParameters, double> ngSetter)
        {
            var row = new TableLayoutPanel { Width = 580, Height = 70, ColumnCount = 5 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            row.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            row.Controls.Add(new Label { Text = "复检≥", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 1, 0);
            NumericUpDown review = CreateNumeric(0, 100, 3, 0.01m);
            review.Dock = DockStyle.Fill;
            row.Controls.Add(review, 2, 0);
            row.Controls.Add(new Label { Text = "NG>", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 3, 0);
            NumericUpDown ng = CreateNumeric(0, 100, 3, 0.01m);
            ng.Dock = DockStyle.Fill;
            row.Controls.Add(ng, 4, 0);
            Bind(review, () => (decimal)(reviewGetter(parameters.Inspection) * 100), v => reviewSetter(parameters.Inspection, (double)v / 100));
            Bind(ng, () => (decimal)(ngGetter(parameters.Inspection) * 100), v => ngSetter(parameters.Inspection, (double)v / 100));
            parent.Controls.Add(row);
        }

        private void AddRatio(FlowLayoutPanel parent, string label,
            Func<InspectionParameters, double> getter, Action<InspectionParameters, double> setter,
            decimal min, decimal max)
        {
            AddParameterRow(parent, label, min, max, 3, 0.01m, "%",
                () => (decimal)(getter(parameters.Inspection) * 100),
                v => setter(parameters.Inspection, (double)v / 100));
        }

        private void AddInteger(FlowLayoutPanel parent, string label,
            Func<InspectionParameters, int> getter, Action<InspectionParameters, int> setter,
            decimal min, decimal max)
        {
            AddParameterRow(parent, label, min, max, 0, 1, string.Empty,
                () => getter(parameters.Inspection),
                v => setter(parameters.Inspection, Decimal.ToInt32(v)));
        }

        private void AddNumber(FlowLayoutPanel parent, string label,
            Func<InspectionParameters, double> getter, Action<InspectionParameters, double> setter,
            decimal min, decimal max, int decimals)
        {
            AddParameterRow(parent, label, min, max, decimals, 0.01m, string.Empty,
                () => (decimal)getter(parameters.Inspection),
                v => setter(parameters.Inspection, (double)v));
        }

        private void AddParameterRow(FlowLayoutPanel parent, string label,
            decimal min, decimal max, int decimals, decimal increment, string unit,
            Func<decimal> getter, Action<decimal> setter)
        {
            var row = new Panel { Width = 580, Height = 50 };
            row.Controls.Add(new Label { Text = label, Location = new Point(4, 10), Size = new Size(250, 28), TextAlign = ContentAlignment.MiddleLeft });
            NumericUpDown numeric = CreateNumeric(min, max, decimals, increment);
            numeric.Location = new Point(265, 9);
            numeric.Size = new Size(220, 28);
            row.Controls.Add(numeric);
            row.Controls.Add(new Label { Text = unit, Location = new Point(495, 10), Size = new Size(45, 28), TextAlign = ContentAlignment.MiddleLeft });
            Bind(numeric, getter, setter);
            parent.Controls.Add(row);
        }

        private NumericUpDown CreateStandaloneIntegerRow(FlowLayoutPanel parent, string label, int min, int max)
        {
            var row = new Panel { Width = 580, Height = 52 };
            row.Controls.Add(new Label { Text = label, Location = new Point(4, 10), Size = new Size(280, 28), TextAlign = ContentAlignment.MiddleLeft });
            var numeric = CreateNumeric(min, max, 0, 1);
            numeric.Location = new Point(300, 9);
            numeric.Size = new Size(180, 28);
            row.Controls.Add(numeric);
            parent.Controls.Add(row);
            return numeric;
        }

        private static NumericUpDown CreateNumeric(decimal min, decimal max, int decimals, decimal increment)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                Increment = increment,
                Margin = new Padding(3, 18, 3, 18),
                ThousandsSeparator = true
            };
        }

        private void Bind(NumericUpDown numeric, Func<decimal> getter, Action<decimal> setter)
        {
            bindActions.Add(() => numeric.Value = Math.Max(numeric.Minimum, Math.Min(numeric.Maximum, getter())));
            numeric.ValueChanged += (s, e) =>
            {
                if (loading) return;
                setter(numeric.Value);
                SchedulePreview();
            };
        }

        private void livePreviewCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (previewTimer == null) return;
            if (livePreviewCheckBox.Checked)
            {
                SchedulePreview();
            }
            else
            {
                previewTimer.Stop();
                statusLabel.Text = "实时预览已关闭；保存参数后仍会刷新当前调试图。";
            }
        }

        private void SchedulePreview()
        {
            if (loading || previewTimer == null || !livePreviewCheckBox.Checked)
                return;
            previewTimer.Stop();
            previewTimer.Start();
        }

        private void previewTimer_Tick(object sender, EventArgs e)
        {
            previewTimer.Stop();
            RequestPreview(updateStatus: true);
        }

        private void RequestPreview(bool updateStatus)
        {
            if (parameters?.Inspection == null) return;
            InspectionParameters snapshot = parameters.Inspection.Clone();
            snapshot.Validate();
            bool updated = PreviewRequested?.Invoke(snapshot) ?? false;
            if (!updateStatus) return;
            statusLabel.Text = updated
                ? "预览已更新，切换到调试页面可查看；当前修改尚未保存。"
                : "调试页面尚未加载图片，请先在调试页面选择图片文件夹。";
        }

        private void BindAll()
        {
            loading = true;
            try
            {
                foreach (Action action in bindActions) action();
                detectionOkPath.Text = parameters.DetectionCamera.OkImagePath;
                detectionNgPath.Text = parameters.DetectionCamera.NgImagePath;
                permissionTimeout.Value = Clamp(parameters.PermissionTimeoutMinutes, permissionTimeout);
                retentionDays.Value = Clamp(parameters.ImageRetentionDays, retentionDays);
            }
            finally
            {
                loading = false;
            }
        }

        private static decimal Clamp(decimal value, NumericUpDown control)
        {
            return Math.Max(control.Minimum, Math.Min(control.Maximum, value));
        }

        private static GroupBox CreateGroup(string title, int width, int height)
        {
            return new GroupBox
            {
                Text = title,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                Width = width,
                Height = height,
                Padding = new Padding(12),
                Margin = new Padding(12)
            };
        }

        private static FlowLayoutPanel CreateVerticalFlow()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
        }

        private static void AddHelp(FlowLayoutPanel parent, string text)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                ForeColor = Color.DimGray,
                Width = 570,
                Height = 60,
                Padding = new Padding(4),
                AutoEllipsis = true
            });
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            previewTimer?.Stop();
            previewTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
