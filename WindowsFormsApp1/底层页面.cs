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
    public partial class 底层页面 : Form
    {
        public static 料号添加 liaohaotianjia = new 料号添加();
        public static 自动页面 zidongjiemian = new 自动页面();
        public static 参数界面 canshujiemian = new 参数界面();
        public static 登录页面 denglujiemian=new 登录页面();
        public static 调试页面 tiaoshijiemian=new 调试页面();
        public static 配方页面 peifangjiemian=new 配方页面();
        public static 相机设置 xiangjishezhijiemian=new 相机设置();
       
        

        //private halcon halcon=new halcon();
        // 加这个：只执行一次
        private bool _firstAutoLoaded = false;

        public 底层页面()
        {
            InitializeComponent();
            ResponsiveLayoutService.ConfigureMainWindow(this, splitContainer1);
            denglujiemian.LogAdded += (int1, string2) => { 信息1.AddLog(int1, string2); };
            xiangjishezhijiemian.LogAdded += (int1, string2) => { 信息1.AddLog(int1, string2); };
            zidongjiemian.LogAdded += (int1, string2) => { 信息1.AddLog(int1, string2); };
            tiaoshijiemian.LogAdded += (int1, string2) => { 信息1.AddLog(int1, string2); };
            peifangjiemian.LogAdded += (int1, string2) => { 信息1.AddLog(int1, string2); };
            canshujiemian.LogAdded += (int1, string2) => { 信息1.AddLog(int1, string2); };
            

            裁图类.日志信息 = 信息1;
            ShowFormInPanel(zidongjiemian, splitContainer1.Panel1);
         
            
        }


        private void ShowFormInPanel(Form form, Panel panel)
        {
           if(状态.自动状态||状态.实时状态)
            {
                信息1.AddLog(0, "请先关闭自动状态或实时采集状态!!!");
                return; 
            }
            // 防错：如果窗口为空，直接退出，避免报错
            if (form == null) return;
            if (form is 自动页面 && !_firstAutoLoaded)
            {
                _firstAutoLoaded = true;  // 只允许一次
                zidongjiemian.相机连接(); // 此时事件已绑定 → 日志必输出！
                
            }
            // ==============================================
            // 【关键】在切换前，判断当前显示的是什么窗体
            // 如果是【调试页面】或【相机设置】，就清空它的图片
            // ==============================================
            if (panel.Controls.Count > 0)
            {
                // 获取当前正在显示的旧窗体
                Form oldForm = panel.Controls[0] as Form;

                if (oldForm != null)
                {
                    // ----------------------
                    // 只清空这两个窗体！
                    // ----------------------
                    if (oldForm is 调试页面 || oldForm is 配方页面)
                    {
                        // 【深度遍历】找到窗体里所有 halcon 控件（不管包几层都能找到）
                        foreach (var c in GetAllControls(oldForm))
                        {
                            if (c is halcon hc)
                            {
                                hc.ClearDisplay(); // 清空
                                状态.控制放大缩小后图片的显示 = false;
                            }
                        }
                    }
                }
            }
            // 清空容器里的所有控件
            panel.Controls.Clear();
            // 必须设置：将窗口改为非顶层窗口（才能嵌入Panel）
            form.TopLevel = false;
            // 隐藏窗口边框（嵌入后更美观）
            form.FormBorderStyle = FormBorderStyle.None;
            // 根据当前笔记本分辨率和 DPI 进行运行时布局适配
            // 让窗口自动填满 panel
            form.Dock = DockStyle.Fill;
            // 将窗口添加到 panel 容器中
            panel.Controls.Add(form);
            // 显示窗口
            form.Show();

        }
       

        private void 自动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFormInPanel(zidongjiemian, splitContainer1.Panel1);
            
            自动ToolStripMenuItem.BackColor= Color.CornflowerBlue;
            参数界面ToolStripMenuItem.BackColor=Color.White;
            相机设置ToolStripMenuItem.BackColor= Color.White;
            登录界面ToolStripMenuItem.BackColor = Color.White;
            配方界面ToolStripMenuItem.BackColor = Color.White;
            调试界面ToolStripMenuItem.BackColor = Color.White;
            料号添加ToolStripMenuItem.BackColor = Color.White;
        }

        private void 相机设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFormInPanel(xiangjishezhijiemian, splitContainer1.Panel1);

            自动ToolStripMenuItem.BackColor = Color.White;
            参数界面ToolStripMenuItem.BackColor = Color.White;
            相机设置ToolStripMenuItem.BackColor = Color.CornflowerBlue;
            登录界面ToolStripMenuItem.BackColor = Color.White;
            配方界面ToolStripMenuItem.BackColor = Color.White;
            调试界面ToolStripMenuItem.BackColor = Color.White;
            料号添加ToolStripMenuItem.BackColor = Color.White;
        }

        private void 参数界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (状态.登录权限)
            {
                ShowFormInPanel(canshujiemian, splitContainer1.Panel1);
                自动ToolStripMenuItem.BackColor = Color.White;
                参数界面ToolStripMenuItem.BackColor = Color.CornflowerBlue;
                相机设置ToolStripMenuItem.BackColor = Color.White;
                登录界面ToolStripMenuItem.BackColor = Color.White;
                配方界面ToolStripMenuItem.BackColor = Color.White;
                调试界面ToolStripMenuItem.BackColor = Color.White;
                料号添加ToolStripMenuItem.BackColor = Color.White;

            }
            else
            {
                
                ShowFormInPanel(denglujiemian, splitContainer1.Panel1);

                自动ToolStripMenuItem.BackColor = Color.White;
                参数界面ToolStripMenuItem.BackColor = Color.White;
                相机设置ToolStripMenuItem.BackColor = Color.White;
                登录界面ToolStripMenuItem.BackColor = Color.CornflowerBlue;
                配方界面ToolStripMenuItem.BackColor = Color.White;
                调试界面ToolStripMenuItem.BackColor = Color.White;
                料号添加ToolStripMenuItem.BackColor = Color.White;
                信息1.AddLog(0, "权限不足,请先登录,跳转到登录界面");
            }
        }

        private void 配方界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFormInPanel(peifangjiemian, splitContainer1.Panel1);

            自动ToolStripMenuItem.BackColor = Color.White;
            参数界面ToolStripMenuItem.BackColor = Color.White;
            相机设置ToolStripMenuItem.BackColor = Color.White;
            登录界面ToolStripMenuItem.BackColor = Color.White;
            配方界面ToolStripMenuItem.BackColor = Color.CornflowerBlue;
            调试界面ToolStripMenuItem.BackColor = Color.White;
            料号添加ToolStripMenuItem.BackColor = Color.White;

        }

        private void 调试界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFormInPanel(tiaoshijiemian, splitContainer1.Panel1);

            自动ToolStripMenuItem.BackColor = Color.White;
            参数界面ToolStripMenuItem.BackColor = Color.White;
            相机设置ToolStripMenuItem.BackColor = Color.White;
            登录界面ToolStripMenuItem.BackColor = Color.White;
            配方界面ToolStripMenuItem.BackColor = Color.White;
            调试界面ToolStripMenuItem.BackColor = Color.CornflowerBlue;
            料号添加ToolStripMenuItem.BackColor = Color.White;


        }

        private void 登录界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFormInPanel(denglujiemian, splitContainer1.Panel1);

            自动ToolStripMenuItem.BackColor = Color.White;
            参数界面ToolStripMenuItem.BackColor = Color.White;
            相机设置ToolStripMenuItem.BackColor = Color.White;
            登录界面ToolStripMenuItem.BackColor = Color.CornflowerBlue;
            配方界面ToolStripMenuItem.BackColor = Color.White;
            调试界面ToolStripMenuItem.BackColor = Color.White;
            料号添加ToolStripMenuItem.BackColor = Color.White;

        }
        // 👇 必须加这个辅助方法：遍历所有控件
        public IEnumerable<Control> GetAllControls(Control parent)// 功能：找到一个容器（窗体/Panel）里面的**所有控件**，包括嵌套在里面的
        {
            // 1. 遍历父容器里【直接放在里面】的每一个控件
            foreach (Control c in parent.Controls)
            {
                // 2. 先返回这个控件（告诉外面：我找到一个）
                yield return c;
                // 3. 重点！递归：再进入这个控件的内部，找它的子控件
                // 比如：Panel 里面的 GroupBox 里面的 halcon1
                foreach (var child in GetAllControls(c))
                    yield return child;
            }
        }

        private void 底层页面_FormClosing(object sender, FormClosingEventArgs e)
        {
            zidongjiemian.关闭相机();
            xiangjishezhijiemian.关闭实时预览();
        }

        private void 底层页面_Shown(object sender, EventArgs e)
        {
            信息1.AddLog(0, "深度学习模型与设备连接中.......");
            zidongjiemian.深度学习数据上传();
            zidongjiemian.电脑设备连接();
            
        }

        private void 料号添加ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            ShowFormInPanel(liaohaotianjia, splitContainer1.Panel1);

            自动ToolStripMenuItem.BackColor = Color.White;
            参数界面ToolStripMenuItem.BackColor = Color.White;
            相机设置ToolStripMenuItem.BackColor = Color.White;
            登录界面ToolStripMenuItem.BackColor = Color.White;
            配方界面ToolStripMenuItem.BackColor = Color.White;
            调试界面ToolStripMenuItem.BackColor = Color.White;
            料号添加ToolStripMenuItem.BackColor = Color.CornflowerBlue;
        }
    }

}
