namespace WindowsFormsApp1
{
    partial class 料号设置页面
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Label materialLabel;
        private System.Windows.Forms.ComboBox materialComboBox;
        private System.Windows.Forms.Button selectImageButton;
        private System.Windows.Forms.Button saveDetectionImageButton;
        private System.Windows.Forms.Button resetRegionButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.SplitContainer contentSplitContainer;
        private System.Windows.Forms.PictureBox imagePictureBox;
        private System.Windows.Forms.TabControl regionTabControl;
        private System.Windows.Forms.TabPage mainRegionTabPage;
        private System.Windows.Forms.TabPage locatorTabPage;
        private System.Windows.Forms.FlowLayoutPanel mainRegionFlowPanel;
        private System.Windows.Forms.FlowLayoutPanel locatorFlowPanel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.topPanel = new System.Windows.Forms.Panel();
            this.materialLabel = new System.Windows.Forms.Label();
            this.materialComboBox = new System.Windows.Forms.ComboBox();
            this.selectImageButton = new System.Windows.Forms.Button();
            this.saveDetectionImageButton = new System.Windows.Forms.Button();
            this.resetRegionButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.contentSplitContainer = new System.Windows.Forms.SplitContainer();
            this.imagePictureBox = new System.Windows.Forms.PictureBox();
            this.regionTabControl = new System.Windows.Forms.TabControl();
            this.mainRegionTabPage = new System.Windows.Forms.TabPage();
            this.locatorTabPage = new System.Windows.Forms.TabPage();
            this.mainRegionFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.locatorFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.contentSplitContainer)).BeginInit();
            this.contentSplitContainer.Panel1.SuspendLayout();
            this.contentSplitContainer.Panel2.SuspendLayout();
            this.contentSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imagePictureBox)).BeginInit();
            this.regionTabControl.SuspendLayout();
            this.mainRegionTabPage.SuspendLayout();
            this.locatorTabPage.SuspendLayout();
            this.SuspendLayout();
            // topPanel
            this.topPanel.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.topPanel.Controls.Add(this.materialLabel);
            this.topPanel.Controls.Add(this.materialComboBox);
            this.topPanel.Controls.Add(this.selectImageButton);
            this.topPanel.Controls.Add(this.saveDetectionImageButton);
            this.topPanel.Controls.Add(this.resetRegionButton);
            this.topPanel.Controls.Add(this.statusLabel);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Height = 76;
            // materialLabel
            this.materialLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.materialLabel.Location = new System.Drawing.Point(16, 13);
            this.materialLabel.Size = new System.Drawing.Size(72, 34);
            this.materialLabel.Text = "料号";
            this.materialLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // materialComboBox
            this.materialComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.materialComboBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.materialComboBox.Location = new System.Drawing.Point(90, 15);
            this.materialComboBox.Size = new System.Drawing.Size(220, 29);
            this.materialComboBox.SelectedIndexChanged += new System.EventHandler(this.materialComboBox_SelectedIndexChanged);
            // selectImageButton
            this.selectImageButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.selectImageButton.Location = new System.Drawing.Point(330, 10);
            this.selectImageButton.Size = new System.Drawing.Size(140, 40);
            this.selectImageButton.Text = "选择检测图";
            this.selectImageButton.Click += new System.EventHandler(this.selectImageButton_Click);
            // saveDetectionImageButton
            this.saveDetectionImageButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.saveDetectionImageButton.Location = new System.Drawing.Point(482, 10);
            this.saveDetectionImageButton.Size = new System.Drawing.Size(150, 40);
            this.saveDetectionImageButton.Text = "保存检测图";
            this.saveDetectionImageButton.Click += new System.EventHandler(this.saveDetectionImageButton_Click);
            // resetRegionButton
            this.resetRegionButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.resetRegionButton.Location = new System.Drawing.Point(644, 10);
            this.resetRegionButton.Size = new System.Drawing.Size(140, 40);
            this.resetRegionButton.Text = "恢复默认框";
            this.resetRegionButton.Click += new System.EventHandler(this.resetRegionButton_Click);
            // statusLabel
            this.statusLabel.AutoEllipsis = true;
            this.statusLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.statusLabel.ForeColor = System.Drawing.Color.DimGray;
            this.statusLabel.Location = new System.Drawing.Point(806, 9);
            this.statusLabel.Size = new System.Drawing.Size(570, 45);
            this.statusLabel.Text = "选择检测图后，右侧调节框比例会实时预览。";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // contentSplitContainer
            this.contentSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentSplitContainer.Location = new System.Drawing.Point(0, 76);
            this.contentSplitContainer.SplitterDistance = 950;
            this.contentSplitContainer.SplitterWidth = 6;
            this.contentSplitContainer.Panel1.Controls.Add(this.imagePictureBox);
            this.contentSplitContainer.Panel2.Controls.Add(this.regionTabControl);
            // imagePictureBox
            this.imagePictureBox.BackColor = System.Drawing.Color.FromArgb(25, 25, 28);
            this.imagePictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.imagePictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imagePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            // regionTabControl
            this.regionTabControl.Controls.Add(this.mainRegionTabPage);
            this.regionTabControl.Controls.Add(this.locatorTabPage);
            this.regionTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.regionTabControl.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F);
            // mainRegionTabPage
            this.mainRegionTabPage.Controls.Add(this.mainRegionFlowPanel);
            this.mainRegionTabPage.Text = "框比例";
            // locatorTabPage
            this.locatorTabPage.Controls.Add(this.locatorFlowPanel);
            this.locatorTabPage.Text = "定位高级";
            // flow panels
            this.mainRegionFlowPanel.AutoScroll = true;
            this.mainRegionFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainRegionFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.mainRegionFlowPanel.Padding = new System.Windows.Forms.Padding(10);
            this.mainRegionFlowPanel.WrapContents = false;
            this.locatorFlowPanel.AutoScroll = true;
            this.locatorFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.locatorFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.locatorFlowPanel.Padding = new System.Windows.Forms.Padding(10);
            this.locatorFlowPanel.WrapContents = false;
            // form
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1406, 791);
            this.ControlBox = false;
            this.Controls.Add(this.contentSplitContainer);
            this.Controls.Add(this.topPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "料号设置页面";
            this.Text = "料号框设置";
            this.Load += new System.EventHandler(this.料号设置页面_Load);
            this.topPanel.ResumeLayout(false);
            this.contentSplitContainer.Panel1.ResumeLayout(false);
            this.contentSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.contentSplitContainer)).EndInit();
            this.contentSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.imagePictureBox)).EndInit();
            this.regionTabControl.ResumeLayout(false);
            this.mainRegionTabPage.ResumeLayout(false);
            this.locatorTabPage.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
