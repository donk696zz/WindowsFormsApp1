using System;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 料号添加 : Form
    {
        public 料号添加()
        {
            InitializeComponent();
        }

        private void 料号添加_Load(object sender, EventArgs e)
        {
            读取当前料号的数据();
        }

        public void 刷新料号列表()
        {
            string selected = materialListBox.SelectedItem?.ToString();
            var materials = 料号切换读取文件.获取所有料号();
            相机变量.料号集合 = materials;
            materialListBox.Items.Clear();
            materialListBox.Items.AddRange(materials.Cast<object>().ToArray());
            if (!string.IsNullOrWhiteSpace(selected) && materials.Contains(selected))
                materialListBox.SelectedItem = selected;
            else if (materials.Count > 0)
                materialListBox.SelectedIndex = 0;
            UpdateCurrentLabel();
        }

        public void 读取当前料号的数据()
        {
            刷新料号列表();
            string current = Properties.Settings.Default.当前料号;
            if (!string.IsNullOrWhiteSpace(current) && materialListBox.Items.Contains(current))
                materialListBox.SelectedItem = current;
            UpdateCurrentLabel();
        }

        public void 保存数据()
        {
            料号切换读取文件.保存当前料号修改(out string message);
            statusLabel.Text = message;
        }

        private void materialListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            materialTextBox.Text = materialListBox.SelectedItem?.ToString() ?? string.Empty;
        }

        private void createButton_Click(object sender, EventArgs e)
        {
            if (料号切换读取文件.保存全局变量为新料号(materialTextBox.Text, out string message))
            {
                刷新料号列表();
                materialListBox.SelectedItem = 数据变量.料号名称;
                NotifyMaterialChanged();
            }
            statusLabel.Text = message;
        }

        private void activateButton_Click(object sender, EventArgs e)
        {
            string material = materialListBox.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(material))
            {
                statusLabel.Text = "请选择料号";
                return;
            }
            bool ok = 料号切换读取文件.加载料号到全局变量(material, false);
            statusLabel.Text = ok ? "当前料号已切换为：" + material : "料号切换失败";
            if (ok) NotifyMaterialChanged();
            UpdateCurrentLabel();
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            string material = materialListBox.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(material)) return;
            if (MessageBox.Show("确定删除料号“" + material + "”？", "删除料号",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            bool deleted = 料号切换读取文件.删除料号(
                material, out string message);
            刷新料号列表();
            if (deleted)
            {
                string current = 数据变量.料号名称;
                if (!string.IsNullOrWhiteSpace(current) &&
                    materialListBox.Items.Contains(current))
                    materialListBox.SelectedItem = current;
                NotifyMaterialChanged();
            }
            statusLabel.Text = message;
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            刷新料号列表();
            statusLabel.Text = "料号列表已刷新";
        }

        private void NotifyMaterialChanged()
        {
            底层页面.liaohaoshezhijiemian?.ReloadMaterials();
            底层页面.canshujiemian?.刷新当前料号显示();
        }

        private void UpdateCurrentLabel()
        {
            currentLabel.Text = "当前生产料号：" +
                (string.IsNullOrWhiteSpace(数据变量.料号名称) ? "未选择" : 数据变量.料号名称);
        }
    }
}
