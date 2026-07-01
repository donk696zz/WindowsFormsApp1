namespace WindowsFormsApp1
{
    partial class 自动页面
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer leftVerticalSplit;
        private System.Windows.Forms.SplitContainer splitContainerStats;
        private System.Windows.Forms.SplitContainer splitContainerImages;
        private System.Windows.Forms.Panel detectionStatsPanel;
        private System.Windows.Forms.Panel classificationStatsPanel;
        private System.Windows.Forms.Label detectionTitleLabel;
        private System.Windows.Forms.Label classificationTitleLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.TextBox textBox6;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button1;
        private halcon halcon1;
        private halcon halcon2;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.leftVerticalSplit = new System.Windows.Forms.SplitContainer();
            this.splitContainerStats = new System.Windows.Forms.SplitContainer();
            this.detectionStatsPanel = new System.Windows.Forms.Panel();
            this.detectionTitleLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.classificationStatsPanel = new System.Windows.Forms.Panel();
            this.classificationTitleLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.splitContainerImages = new System.Windows.Forms.SplitContainer();
            this.halcon1 = new WindowsFormsApp1.halcon();
            this.halcon2 = new WindowsFormsApp1.halcon();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.leftVerticalSplit)).BeginInit();
            this.leftVerticalSplit.Panel1.SuspendLayout();
            this.leftVerticalSplit.Panel2.SuspendLayout();
            this.leftVerticalSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerStats)).BeginInit();
            this.splitContainerStats.Panel1.SuspendLayout();
            this.splitContainerStats.Panel2.SuspendLayout();
            this.splitContainerStats.SuspendLayout();
            this.detectionStatsPanel.SuspendLayout();
            this.classificationStatsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerImages)).BeginInit();
            this.splitContainerImages.Panel1.SuspendLayout();
            this.splitContainerImages.Panel2.SuspendLayout();
            this.splitContainerImages.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.leftVerticalSplit);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.button1);
            this.splitContainer1.Size = new System.Drawing.Size(1406, 791);
            this.splitContainer1.SplitterDistance = 1377;
            this.splitContainer1.TabIndex = 0;
            // 
            // leftVerticalSplit
            // 
            this.leftVerticalSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftVerticalSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.leftVerticalSplit.Location = new System.Drawing.Point(0, 0);
            this.leftVerticalSplit.Name = "leftVerticalSplit";
            this.leftVerticalSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // leftVerticalSplit.Panel1
            // 
            this.leftVerticalSplit.Panel1.Controls.Add(this.splitContainerStats);
            // 
            // leftVerticalSplit.Panel2
            // 
            this.leftVerticalSplit.Panel2.Controls.Add(this.splitContainerImages);
            this.leftVerticalSplit.Size = new System.Drawing.Size(1377, 791);
            this.leftVerticalSplit.SplitterDistance = 71;
            this.leftVerticalSplit.TabIndex = 0;
            // 
            // splitContainerStats
            // 
            this.splitContainerStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerStats.Location = new System.Drawing.Point(0, 0);
            this.splitContainerStats.Name = "splitContainerStats";
            // 
            // splitContainerStats.Panel1
            // 
            this.splitContainerStats.Panel1.Controls.Add(this.detectionStatsPanel);
            // 
            // splitContainerStats.Panel2
            // 
            this.splitContainerStats.Panel2.Controls.Add(this.classificationStatsPanel);
            this.splitContainerStats.Size = new System.Drawing.Size(1377, 71);
            this.splitContainerStats.SplitterDistance = 703;
            this.splitContainerStats.TabIndex = 0;
            // 
            // detectionStatsPanel
            // 
            this.detectionStatsPanel.Controls.Add(this.detectionTitleLabel);
            this.detectionStatsPanel.Controls.Add(this.label1);
            this.detectionStatsPanel.Controls.Add(this.textBox5);
            this.detectionStatsPanel.Controls.Add(this.textBox4);
            this.detectionStatsPanel.Controls.Add(this.textBox6);
            this.detectionStatsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.detectionStatsPanel.Location = new System.Drawing.Point(0, 0);
            this.detectionStatsPanel.Name = "detectionStatsPanel";
            this.detectionStatsPanel.Size = new System.Drawing.Size(703, 71);
            this.detectionStatsPanel.TabIndex = 0;
            // 
            // detectionTitleLabel
            // 
            this.detectionTitleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 13F, System.Drawing.FontStyle.Bold);
            this.detectionTitleLabel.Location = new System.Drawing.Point(15, 12);
            this.detectionTitleLabel.Name = "detectionTitleLabel";
            this.detectionTitleLabel.Size = new System.Drawing.Size(170, 35);
            this.detectionTitleLabel.TabIndex = 0;
            this.detectionTitleLabel.Text = "检测相机";
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.DimGray;
            this.label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(15, 55);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(150, 55);
            this.label1.TabIndex = 1;
            this.label1.Text = "NA";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBox5
            // 
            this.textBox5.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.textBox5.Location = new System.Drawing.Point(205, 68);
            this.textBox5.Name = "textBox5";
            this.textBox5.ReadOnly = true;
            this.textBox5.Size = new System.Drawing.Size(90, 31);
            this.textBox5.TabIndex = 2;
            this.textBox5.Text = "总数  0";
            this.textBox5.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox4
            // 
            this.textBox4.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.textBox4.Location = new System.Drawing.Point(325, 68);
            this.textBox4.Name = "textBox4";
            this.textBox4.ReadOnly = true;
            this.textBox4.Size = new System.Drawing.Size(90, 31);
            this.textBox4.TabIndex = 3;
            this.textBox4.Text = "OK  0";
            this.textBox4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox6
            // 
            this.textBox6.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.textBox6.Location = new System.Drawing.Point(425, 68);
            this.textBox6.Name = "textBox6";
            this.textBox6.ReadOnly = true;
            this.textBox6.Size = new System.Drawing.Size(90, 31);
            this.textBox6.TabIndex = 4;
            this.textBox6.Text = "NG  0";
            this.textBox6.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // classificationStatsPanel
            // 
            this.classificationStatsPanel.Controls.Add(this.classificationTitleLabel);
            this.classificationStatsPanel.Controls.Add(this.label2);
            this.classificationStatsPanel.Controls.Add(this.textBox2);
            this.classificationStatsPanel.Controls.Add(this.textBox3);
            this.classificationStatsPanel.Controls.Add(this.textBox1);
            this.classificationStatsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.classificationStatsPanel.Location = new System.Drawing.Point(0, 0);
            this.classificationStatsPanel.Name = "classificationStatsPanel";
            this.classificationStatsPanel.Size = new System.Drawing.Size(670, 71);
            this.classificationStatsPanel.TabIndex = 0;
            // 
            // classificationTitleLabel
            // 
            this.classificationTitleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 13F, System.Drawing.FontStyle.Bold);
            this.classificationTitleLabel.Location = new System.Drawing.Point(15, 12);
            this.classificationTitleLabel.Name = "classificationTitleLabel";
            this.classificationTitleLabel.Size = new System.Drawing.Size(170, 35);
            this.classificationTitleLabel.TabIndex = 0;
            this.classificationTitleLabel.Text = "分类相机";
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.DimGray;
            this.label2.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(15, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(150, 55);
            this.label2.TabIndex = 1;
            this.label2.Text = "NA";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBox2
            // 
            this.textBox2.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.textBox2.Location = new System.Drawing.Point(205, 68);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(90, 31);
            this.textBox2.TabIndex = 2;
            this.textBox2.Text = "总数  0";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox3
            // 
            this.textBox3.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.textBox3.Location = new System.Drawing.Point(325, 68);
            this.textBox3.Name = "textBox3";
            this.textBox3.ReadOnly = true;
            this.textBox3.Size = new System.Drawing.Size(90, 31);
            this.textBox3.TabIndex = 3;
            this.textBox3.Text = "OK  0";
            this.textBox3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F);
            this.textBox1.Location = new System.Drawing.Point(425, 68);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(90, 31);
            this.textBox1.TabIndex = 4;
            this.textBox1.Text = "NG  0";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // splitContainerImages
            // 
            this.splitContainerImages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerImages.Location = new System.Drawing.Point(0, 0);
            this.splitContainerImages.Name = "splitContainerImages";
            // 
            // splitContainerImages.Panel1
            // 
            this.splitContainerImages.Panel1.Controls.Add(this.halcon1);
            // 
            // splitContainerImages.Panel2
            // 
            this.splitContainerImages.Panel2.Controls.Add(this.halcon2);
            this.splitContainerImages.Size = new System.Drawing.Size(1377, 716);
            this.splitContainerImages.SplitterDistance = 703;
            this.splitContainerImages.TabIndex = 0;
            // 
            // halcon1
            // 
            this.halcon1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.halcon1.Location = new System.Drawing.Point(0, 0);
            this.halcon1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.halcon1.Name = "halcon1";
            this.halcon1.Size = new System.Drawing.Size(703, 716);
            this.halcon1.TabIndex = 0;
            // 
            // halcon2
            // 
            this.halcon2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.halcon2.Location = new System.Drawing.Point(0, 0);
            this.halcon2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.halcon2.Name = "halcon2";
            this.halcon2.Size = new System.Drawing.Size(670, 716);
            this.halcon2.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.IndianRed;
            this.button1.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(30, 35);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(170, 75);
            this.button1.TabIndex = 0;
            this.button1.Text = "开始检测";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // 自动页面
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1406, 791);
            this.ControlBox = false;
            this.Controls.Add(this.splitContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "自动页面";
            this.Text = "自动检测";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.leftVerticalSplit.Panel1.ResumeLayout(false);
            this.leftVerticalSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.leftVerticalSplit)).EndInit();
            this.leftVerticalSplit.ResumeLayout(false);
            this.splitContainerStats.Panel1.ResumeLayout(false);
            this.splitContainerStats.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerStats)).EndInit();
            this.splitContainerStats.ResumeLayout(false);
            this.detectionStatsPanel.ResumeLayout(false);
            this.detectionStatsPanel.PerformLayout();
            this.classificationStatsPanel.ResumeLayout(false);
            this.classificationStatsPanel.PerformLayout();
            this.splitContainerImages.Panel1.ResumeLayout(false);
            this.splitContainerImages.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerImages)).EndInit();
            this.splitContainerImages.ResumeLayout(false);
            this.ResumeLayout(false);

        }

    }
}
