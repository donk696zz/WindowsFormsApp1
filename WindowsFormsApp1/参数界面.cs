using HslCommunication.Profinet.Panasonic.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WindowsFormsApp1
{
    public partial class 参数界面 : Form
    {

        public event Action<int, string> LogAdded;
       裁图类 裁图类=new 裁图类();

        public 参数界面()
        {
            InitializeComponent();
            第一次数据加载();
            保存按钮与路径控制();
        }
        public void 第一次数据加载()
        {
            textBox1.Text = 数据变量.相机1原图保存路径;
            textBox2.Text = 数据变量.相机1hdl文件路径;
            textBox3.Text = 数据变量.相机1hdict文件路径;
            textBox15.Text = 数据变量.相机1OK图保存路径;
            textBox16.Text = 数据变量.相机1NG图保存路径;

            textBox6.Text = 数据变量.相机2原图保存路径;
            textBox5.Text = 数据变量.相机2hdict文件路径;
            textBox17.Text = 数据变量.相机3OK图保存路径;
            textBox18.Text =数据变量.相机3NG图保存路径;

            textBox11.Text = 数据变量.相机3原图保存路径;
            textBox10.Text = 数据变量.相机3hdl文件路径;
            textBox9.Text = 数据变量.相机3hdict文件路径;
            textBox20.Text = 数据变量.相机3OK图保存路径;
            textBox19.Text = 数据变量.相机3NG图保存路径;


            textBox14.Text = 数据变量.相机4原图保存路径;
            textBox13.Text = 数据变量.相机4hdl文件路径;
            textBox12.Text = 数据变量.相机4hdict文件路径;
            textBox22.Text =数据变量.相机4OK图保存路径;
            textBox21.Text = 数据变量.相机4NG图保存路径;

            textBox7.Text = 数据变量.权限时间;
            comboBox1.Text = ClampCameraCountText(数据变量.相机数量设置);
            textBox8.Text =数据变量.图片删除日期;
            comboBox2.Items.Clear();
            comboBox2.Items.AddRange(相机变量.料号集合.ToArray());
            comboBox2.Text =数据变量.料号名称;
        }

        private static string ClampCameraCountText(string value)
        {
            if (!int.TryParse(value, out int count))
                return "2";

            return Math.Max(1, Math.Min(2, count)).ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机1图片保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机1原图保存路径 = Properties.Settings.Default.相机1图片保存路径;
                textBox1.Text = 数据变量.相机1原图保存路径;
            }


        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "ONNX模型 (*.onnx)|*.onnx";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机1hdl文件路径 = folderBrowserDialog.FileName;
                    验证Onnx模型(Properties.Settings.Default.相机1hdl文件路径);
                    Properties.Settings.Default.Save();
                    数据变量.相机1hdl文件路径 = Properties.Settings.Default.相机1hdl文件路径;
                    textBox2.Text = 数据变量.相机1hdl文件路径;
                   
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机1路径错误!"+ex);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "类别名称文件 (*.txt)|*.txt";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机1hdict文件路径 = folderBrowserDialog.FileName;
                    Properties.Settings.Default.Save();
                    数据变量.相机1hdict文件路径 = Properties.Settings.Default.相机1hdict文件路径;
                    textBox3.Text = 数据变量.相机1hdict文件路径;
                    
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机1路径错误!" + ex);
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
                Properties.Settings.Default.相机2图片保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机2原图保存路径 = Properties.Settings.Default.相机2图片保存路径;
                textBox6.Text = 数据变量.相机2原图保存路径;
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "ONNX模型 (*.onnx)|*.onnx";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机2hdl文件路径 = folderBrowserDialog.FileName;
                    验证Onnx模型(Properties.Settings.Default.相机2hdl文件路径);
                    Properties.Settings.Default.Save();
                    数据变量.相机2hdl文件路径 = Properties.Settings.Default.相机2hdl文件路径;
                    textBox5.Text = 数据变量.相机2hdl文件路径;
                    
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
            folderBrowserDialog.Filter = "类别名称文件 (*.txt)|*.txt";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机2hdict文件路径 = folderBrowserDialog.FileName;
                    Properties.Settings.Default.Save();
                    数据变量.相机2hdict文件路径 = Properties.Settings.Default.相机2hdict文件路径;
                    textBox4.Text = 数据变量.相机2hdict文件路径;
                    
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机2路径错误!" + ex);
                }
            }

        }

        private void button11_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机3图片保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机3原图保存路径 = Properties.Settings.Default.相机3图片保存路径;
                textBox11.Text = 数据变量.相机3原图保存路径;
            }

        }

        private void button10_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "ONNX模型 (*.onnx)|*.onnx";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机3hdl文件路径 = folderBrowserDialog.FileName;
                    验证Onnx模型(Properties.Settings.Default.相机3hdl文件路径);
                    Properties.Settings.Default.Save();
                    数据变量.相机3hdl文件路径 = Properties.Settings.Default.相机3hdl文件路径;
                    textBox10.Text = 数据变量.相机3hdl文件路径;
                   
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
            folderBrowserDialog.Filter = "类别名称文件 (*.txt)|*.txt";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机3hdict文件路径 = folderBrowserDialog.FileName;
                    Properties.Settings.Default.Save();
                    数据变量.相机3hdict文件路径 = Properties.Settings.Default.相机3hdict文件路径;
                    textBox9.Text = 数据变量.相机3hdict文件路径;
                    
                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机3路径错误!" + ex);
                }
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机4图片保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机4原图保存路径 = Properties.Settings.Default.相机4图片保存路径;
                textBox14.Text = 数据变量.相机4原图保存路径;

            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            OpenFileDialog folderBrowserDialog = new OpenFileDialog();
            folderBrowserDialog.Title = "选择文件提取路径";
            folderBrowserDialog.Filter = "ONNX模型 (*.onnx)|*.onnx";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机4hdl文件路径 = folderBrowserDialog.FileName;
                    验证Onnx模型(Properties.Settings.Default.相机4hdl文件路径);
                    Properties.Settings.Default.Save();
                    数据变量.相机4hdl文件路径 = Properties.Settings.Default.相机4hdl文件路径;
                    textBox13.Text = 数据变量.相机4hdl文件路径;
                    
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
            folderBrowserDialog.Filter = "类别名称文件 (*.txt)|*.txt";
            folderBrowserDialog.InitialDirectory = "";//文件选择打开的初始位置
            folderBrowserDialog.Multiselect = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Properties.Settings.Default.相机4hdict文件路径 = folderBrowserDialog.FileName;
                    Properties.Settings.Default.Save();
                    数据变量.相机4hdict文件路径 = Properties.Settings.Default.相机4hdict文件路径;
                    textBox12.Text = 数据变量.相机4hdict文件路径;
                    

                }
                catch (Exception ex)
                {
                    LogAdded?.Invoke(0, "相机4路径错误!" + ex);
                }
            }
            
            
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != -1&& comboBox2.SelectedIndex != -1)
            {
                
                int 数量 = int.Parse(comboBox1.Text);
                数据变量.权限时间 = textBox7.Text;
                Properties.Settings.Default.权限时间设置 = textBox7.Text;
                数据变量.相机数量设置 = comboBox1.Text;
                Properties.Settings.Default.相机数量设置 = comboBox1.Text;
                数据变量.图片删除日期 = textBox8.Text;
                Properties.Settings.Default.图片删除时间 = textBox7.Text;
                数据变量.料号名称 = comboBox2.Text;
                Properties.Settings.Default.当前料号= comboBox2.Text;
                Properties.Settings.Default.Save();
                数据保存();
             
                底层页面.xiangjishezhijiemian.相机数量设定(数量);
                底层页面.tiaoshijiemian.相机数量设定(数量);
                底层页面.peifangjiemian.相机数量设定(数量);
                底层页面.zidongjiemian.调整控件布局(数量);


                底层页面.liaohaotianjia.保存数据();
                保存按钮与路径控制();

                LogAdded?.Invoke(1, "数据保存成功!");
            }
            else
            {
                LogAdded?.Invoke(0, "请选择相机数量和料号选择!");
            }
           
        }
        public void 数据保存()
        {
            数据变量.相机1原图保存路径=textBox1.Text;
            数据变量.相机1hdl文件路径= textBox2.Text;
            数据变量.相机1hdict文件路径 = textBox3.Text;
            数据变量.相机1OK图保存路径 = textBox15.Text;
            数据变量.相机1NG图保存路径 = textBox16.Text;

            数据变量.相机2原图保存路径 = textBox6.Text;
            数据变量.相机2hdict文件路径 = textBox5.Text;
            数据变量.相机3OK图保存路径 = textBox17.Text;
            数据变量.相机3NG图保存路径 = textBox18.Text;

            数据变量.相机3原图保存路径 = textBox11.Text;
            数据变量.相机3hdl文件路径 = textBox10.Text;
            数据变量.相机3hdict文件路径 = textBox9.Text;
            数据变量.相机3OK图保存路径 = textBox20.Text;
            数据变量.相机3NG图保存路径 = textBox19.Text;


            数据变量.相机4原图保存路径 = textBox14.Text;
            数据变量.相机4hdl文件路径 = textBox13.Text;
            数据变量.相机4hdict文件路径 = textBox12.Text;
            数据变量.相机4OK图保存路径 = textBox22.Text;
            数据变量.相机4NG图保存路径 = textBox21.Text;

        }

        private void button15_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机1OK图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机1原图保存路径 = Properties.Settings.Default.相机1OK图保存路径;
                textBox15.Text = 数据变量.相机1原图保存路径;
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
                Properties.Settings.Default.Save();
                数据变量.相机1NG图保存路径 = Properties.Settings.Default.相机1NG图保存路径;
                textBox16.Text = 数据变量.相机1NG图保存路径;
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机2OK图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机2OK图保存路径 = Properties.Settings.Default.相机2OK图保存路径;
                textBox17.Text = 数据变量.相机2OK图保存路径;
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机2NG图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机2NG图保存路径 = Properties.Settings.Default.相机2NG图保存路径;
                textBox18.Text = 数据变量.相机2NG图保存路径;
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机3OK图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机3OK图保存路径 = Properties.Settings.Default.相机3OK图保存路径;
                textBox20.Text = 数据变量.相机3OK图保存路径;
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机3NG图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机3NG图保存路径 = Properties.Settings.Default.相机3NG图保存路径;
                textBox19.Text = 数据变量.相机3NG图保存路径;
            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机4OK图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机4OK图保存路径 = Properties.Settings.Default.相机4OK图保存路径;
                textBox22.Text = 数据变量.相机4OK图保存路径;
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择文件提取路径";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;//文件选择打开的初始位置
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.相机4NG图保存路径 = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
                数据变量.相机4NG图保存路径 = Properties.Settings.Default.相机4NG图保存路径;
                textBox21.Text = 数据变量.相机4NG图保存路径;
            }
        }
        public void 清理图片()
        {
            try
            {
                // 读取配置里所有根路径
                List<string> rootPaths = new List<string>
             {
            //相机1
            Properties.Settings.Default.相机1图片保存路径,
            Properties.Settings.Default.相机1OK图保存路径,
            Properties.Settings.Default.相机1NG图保存路径,
            //相机2
            Properties.Settings.Default.相机2图片保存路径,
            Properties.Settings.Default.相机2OK图保存路径,
            Properties.Settings.Default.相机2NG图保存路径,
            //相机3
            Properties.Settings.Default.相机3图片保存路径,
            Properties.Settings.Default.相机3OK图保存路径,
            Properties.Settings.Default.相机3NG图保存路径,
            //相机4
            Properties.Settings.Default.相机4图片保存路径,
            Properties.Settings.Default.相机4OK图保存路径,
            Properties.Settings.Default.相机4NG图保存路径
             };

                // 7天前日期
                DateTime delLimitDate = DateTime.Now.AddDays(-7);

                foreach (string root in rootPaths)
                {
                    if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                        continue;

                    // 获取根目录下所有子文件夹
                    string[] dirs = Directory.GetDirectories(root);
                    foreach (string dir in dirs)
                    {
                        try
                        {
                            // 取文件夹名称 如 20260508
                            string dirName = Path.GetFileName(dir.TrimEnd('\\'));
                            // 尝试转成日期
                            if (DateTime.TryParseExact(dirName, "yyyyMMdd",
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None,
                                out DateTime folderDate))
                            {
                                // 早于7天前 → 删除整个文件夹
                                if (folderDate < delLimitDate)
                                {
                                    Directory.Delete(dir, true); // true=递归删除里面所有文件
                                }
                            }
                        }
                        catch
                        {
                            // 单个文件夹删除失败不影响其他
                        }
                    }
                }

                LogAdded?.Invoke(1, "已自动清理7天前过期图片文件夹");
            }
            catch
            {

            }
        }
        public void 保存按钮与路径控制()
        {
            int 数量 = int.Parse(comboBox1.Text);
            switch(数量)
            {
                case 1:
                textBox6.ReadOnly = true;
                textBox17.ReadOnly = true;
                textBox18.ReadOnly = true;
                textBox5.ReadOnly = true;
                textBox4.ReadOnly = true;

                button6.Enabled = false;
                button17.Enabled = false;
                button18.Enabled = false;
                button5.Enabled = false;
                button4.Enabled = false;

                textBox11.ReadOnly = true;
                textBox20.ReadOnly = true;
                textBox19.ReadOnly = true;
                textBox10.ReadOnly = true;
                textBox9.ReadOnly = true;

                button11.Enabled = false;
                button20.Enabled = false;
                button19.Enabled = false;
                button10.Enabled = false;
                button9.Enabled = false;

                textBox14.ReadOnly = true;
                textBox22.ReadOnly = true;
                textBox21.ReadOnly = true;
                textBox13.ReadOnly = true;
                textBox12.ReadOnly = true;

                button14.Enabled = false;
                button22.Enabled = false;
                button21.Enabled = false;
                button13.Enabled = false;
                button12.Enabled = false;

                button23.Enabled = true;
                button24.Enabled = false;
                button25.Enabled = false;
                button26.Enabled = false;
                   
                

                    break;
            
             case 2:
                textBox6.ReadOnly = false;
                textBox17.ReadOnly = false;
                textBox18.ReadOnly = false;
                textBox5.ReadOnly = false;
                textBox4.ReadOnly = false;

                button6.Enabled = true;
                button17.Enabled = true;
                button18.Enabled = true;
                button5.Enabled = true;
                button4.Enabled = true;

                textBox11.ReadOnly = true;
                textBox20.ReadOnly = true;
                textBox19.ReadOnly = true;
                textBox10.ReadOnly = true;
                textBox9.ReadOnly = true;

                button11.Enabled = false;
                button20.Enabled = false;
                button19.Enabled = false;
                button10.Enabled = false;
                button9.Enabled = false;

                textBox14.ReadOnly = true;
                textBox22.ReadOnly = true;
                textBox21.ReadOnly = true;
                textBox13.ReadOnly = true;
                textBox12.ReadOnly = true;

                button14.Enabled = false;
                button22.Enabled = false;
                button21.Enabled = false;
                button13.Enabled = false;
                button12.Enabled = false;

                button23.Enabled = true;
                button24.Enabled = true;
                button25.Enabled = false;
                button26.Enabled = false;
                   
               
               

                    break;

              case 3:
                    textBox6.ReadOnly = false;
                    textBox17.ReadOnly = false;
                    textBox18.ReadOnly = false;
                    textBox5.ReadOnly = false;
                    textBox4.ReadOnly = false;

                    button6.Enabled = true;
                    button17.Enabled = true;
                    button18.Enabled = true;
                    button5.Enabled = true;
                    button4.Enabled = true;

                    textBox11.ReadOnly = false;
                    textBox20.ReadOnly =false;
                    textBox19.ReadOnly = false;
                    textBox10.ReadOnly = false;
                    textBox9.ReadOnly = false;

                    button11.Enabled = true;
                    button20.Enabled = true;
                    button19.Enabled = true;
                    button10.Enabled = true;
                    button9.Enabled = true;

                    textBox14.ReadOnly = true;
                    textBox22.ReadOnly = true;
                    textBox21.ReadOnly = true;
                    textBox13.ReadOnly = true;
                    textBox12.ReadOnly = true;

                    button14.Enabled = false;
                    button22.Enabled = false;
                    button21.Enabled = false;
                    button13.Enabled = false;
                    button12.Enabled = false;

                    button23.Enabled = true;
                    button24.Enabled = true;
                    button25.Enabled = true;
                    button26.Enabled = false;
                   
                    

                    break;
                case 4:
                    textBox6.ReadOnly = false;
                    textBox17.ReadOnly = false;
                    textBox18.ReadOnly = false;
                    textBox5.ReadOnly = false;
                    textBox4.ReadOnly = false;

                    button6.Enabled = true;
                    button17.Enabled = true;
                    button18.Enabled = true;
                    button5.Enabled = true;
                    button4.Enabled = true;

                    textBox11.ReadOnly = false;
                    textBox20.ReadOnly = false;
                    textBox19.ReadOnly = false;
                    textBox10.ReadOnly = false;
                    textBox9.ReadOnly = false;

                    button11.Enabled = true;
                    button20.Enabled = true;
                    button19.Enabled = true;
                    button10.Enabled = true;
                    button9.Enabled = true;

                    textBox14.ReadOnly = false;
                    textBox22.ReadOnly = false;
                    textBox21.ReadOnly = false;
                    textBox13.ReadOnly = false;
                    textBox12.ReadOnly = false;

                    button14.Enabled = true;
                    button22.Enabled = true;
                    button21.Enabled = true;
                    button13.Enabled = true;
                    button12.Enabled = true;


                    button23.Enabled = true;
                    button24.Enabled = true;
                    button25.Enabled = true;
                    button26.Enabled = true;
                    
                        
                        
                    break;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            textBox1.Text = Properties.Settings.Default.相机1图片保存路径;
            textBox2.Text = Properties.Settings.Default.相机1hdl文件路径;
            textBox3.Text = Properties.Settings.Default.相机1hdict文件路径;
            textBox15.Text = Properties.Settings.Default.相机1OK图保存路径;
            textBox16.Text = Properties.Settings.Default.相机1NG图保存路径;

            textBox6.Text = Properties.Settings.Default.相机2图片保存路径;
            textBox5.Text = Properties.Settings.Default.相机2hdl文件路径;
            textBox4.Text = Properties.Settings.Default.相机2hdict文件路径;
            textBox17.Text = Properties.Settings.Default.相机3OK图保存路径;
            textBox18.Text = Properties.Settings.Default.相机3NG图保存路径;

            textBox11.Text = Properties.Settings.Default.相机3图片保存路径;
            textBox10.Text = Properties.Settings.Default.相机3hdl文件路径;
            textBox9.Text = Properties.Settings.Default.相机3hdict文件路径;
            textBox20.Text = Properties.Settings.Default.相机3OK图保存路径;
            textBox19.Text = Properties.Settings.Default.相机3NG图保存路径;


            textBox14.Text = Properties.Settings.Default.相机4图片保存路径;
            textBox13.Text = Properties.Settings.Default.相机4hdl文件路径;
            textBox12.Text = Properties.Settings.Default.相机4hdict文件路径;
            textBox22.Text = Properties.Settings.Default.相机4OK图保存路径;
            textBox21.Text = Properties.Settings.Default.相机4NG图保存路径;

            textBox7.Text = Properties.Settings.Default.权限时间设置;
            comboBox1.Text = ClampCameraCountText(Properties.Settings.Default.相机数量设置);
            textBox8.Text = Properties.Settings.Default.图片删除时间;
            comboBox2.Text = Properties.Settings.Default.当前料号;
        }

        private void button23_Click(object sender, EventArgs e)
        {
            裁图类.截图分类(1, "相机1深度学习模板/待分类图片");
        }

        private void button24_Click(object sender, EventArgs e)
        {
            裁图类.截图分类(2, "相机2深度学习模板/待分类图片");
        }

        private void button25_Click(object sender, EventArgs e)
        {
            裁图类.截图分类(3, "相机3深度学习模板/待分类图片");
        }

        private void button26_Click(object sender, EventArgs e)
        {
            裁图类.截图分类(4, "相机4深度学习模板/待分类图片");
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem == null) return;
            bool 只显示 = false;
            string 料号 = comboBox2.SelectedItem.ToString();
            bool ok = 料号切换读取文件.加载料号到全局变量(料号,只显示);

            textBox1.Text = 数据变量.相机1原图保存路径;
            textBox2.Text = 数据变量.相机1hdl文件路径;
            textBox3.Text = 数据变量.相机1hdict文件路径;
            textBox15.Text = 数据变量.相机1OK图保存路径;
            textBox16.Text = 数据变量.相机1NG图保存路径;

            textBox6.Text = 数据变量.相机2原图保存路径;
            textBox5.Text =数据变量.相机2hdl文件路径;
            textBox4.Text = 数据变量.相机2hdict文件路径;
            textBox17.Text = 数据变量.相机3OK图保存路径;
            textBox18.Text =数据变量.相机3NG图保存路径;

            textBox11.Text = 数据变量.相机3原图保存路径;
            textBox10.Text = 数据变量.相机3hdl文件路径;
            textBox9.Text = 数据变量.相机3hdict文件路径;
            textBox20.Text = 数据变量.相机3OK图保存路径;
            textBox19.Text = 数据变量.相机3NG图保存路径;


            textBox14.Text = 数据变量.相机4原图保存路径;
            textBox13.Text = 数据变量.相机4hdl文件路径;
            textBox12.Text = 数据变量.相机4hdict文件路径;
            textBox22.Text = 数据变量.相机4OK图保存路径;
            textBox21.Text = 数据变量.相机4NG图保存路径;

            textBox7.Text = 数据变量.权限时间;
            comboBox1.Text = ClampCameraCountText(数据变量.相机数量设置);
            textBox8.Text =数据变量.图片删除日期;
           
        }

        private static void 验证Onnx模型(string modelPath)
        {
            using (var detector = new DeimOnnxDetector(modelPath, 0.4f))
            {
                // 构造成功即表示文件存在且输入输出符合DEIM导出格式。
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
