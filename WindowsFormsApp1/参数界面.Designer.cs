namespace WindowsFormsApp1
{
    partial class 参数界面
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label currentMaterialLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button resetButton;
        private System.Windows.Forms.TabControl parameterTabs;
        private System.Windows.Forms.TabPage decisionTab;
        private System.Windows.Forms.TabPage advancedTab;
        private System.Windows.Forms.TabPage cameraStorageTab;
        private System.Windows.Forms.TabPage systemTab;
        private System.Windows.Forms.FlowLayoutPanel decisionFlowPanel;
        private System.Windows.Forms.FlowLayoutPanel advancedFlowPanel;
        private System.Windows.Forms.FlowLayoutPanel cameraStorageFlowPanel;
        private System.Windows.Forms.FlowLayoutPanel systemFlowPanel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.CheckBox livePreviewCheckBox;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.headerPanel = new System.Windows.Forms.Panel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.currentMaterialLabel = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.resetButton = new System.Windows.Forms.Button();
            this.parameterTabs = new System.Windows.Forms.TabControl();
            this.decisionTab = new System.Windows.Forms.TabPage();
            this.advancedTab = new System.Windows.Forms.TabPage();
            this.cameraStorageTab = new System.Windows.Forms.TabPage();
            this.systemTab = new System.Windows.Forms.TabPage();
            this.decisionFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.advancedFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.cameraStorageFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.systemFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.livePreviewCheckBox = new System.Windows.Forms.CheckBox();
            this.headerPanel.SuspendLayout();
            this.parameterTabs.SuspendLayout();
            this.decisionTab.SuspendLayout();
            this.advancedTab.SuspendLayout();
            this.cameraStorageTab.SuspendLayout();
            this.systemTab.SuspendLayout();
            this.SuspendLayout();
            // header
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.headerPanel.Controls.Add(this.titleLabel);
            this.headerPanel.Controls.Add(this.currentMaterialLabel);
            this.headerPanel.Controls.Add(this.saveButton);
            this.headerPanel.Controls.Add(this.resetButton);
            this.headerPanel.Controls.Add(this.livePreviewCheckBox);
            this.headerPanel.Controls.Add(this.statusLabel);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Height = 76;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(18, 8);
            this.titleLabel.Size = new System.Drawing.Size(220, 38);
            this.titleLabel.Text = "检测参数中心";
            this.currentMaterialLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.currentMaterialLabel.ForeColor = System.Drawing.Color.DimGray;
            this.currentMaterialLabel.Location = new System.Drawing.Point(22, 45);
            this.currentMaterialLabel.Size = new System.Drawing.Size(310, 24);
            this.saveButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.saveButton.Location = new System.Drawing.Point(350, 15);
            this.saveButton.Size = new System.Drawing.Size(130, 42);
            this.saveButton.Text = "保存参数";
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            this.resetButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.resetButton.Location = new System.Drawing.Point(494, 15);
            this.resetButton.Size = new System.Drawing.Size(130, 42);
            this.resetButton.Text = "恢复默认";
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
            this.statusLabel.AutoEllipsis = true;
            this.statusLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.statusLabel.ForeColor = System.Drawing.Color.DimGray;
            this.statusLabel.Location = new System.Drawing.Point(850, 14);
            this.statusLabel.Size = new System.Drawing.Size(520, 44);
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.livePreviewCheckBox.AutoSize = true;
            this.livePreviewCheckBox.Checked = true;
            this.livePreviewCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.livePreviewCheckBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F);
            this.livePreviewCheckBox.Location = new System.Drawing.Point(650, 25);
            this.livePreviewCheckBox.Name = "livePreviewCheckBox";
            this.livePreviewCheckBox.Size = new System.Drawing.Size(190, 24);
            this.livePreviewCheckBox.Text = "实时预览当前调试图";
            this.livePreviewCheckBox.UseVisualStyleBackColor = true;
            this.livePreviewCheckBox.CheckedChanged += new System.EventHandler(this.livePreviewCheckBox_CheckedChanged);
            // tabs
            this.parameterTabs.Controls.Add(this.decisionTab);
            this.parameterTabs.Controls.Add(this.advancedTab);
            this.parameterTabs.Controls.Add(this.cameraStorageTab);
            this.parameterTabs.Controls.Add(this.systemTab);
            this.parameterTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.parameterTabs.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.parameterTabs.Location = new System.Drawing.Point(0, 76);
            this.decisionTab.Controls.Add(this.decisionFlowPanel);
            this.decisionTab.Text = "OK/复检/NG判定";
            this.advancedTab.Controls.Add(this.advancedFlowPanel);
            this.advancedTab.Text = "图像处理高级";
            this.cameraStorageTab.Controls.Add(this.cameraStorageFlowPanel);
            this.cameraStorageTab.Text = "相机与存储";
            this.systemTab.Controls.Add(this.systemFlowPanel);
            this.systemTab.Text = "系统";
            // flow panels
            this.decisionFlowPanel.AutoScroll = true;
            this.decisionFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.decisionFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.decisionFlowPanel.Name = "decisionFlowPanel";
            this.decisionFlowPanel.Padding = new System.Windows.Forms.Padding(16);
            this.decisionFlowPanel.WrapContents = true;
            this.advancedFlowPanel.AutoScroll = true;
            this.advancedFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.advancedFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.advancedFlowPanel.Name = "advancedFlowPanel";
            this.advancedFlowPanel.Padding = new System.Windows.Forms.Padding(16);
            this.advancedFlowPanel.WrapContents = true;
            this.cameraStorageFlowPanel.AutoScroll = true;
            this.cameraStorageFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cameraStorageFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.cameraStorageFlowPanel.Name = "cameraStorageFlowPanel";
            this.cameraStorageFlowPanel.Padding = new System.Windows.Forms.Padding(16);
            this.cameraStorageFlowPanel.WrapContents = true;
            this.systemFlowPanel.AutoScroll = true;
            this.systemFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.systemFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.systemFlowPanel.Name = "systemFlowPanel";
            this.systemFlowPanel.Padding = new System.Windows.Forms.Padding(16);
            this.systemFlowPanel.WrapContents = true;
            // form
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1406, 791);
            this.ControlBox = false;
            this.Controls.Add(this.parameterTabs);
            this.Controls.Add(this.headerPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "参数界面";
            this.Text = "参数界面";
            this.Load += new System.EventHandler(this.参数界面_Load);
            this.headerPanel.ResumeLayout(false);
            this.parameterTabs.ResumeLayout(false);
            this.decisionTab.ResumeLayout(false);
            this.advancedTab.ResumeLayout(false);
            this.cameraStorageTab.ResumeLayout(false);
            this.systemTab.ResumeLayout(false);
            this.ResumeLayout(false);
        }

    }
}
