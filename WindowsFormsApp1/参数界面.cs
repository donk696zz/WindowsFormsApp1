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

        private TextBox detectionRawPath;
        private TextBox detectionOkPath;
        private TextBox detectionNgPath;
        private TextBox classificationRawPath;
        private TextBox classificationOkPath;
        private TextBox classificationNgPath;
        private NumericUpDown detectionExposure;
        private NumericUpDown detectionGain;
        private NumericUpDown classificationExposure;
        private NumericUpDown classificationGain;
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
                parameters.DetectionCamera.RawImagePath = detectionRawPath.Text.Trim();
                parameters.DetectionCamera.OkImagePath = detectionOkPath.Text.Trim();
                parameters.DetectionCamera.NgImagePath = detectionNgPath.Text.Trim();
                parameters.ClassificationCamera.RawImagePath = classificationRawPath.Text.Trim();
                parameters.ClassificationCamera.OkImagePath = classificationOkPath.Text.Trim();
                parameters.ClassificationCamera.NgImagePath = classificationNgPath.Text.Trim();
                parameters.DetectionCamera.Exposure = (double)detectionExposure.Value;
                parameters.DetectionCamera.Gain = (double)detectionGain.Value;
                parameters.ClassificationCamera.Exposure = (double)classificationExposure.Value;
                parameters.ClassificationCamera.Gain = (double)classificationGain.Value;
                parameters.PermissionTimeoutMinutes = Decimal.ToInt32(permissionTimeout.Value);
                parameters.ImageRetentionDays = Decimal.ToInt32(retentionDays.Value);

                VisionParameterStore.SaveApplicationParameters(parameters);
                数据变量.从参数模型同步();
                ApplyCameraParameters();
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

            parameters = new ApplicationParameters();
            BindAll();
            statusLabel.Text = "已载入默认值，点击“保存参数”后生效。";
            SchedulePreview();
        }

        private void ApplyCameraParameters()
        {
            if (相机变量.CameraList.Count > 0)
                相机变量.CameraList[0].实时设置曝光增益(
                    parameters.DetectionCamera.Exposure, parameters.DetectionCamera.Gain);
            if (相机变量.CameraList.Count > 1)
                相机变量.CameraList[1].实时设置曝光增益(
                    parameters.ClassificationCamera.Exposure, parameters.ClassificationCamera.Gain);
        }

        private void BuildAdaptiveDecisionPage()
        {
            GroupBox group = CreateGroup("三级分流", 630, 310);
            FlowLayoutPanel flow = CreateVerticalFlow();
            group.Controls.Add(flow);
            AddRatioPair(flow, "中间残银", p => p.MiddleSilverReviewRatio, (p, v) => p.MiddleSilverReviewRatio = v,
                p => p.MiddleSilverNgRatio, (p, v) => p.MiddleSilverNgRatio = v);
            AddHelp(flow, "明显OK：与正常样本库足够接近。明显NG：物料未完整显示、明显中间残银，或形状异常超过全部OK样本上限。其余进入复检；没有可靠区域时不画缺陷框。");
            decisionFlowPanel.Controls.Add(group);
        }

        private void BuildAdaptiveAdvancedPage()
        {
            GroupBox adaptive = CreateGroup("正常模型与自适应银区", 630, 760);
            FlowLayoutPanel flow = CreateVerticalFlow();
            adaptive.Controls.Add(flow);
            AddInteger(flow, "银区自适应阈值下限", p => p.AdaptiveSilverThresholdMinimum, (p, v) => p.AdaptiveSilverThresholdMinimum = v, 0, 255);
            AddInteger(flow, "银区自适应阈值上限", p => p.AdaptiveSilverThresholdMaximum, (p, v) => p.AdaptiveSilverThresholdMaximum = v, 0, 255);
            AddRatio(flow, "闭运算核/银区短边", p => p.AdaptiveMaskCloseRatio, (p, v) => p.AdaptiveMaskCloseRatio = v, 0.5m, 50);
            AddRatio(flow, "开运算核/银区短边", p => p.AdaptiveMaskOpenRatio, (p, v) => p.AdaptiveMaskOpenRatio = v, 0.5m, 25);
            AddRatio(flow, "内部腐蚀核/银区短边", p => p.AdaptiveInteriorErodeRatio, (p, v) => p.AdaptiveInteriorErodeRatio = v, 0.5m, 50);
            AddRatio(flow, "纹理背景核/银区短边", p => p.AdaptiveBackgroundKernelRatio, (p, v) => p.AdaptiveBackgroundKernelRatio = v, 1, 80);
            AddRatio(flow, "正常形状核心概率", p => p.AdaptiveShapeCoreProbability, (p, v) => p.AdaptiveShapeCoreProbability = v, 50, 100);
            AddNumber(flow, "纹理最小归一化对比度", p => p.AdaptiveTextureMinimumContrast, (p, v) => p.AdaptiveTextureMinimumContrast = v, 0.10m, 20, 2);
            AddRatio(flow, "纹理自适应分位", p => p.AdaptiveTexturePercentile, (p, v) => p.AdaptiveTexturePercentile = v, 80, 99.99m);
            AddNumber(flow, "正常候选安全系数", p => p.AdaptiveCandidateSafetyFactor, (p, v) => p.AdaptiveCandidateSafetyFactor = v, 1, 5, 2);
            AddNumber(flow, "NG异常倍数", p => p.AdaptiveNgMultiplier, (p, v) => p.AdaptiveNgMultiplier = v, 1, 10, 2);
            AddRatio(flow, "明显缺形NG最小占比", p => p.AdaptiveShapeNgRatioMinimum, (p, v) => p.AdaptiveShapeNgRatioMinimum = v, 0, 100);
            AddRatio(flow, "明显纹理NG最小占比", p => p.AdaptiveTextureNgRatioMinimum, (p, v) => p.AdaptiveTextureNgRatioMinimum = v, 0, 100);
            AddRatio(flow, "提示框扩展/银区短边", p => p.AdaptiveBoxPaddingRatio, (p, v) => p.AdaptiveBoxPaddingRatio = v, 0, 25);
            AddHelp(flow, "形态学核尺寸由银区短边和比例自动计算，不再使用固定像素核。正常范围由确认OK图片生成；调节这些参数后应重新生成正常模型。");
            advancedFlowPanel.Controls.Add(adaptive);

            GroupBox visibility = CreateGroup("物料完整显示", 630, 300);
            FlowLayoutPanel visibilityFlow = CreateVerticalFlow();
            visibility.Controls.Add(visibilityFlow);
            AddInteger(visibilityFlow, "画面边界检查宽度", p => p.ModuleVisibleMargin, (p, v) => p.ModuleVisibleMargin = v, 0, 100);
            AddRatio(visibilityFlow, "边界深色占比上限", p => p.ModuleBorderDarkRatio, (p, v) => p.ModuleBorderDarkRatio = v, 0, 100);
            AddHelp(visibilityFlow, "只检查物料是否严重超出画面，不检查轻微角度和中心偏移。");
            advancedFlowPanel.Controls.Add(visibility);

            GroupBox middle = CreateGroup("中间残银", 630, 360);
            FlowLayoutPanel middleFlow = CreateVerticalFlow();
            middle.Controls.Add(middleFlow);
            AddInteger(middleFlow, "中间阈值偏移", p => p.MiddleSilverThresholdOffset, (p, v) => p.MiddleSilverThresholdOffset = v, 0, 255);
            AddInteger(middleFlow, "中间阈值下限", p => p.MiddleSilverThresholdMinimum, (p, v) => p.MiddleSilverThresholdMinimum = v, 0, 255);
            AddInteger(middleFlow, "中间阈值上限", p => p.MiddleSilverThresholdMaximum, (p, v) => p.MiddleSilverThresholdMaximum = v, 0, 255);
            advancedFlowPanel.Controls.Add(middle);
        }

        private void BuildDecisionPage()
        {
            BuildAdaptiveDecisionPage();
        }

        private void BuildLegacyDecisionPage()
        {
            GroupBox highBad = CreateGroup("数值越高越差", 630, 410);
            FlowLayoutPanel flow = CreateVerticalFlow();
            highBad.Controls.Add(flow);
            AddRatioPair(flow, "中间串银", p => p.MiddleSilverReviewRatio, (p, v) => p.MiddleSilverReviewRatio = v,
                p => p.MiddleSilverNgRatio, (p, v) => p.MiddleSilverNgRatio = v);
            AddRatioPair(flow, "最大缺银块", p => p.MissingSilverReviewRatio, (p, v) => p.MissingSilverReviewRatio = v,
                p => p.MissingSilverNgRatio, (p, v) => p.MissingSilverNgRatio = v);
            AddRatioPair(flow, "银面内部暗缺陷", p => p.InnerDefectReviewRatio, (p, v) => p.InnerDefectReviewRatio = v,
                p => p.InnerDefectNgRatio, (p, v) => p.InnerDefectNgRatio = v);
            AddRatioPair(flow, "边缘缺银/崩边", p => p.EdgeMissingReviewRatio, (p, v) => p.EdgeMissingReviewRatio = v,
                p => p.EdgeMissingNgRatio, (p, v) => p.EdgeMissingNgRatio = v);
            AddRatioPair(flow, "线状暗沟", p => p.LineDefectReviewRatio, (p, v) => p.LineDefectReviewRatio = v,
                p => p.LineDefectNgRatio, (p, v) => p.LineDefectNgRatio = v);
            AddRatioPair(flow, "边缘溢银", p => p.EdgeSilverReviewRatio, (p, v) => p.EdgeSilverReviewRatio = v,
                p => p.EdgeSilverNgRatio, (p, v) => p.EdgeSilverNgRatio = v);
            decisionFlowPanel.Controls.Add(highBad);

            GroupBox coverage = CreateGroup("银面覆盖率", 630, 300);
            FlowLayoutPanel coverageFlow = CreateVerticalFlow();
            coverage.Controls.Add(coverageFlow);
            AddRatio(coverageFlow, "顶部低于此值为NG", p => p.SilverTopCoverageNgRatio, (p, v) => p.SilverTopCoverageNgRatio = v, 0, 100);
            AddRatio(coverageFlow, "底部低于此值为NG", p => p.SilverBottomCoverageNgRatio, (p, v) => p.SilverBottomCoverageNgRatio = v, 0, 100);
            AddRatio(coverageFlow, "达到此值为OK", p => p.SilverCoverageOkRatio, (p, v) => p.SilverCoverageOkRatio = v, 0, 100);
            AddHelp(coverageFlow, "NG线与OK线之间判定为待复检；待复检不会自动放行为OK。");
            decisionFlowPanel.Controls.Add(coverage);
        }

        private void BuildAdvancedPage()
        {
            BuildAdaptiveAdvancedPage();
        }

        private void BuildLegacyAdvancedPage()
        {
            GroupBox thresholds = CreateGroup("基础灰度与二值化", 630, 380);
            FlowLayoutPanel flow = CreateVerticalFlow();
            thresholds.Controls.Add(flow);
            AddInteger(flow, "银面灰度阈值", p => p.SilverGrayThreshold, (p, v) => p.SilverGrayThreshold = v, 0, 255);
            AddInteger(flow, "边缘溢银灰度阈值", p => p.EdgeSilverGrayThreshold, (p, v) => p.EdgeSilverGrayThreshold = v, 0, 255);
            AddInteger(flow, "中间阈值偏移", p => p.MiddleSilverThresholdOffset, (p, v) => p.MiddleSilverThresholdOffset = v, 0, 255);
            AddInteger(flow, "中间阈值下限", p => p.MiddleSilverThresholdMinimum, (p, v) => p.MiddleSilverThresholdMinimum = v, 0, 255);
            AddInteger(flow, "中间阈值上限", p => p.MiddleSilverThresholdMaximum, (p, v) => p.MiddleSilverThresholdMaximum = v, 0, 255);
            AddInteger(flow, "缺银暗像素阈值", p => p.MissingDarkThreshold, (p, v) => p.MissingDarkThreshold = v, 0, 255);
            AddInteger(flow, "缺银边界忽略像素", p => p.MissingBoundaryMargin, (p, v) => p.MissingBoundaryMargin = v, 0, 100);
            advancedFlowPanel.Controls.Add(thresholds);

            GroupBox darkDefect = CreateGroup("银面暗缺陷（Black Hat）", 630, 570);
            FlowLayoutPanel darkFlow = CreateVerticalFlow();
            darkDefect.Controls.Add(darkFlow);
            AddInteger(darkFlow, "强暗灰度阈值", p => p.StrongDarkGrayThreshold, (p, v) => p.StrongDarkGrayThreshold = v, 0, 255);
            AddInteger(darkFlow, "局部对比度阈值", p => p.LocalContrastThreshold, (p, v) => p.LocalContrastThreshold = v, 0, 255);
            AddInteger(darkFlow, "局部背景核尺寸", p => p.DefectBackgroundKernelSize, (p, v) => p.DefectBackgroundKernelSize = v, 3, 255);
            AddInteger(darkFlow, "形态学核尺寸", p => p.DefectMorphKernelSize, (p, v) => p.DefectMorphKernelSize = v, 3, 31);
            AddInteger(darkFlow, "缺陷框扩展像素", p => p.DefectBoxPadding, (p, v) => p.DefectBoxPadding = v, 0, 200);
            AddInteger(darkFlow, "缺陷框最小宽度", p => p.DefectBoxMinimumWidth, (p, v) => p.DefectBoxMinimumWidth = v, 1, 500);
            AddInteger(darkFlow, "缺陷框最小高度", p => p.DefectBoxMinimumHeight, (p, v) => p.DefectBoxMinimumHeight = v, 1, 500);
            AddInteger(darkFlow, "每侧最多缺陷数", p => p.MaximumDefectsPerSilverRegion, (p, v) => p.MaximumDefectsPerSilverRegion = v, 1, 20);
            AddHelp(darkFlow, "强暗阈值筛选绝对灰度；局部对比度用于排除正常银面纹理；背景核应明显大于目标暗孔。偶数核保存时会自动转为奇数。");
            advancedFlowPanel.Controls.Add(darkDefect);

            GroupBox edgeDefect = CreateGroup("边缘缺银与完整显示", 630, 670);
            FlowLayoutPanel edgeFlow = CreateVerticalFlow();
            edgeDefect.Controls.Add(edgeFlow);
            AddInteger(edgeFlow, "边缘银面灰度阈值", p => p.EdgeMaskGrayThreshold, (p, v) => p.EdgeMaskGrayThreshold = v, 0, 255);
            AddInteger(edgeFlow, "单侧轮廓闭合核", p => p.EdgeMaskCloseKernelSize, (p, v) => p.EdgeMaskCloseKernelSize = v, 3, 255);
            AddInteger(edgeFlow, "左右比较闭合核", p => p.PairedEdgeCloseKernelSize, (p, v) => p.PairedEdgeCloseKernelSize = v, 3, 255);
            AddInteger(edgeFlow, "边缘检测深度", p => p.EdgeBandDepth, (p, v) => p.EdgeBandDepth = v, 1, 300);
            AddInteger(edgeFlow, "轮廓接触深度", p => p.EdgeBoundaryContactDepth, (p, v) => p.EdgeBoundaryContactDepth = v, 1, 100);
            AddInteger(edgeFlow, "边缘最小缺陷面积", p => p.EdgeMinimumDefectArea, (p, v) => p.EdgeMinimumDefectArea = v, 1, 100000);
            AddInteger(edgeFlow, "边缘最小宽度", p => p.EdgeMinimumDefectWidth, (p, v) => p.EdgeMinimumDefectWidth = v, 1, 500);
            AddInteger(edgeFlow, "边缘最小高度", p => p.EdgeMinimumDefectHeight, (p, v) => p.EdgeMinimumDefectHeight = v, 1, 500);
            AddRatio(edgeFlow, "边缘最小填充率", p => p.EdgeMinimumDefectFillRatio, (p, v) => p.EdgeMinimumDefectFillRatio = v, 0, 100);
            AddInteger(edgeFlow, "边缘框扩展像素", p => p.EdgeBoxPadding, (p, v) => p.EdgeBoxPadding = v, 0, 200);
            AddInteger(edgeFlow, "每侧最多边缘缺陷", p => p.MaximumEdgeDefectsPerSilverRegion, (p, v) => p.MaximumEdgeDefectsPerSilverRegion = v, 1, 20);
            AddInteger(edgeFlow, "画面完整显示边距", p => p.ModuleVisibleMargin, (p, v) => p.ModuleVisibleMargin = v, 0, 100);
            AddRatio(edgeFlow, "边界深色占比上限", p => p.ModuleBorderDarkRatio, (p, v) => p.ModuleBorderDarkRatio = v, 0, 100);
            AddHelp(edgeFlow, "物料外框接触画面边界时判定为未完整显示；不检查轻微角度、中心距或左右面积差。");
            advancedFlowPanel.Controls.Add(edgeDefect);

            GroupBox lineDefect = CreateGroup("线状暗沟", 630, 450);
            FlowLayoutPanel lineFlow = CreateVerticalFlow();
            lineDefect.Controls.Add(lineFlow);
            AddInteger(lineFlow, "线状局部对比度", p => p.LineContrastThreshold, (p, v) => p.LineContrastThreshold = v, 0, 255);
            AddInteger(lineFlow, "方向核长度", p => p.LineKernelLength, (p, v) => p.LineKernelLength = v, 3, 255);
            AddInteger(lineFlow, "最小线长度", p => p.LineMinimumLength, (p, v) => p.LineMinimumLength = v, 1, 1000);
            AddInteger(lineFlow, "最大线宽度", p => p.LineMaximumWidth, (p, v) => p.LineMaximumWidth = v, 1, 500);
            AddInteger(lineFlow, "最小线面积", p => p.LineMinimumArea, (p, v) => p.LineMinimumArea = v, 1, 100000);
            AddInteger(lineFlow, "线状框扩展像素", p => p.LineBoxPadding, (p, v) => p.LineBoxPadding = v, 0, 200);
            AddHelp(lineFlow, "分别使用横向与纵向 Black Hat 核，只保留长度足够且宽度受限的暗线候选。");
            advancedFlowPanel.Controls.Add(lineDefect);

            GroupBox filter = CreateGroup("暗缺陷连通域过滤", 630, 430);
            FlowLayoutPanel filterFlow = CreateVerticalFlow();
            filter.Controls.Add(filterFlow);
            AddInteger(filterFlow, "最小缺陷面积(px)", p => p.MinimumDefectArea, (p, v) => p.MinimumDefectArea = v, 1, 100000);
            AddRatio(filterFlow, "最小相对面积", p => p.MinimumDefectAreaRatio, (p, v) => p.MinimumDefectAreaRatio = v, 0, 10);
            AddInteger(filterFlow, "最小宽度", p => p.MinimumDefectWidth, (p, v) => p.MinimumDefectWidth = v, 1, 500);
            AddInteger(filterFlow, "最小高度", p => p.MinimumDefectHeight, (p, v) => p.MinimumDefectHeight = v, 1, 500);
            AddRatio(filterFlow, "最小填充率", p => p.MinimumDefectFillRatio, (p, v) => p.MinimumDefectFillRatio = v, 0, 100);
            AddNumber(filterFlow, "最小长宽比", p => p.MinimumDefectAspectRatio, (p, v) => p.MinimumDefectAspectRatio = v, 0.01m, 100, 2);
            AddNumber(filterFlow, "最大长宽比", p => p.MaximumDefectAspectRatio, (p, v) => p.MaximumDefectAspectRatio = v, 0.01m, 100, 2);
            advancedFlowPanel.Controls.Add(filter);
        }

        private void BuildCameraStoragePage()
        {
            GroupBox detection = CreateCameraGroup("检测相机", out detectionExposure, out detectionGain,
                out detectionRawPath, out detectionOkPath, out detectionNgPath);
            GroupBox classification = CreateCameraGroup("分类相机", out classificationExposure, out classificationGain,
                out classificationRawPath, out classificationOkPath, out classificationNgPath);
            cameraStorageFlowPanel.Controls.Add(detection);
            cameraStorageFlowPanel.Controls.Add(classification);
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

        private GroupBox CreateCameraGroup(
            string title,
            out NumericUpDown exposure,
            out NumericUpDown gain,
            out TextBox raw,
            out TextBox ok,
            out TextBox ng)
        {
            GroupBox group = CreateGroup(title, 650, 430);
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 3,
                RowCount = 5
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
            exposure = AddCameraNumber(table, 0, "曝光时间", 1, 1000000, 0);
            gain = AddCameraNumber(table, 1, "增益", 0, 24, 2);
            raw = AddPathRow(table, 2, "原图路径");
            ok = AddPathRow(table, 3, "OK图路径");
            ng = AddPathRow(table, 4, "NG/复检图路径");
            group.Controls.Add(table);
            return group;
        }

        private NumericUpDown AddCameraNumber(TableLayoutPanel table, int row, string label, decimal min, decimal max, int decimals)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            table.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
            var numeric = new NumericUpDown { Minimum = min, Maximum = max, DecimalPlaces = decimals, Dock = DockStyle.Fill, Margin = new Padding(3, 13, 3, 13) };
            table.Controls.Add(numeric, 1, row);
            return numeric;
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
                detectionExposure.Value = Clamp((decimal)parameters.DetectionCamera.Exposure, detectionExposure);
                detectionGain.Value = Clamp((decimal)parameters.DetectionCamera.Gain, detectionGain);
                classificationExposure.Value = Clamp((decimal)parameters.ClassificationCamera.Exposure, classificationExposure);
                classificationGain.Value = Clamp((decimal)parameters.ClassificationCamera.Gain, classificationGain);
                detectionRawPath.Text = parameters.DetectionCamera.RawImagePath;
                detectionOkPath.Text = parameters.DetectionCamera.OkImagePath;
                detectionNgPath.Text = parameters.DetectionCamera.NgImagePath;
                classificationRawPath.Text = parameters.ClassificationCamera.RawImagePath;
                classificationOkPath.Text = parameters.ClassificationCamera.OkImagePath;
                classificationNgPath.Text = parameters.ClassificationCamera.NgImagePath;
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
