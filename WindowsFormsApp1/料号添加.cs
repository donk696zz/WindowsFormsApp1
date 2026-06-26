using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 料号添加 : Form
    {
        public event Action<int, string> LogAdded;
        public 料号添加()
        {
            InitializeComponent();
            刷新料号列表();
            读取当前料号的数据();
        }


        public void 刷新料号列表()
        {
            listBox1.Items.Clear();
            var list = 料号切换读取文件.获取所有料号();
            相机变量.料号集合 = list;
            listBox1.Items.AddRange(list.ToArray());
        }
        // 点击ListBox切换料号 → 自动显示到控件
        public void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool 只显示 = true;
            if (listBox1.SelectedItem == null) return;
            string 料号 = listBox1.SelectedItem.ToString();
            bool ok = 料号切换读取文件.加载料号到全局变量(料号,只显示);
            if (ok) 显示数据到控件(只显示);
        }
        // 全局变量 → 显示到所有文本框
        public void 显示数据到控件(bool 是否显示)
        {
            if (!是否显示)
            // 相机1
            {
                txt相机1曝光时间.Text = 数据变量.相机1曝光时间;
                txt相机1增益.Text = 数据变量.相机1增益;
                txt相机1模板数量.Text = 数据变量.相机1模板数量;
                txt相机1对比度.Text = 数据变量.相机1对比度;
                txt相机1匹配度.Text = 数据变量.相机1匹配度;
                txt相机1hdl.Text = 数据变量.相机1hdl文件路径;
                txt相机1hdict.Text = 数据变量.相机1hdict文件路径;
                txt相机1原图保存路径.Text = 数据变量.相机1原图保存路径;
                txt相机1OK图保存路径.Text = 数据变量.相机1OK图保存路径;
                txt相机1NG图保存路径.Text = 数据变量.相机1NG图保存路径;

                // 相机2
                txt相机2曝光时间.Text = 数据变量.相机2曝光时间;
                txt相机2增益.Text = 数据变量.相机2增益;
                txt相机2模板数量.Text = 数据变量.相机2模板数量;
                txt相机2对比度.Text = 数据变量.相机2对比度;
                txt相机2匹配度.Text = 数据变量.相机2匹配度;
                txt相机2hdl.Text = 数据变量.相机2hdl文件路径;
                txt相机2hdict.Text = 数据变量.相机2hdict文件路径;
                txt相机2原图保存路径.Text = 数据变量.相机2原图保存路径;
                txt相机2OK图保存路径.Text = 数据变量.相机2OK图保存路径;
                txt相机2NG图保存路径.Text = 数据变量.相机2NG图保存路径;

                // 相机3
                txt相机3曝光时间.Text = 数据变量.相机3曝光时间;
                txt相机3增益.Text = 数据变量.相机3增益;
                txt相机3模板数量.Text = 数据变量.相机3模板数量;
                txt相机3对比度.Text = 数据变量.相机3对比度;
                txt相机3匹配度.Text = 数据变量.相机3匹配度;
                txt相机3hdl.Text = 数据变量.相机3hdl文件路径;
                txt相机3hdict.Text = 数据变量.相机3hdict文件路径;
                txt相机3原图保存路径.Text = 数据变量.相机3原图保存路径;
                txt相机3OK图保存路径.Text = 数据变量.相机3OK图保存路径;
                txt相机3NG图保存路径.Text = 数据变量.相机3NG图保存路径;

                // 相机4
                txt相机4曝光时间.Text = 数据变量.相机4曝光时间;
                txt相机4增益.Text = 数据变量.相机4增益;
                txt相机4模板数量.Text = 数据变量.相机4模板数量;
                txt相机4对比度.Text = 数据变量.相机4对比度;
                txt相机4匹配度.Text = 数据变量.相机4匹配度;
                txt相机4hdl.Text = 数据变量.相机4hdl文件路径;
                txt相机4hdict.Text = 数据变量.相机4hdict文件路径;
                txt相机4原图保存路径.Text = 数据变量.相机4原图保存路径;
                txt相机4OK图保存路径.Text = 数据变量.相机4OK图保存路径;
                txt相机4NG图保存路径.Text = 数据变量.相机4NG图保存路径;

                txt相机1模板保存路径.Text = 数据变量.相机1模板保存路径;
                txt相机2模板保存路径.Text = 数据变量.相机2模板保存路径;
                txt相机3模板保存路径.Text = 数据变量.相机3模板保存路径;
                txt相机4模板保存路径.Text = 数据变量.相机4模板保存路径;

                // 系统
                txt料号名称.Text = 数据变量.料号名称;
                txt权限时间.Text = 数据变量.权限时间;
                txt图片删除日期.Text = 数据变量.图片删除日期;
                txt相机数量设置.Text = ClampCameraCountText(数据变量.相机数量设置);
            }
            else
            {
                txt相机1曝光时间.Text = 只显示.相机1曝光时间;
                txt相机1增益.Text = 只显示.相机1增益;
                txt相机1模板数量.Text = 只显示.相机1模板数量;
                txt相机1对比度.Text = 只显示.相机1对比度;
                txt相机1匹配度.Text = 只显示.相机1匹配度;
                txt相机1hdl.Text = 只显示.相机1hdl文件路径;
                txt相机1hdict.Text = 只显示.相机1hdict文件路径;
                txt相机1原图保存路径.Text = 只显示.相机1原图保存路径;
                txt相机1OK图保存路径.Text = 只显示.相机1OK图保存路径;
                txt相机1NG图保存路径.Text = 只显示.相机1NG图保存路径;

                // 相机2
                txt相机2曝光时间.Text = 只显示.相机2曝光时间;
                txt相机2增益.Text = 只显示.相机2增益;
                txt相机2模板数量.Text = 只显示.相机2模板数量;
                txt相机2对比度.Text = 只显示.相机2对比度;
                txt相机2匹配度.Text = 只显示.相机2匹配度;
                txt相机2hdl.Text = 只显示.相机2hdl文件路径;
                txt相机2hdict.Text = 只显示.相机2hdict文件路径;
                txt相机2原图保存路径.Text = 只显示.相机2原图保存路径;
                txt相机2OK图保存路径.Text = 只显示.相机2OK图保存路径;
                txt相机2NG图保存路径.Text = 只显示.相机2NG图保存路径;

                // 相机3
                txt相机3曝光时间.Text = 只显示.相机3曝光时间;
                txt相机3增益.Text = 只显示.相机3增益;
                txt相机3模板数量.Text = 只显示.相机3模板数量;
                txt相机3对比度.Text = 只显示.相机3对比度;
                txt相机3匹配度.Text = 只显示.相机3匹配度;
                txt相机3hdl.Text = 只显示.相机3hdl文件路径;
                txt相机3hdict.Text = 只显示.相机3hdict文件路径;
                txt相机3原图保存路径.Text = 只显示.相机3原图保存路径;
                txt相机3OK图保存路径.Text = 只显示.相机3OK图保存路径;
                txt相机3NG图保存路径.Text = 只显示.相机3NG图保存路径;

                // 相机4
                txt相机4曝光时间.Text = 只显示.相机4曝光时间;
                txt相机4增益.Text = 只显示.相机4增益;
                txt相机4模板数量.Text = 只显示.相机4模板数量;
                txt相机4对比度.Text = 只显示.相机4对比度;
                txt相机4匹配度.Text = 只显示.相机4匹配度;
                txt相机4hdl.Text = 只显示.相机4hdl文件路径;
                txt相机4hdict.Text = 只显示.相机4hdict文件路径;
                txt相机4原图保存路径.Text = 只显示.相机4原图保存路径;
                txt相机4OK图保存路径.Text = 只显示.相机4OK图保存路径;
                txt相机4NG图保存路径.Text = 只显示.相机4NG图保存路径;

                txt相机1模板保存路径.Text = 只显示.相机1模板保存路径;
                txt相机2模板保存路径.Text = 只显示.相机2模板保存路径;
                txt相机3模板保存路径.Text = 只显示.相机3模板保存路径;
                txt相机4模板保存路径.Text = 只显示.相机4模板保存路径;

                // 系统
                txt料号名称.Text = 只显示.料号名称;
                txt权限时间.Text = 只显示.权限时间;
                txt图片删除日期.Text = 只显示.图片删除日期;
                txt相机数量设置.Text = ClampCameraCountText(只显示.相机数量设置);
            }
        }

        private static string ClampCameraCountText(string value)
        {
            if (!int.TryParse(value, out int count))
                return "2";

            return Math.Max(1, Math.Min(2, count)).ToString();
        }

        // 控件数据 → 存入全局变量
        public void 控件数据存入全局变量()
        {
            数据变量.相机1曝光时间 = txt相机1曝光时间.Text;
            数据变量.相机1增益 = txt相机1增益.Text;
            数据变量.相机1模板数量 = txt相机1模板数量.Text;
            数据变量.相机1对比度 = txt相机1对比度.Text;
            数据变量.相机1匹配度 = txt相机1匹配度.Text;
            数据变量.相机1hdl文件路径 = txt相机1hdl.Text;
            数据变量.相机1hdict文件路径 = txt相机1hdict.Text;
            数据变量.相机1原图保存路径=txt相机1原图保存路径.Text;
            数据变量.相机1OK图保存路径=txt相机1OK图保存路径.Text;
            数据变量.相机1NG图保存路径=txt相机1NG图保存路径.Text;

            数据变量.相机2曝光时间 = txt相机2曝光时间.Text;
            数据变量.相机2增益 = txt相机2增益.Text;
            数据变量.相机2模板数量 = txt相机2模板数量.Text;
            数据变量.相机2对比度 = txt相机2对比度.Text;
            数据变量.相机2匹配度 = txt相机2匹配度.Text;
            数据变量.相机2hdl文件路径 = txt相机2hdl.Text;
            数据变量.相机2hdict文件路径 = txt相机2hdict.Text;
            数据变量.相机2原图保存路径 = txt相机2原图保存路径.Text;
            数据变量.相机2OK图保存路径 = txt相机2OK图保存路径.Text;
            数据变量.相机2NG图保存路径 = txt相机2NG图保存路径.Text;

            数据变量.相机3曝光时间 = txt相机3曝光时间.Text;
            数据变量.相机3增益 = txt相机3增益.Text;
            数据变量.相机3模板数量 = txt相机3模板数量.Text;
            数据变量.相机3对比度 = txt相机3对比度.Text;
            数据变量.相机3匹配度 = txt相机3匹配度.Text;
            数据变量.相机3hdl文件路径 = txt相机3hdl.Text;
            数据变量.相机3hdict文件路径 = txt相机3hdict.Text;
            数据变量.相机3原图保存路径 = txt相机3原图保存路径.Text;
            数据变量.相机3OK图保存路径 = txt相机3OK图保存路径.Text;
            数据变量.相机3NG图保存路径 = txt相机3NG图保存路径.Text;

            数据变量.相机4曝光时间 = txt相机4曝光时间.Text;
            数据变量.相机4增益 = txt相机4增益.Text;
            数据变量.相机4模板数量 = txt相机4模板数量.Text;
            数据变量.相机4对比度 = txt相机4对比度.Text;
            数据变量.相机4匹配度 = txt相机4匹配度.Text;
            数据变量.相机4hdl文件路径 = txt相机4hdl.Text;
            数据变量.相机4hdict文件路径 = txt相机4hdict.Text;
            数据变量.相机4原图保存路径 = txt相机4原图保存路径.Text;
            数据变量.相机4OK图保存路径 = txt相机4OK图保存路径.Text;
            数据变量.相机4NG图保存路径 = txt相机4NG图保存路径.Text;

            数据变量.相机1模板保存路径 = txt相机1模板保存路径.Text;
            数据变量.相机2模板保存路径 = txt相机2模板保存路径.Text;
            数据变量.相机3模板保存路径 = txt相机3模板保存路径.Text;
            数据变量.相机4模板保存路径 = txt相机4模板保存路径.Text;

            数据变量.料号名称 = txt料号名称.Text;
            数据变量.权限时间 = txt权限时间.Text;
            数据变量.图片删除日期 = txt图片删除日期.Text;
            数据变量.相机数量设置 = txt相机数量设置.Text;
        }

       public void button11_Click(object sender, EventArgs e)
        {
            控件数据存入全局变量();
            bool ok = 料号切换读取文件.保存当前料号修改(out string msg);
            MessageBox.Show(msg);
            底层页面.canshujiemian.第一次数据加载();
        }
        public void 保存数据()
        {
            bool 只显示=false;
            bool ok = 料号切换读取文件.保存当前料号修改(out string msg);
            显示数据到控件(只显示);
            MessageBox.Show(msg);

        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null)
            {
                MessageBox.Show("请选择要删除的料号");
                return;
            }
            string 料号 = listBox1.SelectedItem.ToString();
            if (MessageBox.Show($"确定删除：{料号}？", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                bool ok = 料号切换读取文件.删除料号(料号, out string msg);
                
                MessageBox.Show(msg);
                刷新料号列表();
                数据变量.料号名称 = 相机变量.料号集合[0];
                清空内容();
                底层页面.canshujiemian.第一次数据加载();
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            string 新料号 = txt料号名称.Text.Trim();
            if (string.IsNullOrEmpty(新料号))
            {
                MessageBox.Show("请输入新料号");
                return;
            }
            控件数据存入全局变量();
            bool ok = 料号切换读取文件.保存全局变量为新料号(新料号, out string msg);
            

            MessageBox.Show(msg);
            刷新料号列表();
            底层页面.canshujiemian.第一次数据加载();
        }
        public void 读取当前料号的数据()
        {
            // 1. 先获取配置里保存的当前料号
            string 料号 = Properties.Settings.Default.当前料号?.Trim();

            // 2. 获取所有料号列表
            List<string> allParts = 料号切换读取文件.获取所有料号();

            // 3. 如果配置里没有料号 或者 料号不存在 → 自动取第一个料号
            if (string.IsNullOrEmpty(料号) || !allParts.Contains(料号))
            {
                if (allParts.Count > 0)
                {
                    料号 = allParts[0]; // 取第一个料号
                }

            }

            // 4. 加载并显示
            bool 只显示 = false;
            bool ok = 料号切换读取文件.加载料号到全局变量(料号,只显示);
            if (ok)
            {
                
                显示数据到控件(只显示);
                Properties.Settings.Default.当前料号 = 料号;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "(*.hdl)|*.hdl";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机1hdl文件路径 = folderBrowserDialog.InitialDirectory + "相机1深度学习模板\\" + folderBrowserDialog.SafeFileName;
                    Properties.Settings.Default.Save();
                    txt相机1hdl.Text = Properties.Settings.Default.相机1hdl文件路径;
                    数据变量.相机1hdl文件路径 = Properties.Settings.Default.相机1hdl文件路径;
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机1路径错误!" + ex);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "(*.hdict)|*.hdict";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机1hdict文件路径 = folderBrowserDialog.InitialDirectory + "相机1深度学习模板\\" + folderBrowserDialog.SafeFileName;
                    Properties.Settings.Default.Save();
                    txt相机1hdict.Text = Properties.Settings.Default.相机1hdict文件路径;
                    数据变量.相机1hdict文件路径 = Properties.Settings.Default.相机1hdict文件路径;
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机1路径错误!" + ex);
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "(*.hdl)|*.hdl";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机3hdl文件路径 = folderBrowserDialog.InitialDirectory + "相机3深度学习模板\\" + folderBrowserDialog.SafeFileName;
                    Properties.Settings.Default.Save();
                    txt相机3hdl.Text = Properties.Settings.Default.相机3hdl文件路径;
                    数据变量.相机3hdl文件路径 = Properties.Settings.Default.相机3hdl文件路径;
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机3路径错误!" + ex);
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "(*.hdict)|*.hdict";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机3hdict文件路径 = folderBrowserDialog.InitialDirectory + "相机3深度学习模板\\" + folderBrowserDialog.SafeFileName;
                    Properties.Settings.Default.Save();
                    txt相机3hdict.Text = Properties.Settings.Default.相机3hdict文件路径;
                    数据变量.相机3hdict文件路径 = Properties.Settings.Default.相机3hdict文件路径;
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机3路径错误!" + ex);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机1配方地址 = folderBrowserDialog.SelectedPath;
                数据变量.相机1模板保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机1模板保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机3配方地址 = folderBrowserDialog.SelectedPath;
                数据变量.相机3模板保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机3模板保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "(*.hdl)|*.hdl";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机2hdl文件路径 = folderBrowserDialog.InitialDirectory + "相机2深度学习模板\\" + folderBrowserDialog.SafeFileName;
                    Properties.Settings.Default.Save();
                    txt相机2hdl.Text = Properties.Settings.Default.相机2hdl文件路径;
                    数据变量.相机2hdl文件路径 = Properties.Settings.Default.相机2hdl文件路径;
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机2路径错误!" + ex);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "(*.hdict)|*.hdict";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机2hdict文件路径 = folderBrowserDialog.InitialDirectory + "相机2深度学习模板\\" + folderBrowserDialog.SafeFileName;
                    Properties.Settings.Default.Save();
                    txt相机2hdict.Text = Properties.Settings.Default.相机2hdict文件路径;
                    数据变量.相机2hdict文件路径 = Properties.Settings.Default.相机2hdict文件路径;
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机2路径错误!" + ex);
                }
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "(*.hdl)|*.hdl";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机4hdl文件路径 = folderBrowserDialog.InitialDirectory + "相机4深度学习模板\\" + folderBrowserDialog.SafeFileName;
                    Properties.Settings.Default.Save();
                    txt相机4hdl.Text = Properties.Settings.Default.相机4hdl文件路径;
                    数据变量.相机4hdl文件路径 = Properties.Settings.Default.相机4hdl文件路径;
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机4路径错误!" + ex);
                }
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "(*.hdict)|*.hdict";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机4hdict文件路径 = folderBrowserDialog.InitialDirectory + "相机4深度学习模板\\" + folderBrowserDialog.SafeFileName;
                    Properties.Settings.Default.Save();
                    txt相机4hdict.Text = Properties.Settings.Default.相机4hdict文件路径;
                    数据变量.相机4hdict文件路径 = Properties.Settings.Default.相机4hdict文件路径;
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机4路径错误!" + ex);
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机2配方地址 = folderBrowserDialog.SelectedPath;
                数据变量.相机2模板保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机2模板保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机4配方地址 = folderBrowserDialog.SelectedPath;
                数据变量.相机4模板保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机4模板保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机1图片保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机1原图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机1原图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机1OK图保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机1OK图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机1OK图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机1NG图保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机1NG图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机1NG图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机2图片保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机2原图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机2原图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机2OK图保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机2OK图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机2OK图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机2NG图保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机2NG图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机2NG图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机3图片保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机3原图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机3原图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机3OK图保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机3OK图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机3OK图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机3NG图保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机3NG图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机3NG图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button27_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机4图片保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机4原图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机4原图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机4OK图保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机4OK图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机4OK图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机4NG图保存路径 = folderBrowserDialog.SelectedPath;
                数据变量.相机4NG图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                txt相机4NG图保存路径.Text = folderBrowserDialog.SelectedPath;
            }
        }
        private void 清空内容()
        {
            txt相机1曝光时间.Value = 0;
            txt相机1增益.Value = 0;
            txt相机1模板数量.Value = 0;
            txt相机1对比度.Value = 0;
            txt相机1匹配度.Value = 0;
            txt相机1hdl.Clear();
            txt相机1hdict.Clear();
            txt相机1原图保存路径.Clear();
            txt相机1OK图保存路径.Clear();
            txt相机1NG图保存路径.Clear();

            // 相机2
            txt相机2曝光时间.Value = 0;
            txt相机2增益.Value = 0;
            txt相机2模板数量.Value = 0;
            txt相机2对比度.Value = 0;
            txt相机2匹配度.Value = 0;
            txt相机2hdl.Clear();
            txt相机2hdict.Clear();
            txt相机2原图保存路径.Clear();
            txt相机2OK图保存路径.Clear();
            txt相机2NG图保存路径.Clear();

            // 相机3
            txt相机3曝光时间.Value = 0;
            txt相机3增益.Value = 0;
            txt相机3模板数量.Value = 0;
            txt相机3对比度.Value = 0;
            txt相机3匹配度.Value = 0;
            txt相机3hdl.Clear();
            txt相机3hdict.Clear();
            txt相机3原图保存路径.Clear();
            txt相机3OK图保存路径.Clear();
            txt相机3NG图保存路径.Clear();

            // 相机4
            txt相机4曝光时间.Value = 0;
            txt相机4增益.Value = 0;
            txt相机4模板数量.Value = 0;
            txt相机4对比度.Value = 0;
            txt相机4匹配度.Value = 0;
            txt相机4hdl.Clear();
            txt相机4hdict.Clear();
            txt相机4原图保存路径.Clear();
            txt相机4OK图保存路径.Clear();
            txt相机4NG图保存路径.Clear();

            txt相机1模板保存路径.Clear();
            txt相机2模板保存路径.Clear();
            txt相机3模板保存路径.Clear();
            txt相机4模板保存路径.Clear();

            // 系统
            txt料号名称.SelectedIndex = -1;
            txt权限时间.Clear();
            txt图片删除日期.Clear();
            txt相机数量设置.Items.Clear();

        }

    }
}
