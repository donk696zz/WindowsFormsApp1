using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 登录页面 : Form
    {
        private const string 有效账号 = "Toyou";
        private const string 有效密码 = "123";

        public event Action<int,string> LogAdded;
        public event Action 登录成功;

        public 登录页面()
        {
            InitializeComponent();
            textBox1.PasswordChar = '●';
            comboBox1.Items.Clear();
            comboBox1.Items.Add(有效账号);
            AcceptButton = button1;

            checkBox1.Checked = Properties.Settings.Default.显示密码的勾取;
            checkBox2.Checked = Properties.Settings.Default.保存密码的勾取;
            自动加载上次保存账号();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string 账号 = comboBox1.Text.Trim();
            string 密码 = textBox1.Text;

            if (string.IsNullOrEmpty(账号) || string.IsNullOrEmpty(密码))
            {
                LogAdded?.Invoke(0, "账号密码不能为空!");
                return;
            }

            if (!string.Equals(账号, 有效账号, StringComparison.Ordinal) ||
                !string.Equals(密码, 有效密码, StringComparison.Ordinal))
            {
                LogAdded?.Invoke(0, "账号或者密码错误!");
                return;
            }

            账号密码.账号 = 有效账号;
            账号密码.密码 = 有效密码;
            Properties.Settings.Default.账号 = 有效账号;
            Properties.Settings.Default.密码 = checkBox2.Checked ? 有效密码 : string.Empty;
            Properties.Settings.Default.Save();

            状态.登录权限 = true;
            timer1.Stop();
            timer1.Interval = VisionParameterStore.ApplicationParameters.PermissionTimeoutMinutes * 60 * 1000;
            timer1.Start();

            if (!checkBox2.Checked)
                textBox1.Clear();

            LogAdded?.Invoke(1, "登录成功，已进入参数页面。");
            登录成功?.Invoke();
        }
        public void 自动加载上次保存账号()
        {
            comboBox1.Text = 有效账号;
            if(checkBox2.Checked)
            {
                textBox1.Text = Properties.Settings.Default.密码;
            }
            else
            {
                textBox1.Text = null;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            // 如果用户取消勾选
            if (!checkBox1.Checked)
            {
                textBox1.PasswordChar = '●';
                Properties.Settings.Default.显示密码的勾取=false;
                Properties.Settings.Default.Save();
            }
            else 
            {
                textBox1.PasswordChar = '\0';
                Properties.Settings.Default.显示密码的勾取 = true;
                Properties.Settings.Default.Save();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            LogAdded?.Invoke(0, "当前使用固定登录密码，不能在此修改。");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            状态.登录权限 = false;
            LogAdded?.Invoke(0, "登录超时！权限时间到，请重新登录！");
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if(状态.登录权限&&string.IsNullOrEmpty(textBox1.Text))
            {
                textBox1.Text=账号密码.密码;
            }
            else
            {
               
            }
            if (!checkBox2.Checked)
            {
                Properties.Settings.Default.保存密码的勾取=false ;
                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.保存密码的勾取 = true;
                Properties.Settings.Default.Save();
            }
        }
    }
}
