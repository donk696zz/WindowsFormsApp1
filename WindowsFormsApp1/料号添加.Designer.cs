namespace WindowsFormsApp1
{
    partial class 料号添加
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel leftPanel;
        private System.Windows.Forms.Label listTitleLabel;
        private System.Windows.Forms.ListBox materialListBox;
        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label currentLabel;
        private System.Windows.Forms.TextBox materialTextBox;
        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.Button activateButton;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button refreshButton;
        private System.Windows.Forms.Label statusLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.leftPanel = new System.Windows.Forms.Panel();
            this.listTitleLabel = new System.Windows.Forms.Label();
            this.materialListBox = new System.Windows.Forms.ListBox();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.currentLabel = new System.Windows.Forms.Label();
            this.materialTextBox = new System.Windows.Forms.TextBox();
            this.createButton = new System.Windows.Forms.Button();
            this.activateButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.refreshButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.leftPanel.SuspendLayout();
            this.contentPanel.SuspendLayout();
            this.SuspendLayout();
            // left
            this.leftPanel.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.leftPanel.Controls.Add(this.listTitleLabel);
            this.leftPanel.Controls.Add(this.materialListBox);
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.leftPanel.Width = 360;
            this.listTitleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.listTitleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.listTitleLabel.Height = 62;
            this.listTitleLabel.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.listTitleLabel.Text = "料号列表";
            this.listTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.materialListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialListBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.materialListBox.IntegralHeight = false;
            this.materialListBox.SelectedIndexChanged += new System.EventHandler(this.materialListBox_SelectedIndexChanged);
            // content
            this.contentPanel.Controls.Add(this.titleLabel);
            this.contentPanel.Controls.Add(this.currentLabel);
            this.contentPanel.Controls.Add(this.materialTextBox);
            this.contentPanel.Controls.Add(this.createButton);
            this.contentPanel.Controls.Add(this.activateButton);
            this.contentPanel.Controls.Add(this.deleteButton);
            this.contentPanel.Controls.Add(this.refreshButton);
            this.contentPanel.Controls.Add(this.statusLabel);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 18F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(50, 55);
            this.titleLabel.Size = new System.Drawing.Size(330, 45);
            this.titleLabel.Text = "料号管理";
            this.currentLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F);
            this.currentLabel.Location = new System.Drawing.Point(54, 115);
            this.currentLabel.Size = new System.Drawing.Size(520, 34);
            this.materialTextBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 13F);
            this.materialTextBox.Location = new System.Drawing.Point(58, 175);
            this.materialTextBox.Size = new System.Drawing.Size(400, 30);
            this.createButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.createButton.Location = new System.Drawing.Point(480, 170);
            this.createButton.Size = new System.Drawing.Size(130, 42);
            this.createButton.Text = "新建料号";
            this.createButton.Click += new System.EventHandler(this.createButton_Click);
            this.activateButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.activateButton.Location = new System.Drawing.Point(58, 240);
            this.activateButton.Size = new System.Drawing.Size(150, 44);
            this.activateButton.Text = "设为当前料号";
            this.activateButton.Click += new System.EventHandler(this.activateButton_Click);
            this.deleteButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.deleteButton.Location = new System.Drawing.Point(224, 240);
            this.deleteButton.Size = new System.Drawing.Size(120, 44);
            this.deleteButton.Text = "删除料号";
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            this.refreshButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.refreshButton.Location = new System.Drawing.Point(360, 240);
            this.refreshButton.Size = new System.Drawing.Size(120, 44);
            this.refreshButton.Text = "刷新";
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            this.statusLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.statusLabel.ForeColor = System.Drawing.Color.DimGray;
            this.statusLabel.Location = new System.Drawing.Point(58, 315);
            this.statusLabel.Size = new System.Drawing.Size(650, 80);
            this.statusLabel.Text = "框比例请在“料号设置”页面调节。";
            // form
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1406, 791);
            this.ControlBox = false;
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.leftPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "料号添加";
            this.Text = "料号管理";
            this.Load += new System.EventHandler(this.料号添加_Load);
            this.leftPanel.ResumeLayout(false);
            this.contentPanel.ResumeLayout(false);
            this.contentPanel.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
