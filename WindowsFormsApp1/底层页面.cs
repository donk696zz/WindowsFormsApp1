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
        public static 料号设置页面 liaohaoshezhijiemian = new 料号设置页面();
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
            liaohaoshezhijiemian.LogAdded += (int1, string2) => { 信息1.AddLog(int1, string2); };
            canshujiemian.LogAdded += (int1, string2) => { 信息1.AddLog(int1, string2); };
            canshujiemian.PreviewRequested += tiaoshijiemian.预览当前图片;
            denglujiemian.登录成功 += 显示参数页面;
            

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
            // 页面采用缓存切换：离开时只隐藏，不清空图片、不移除窗体。
            // 这样调试页的当前图片、文件夹索引和检测结果都能保留，
            // 参数页也可以在调试页隐藏期间刷新预览结果。
            foreach (Form oldForm in panel.Controls.OfType<Form>())
            {
                if (!ReferenceEquals(oldForm, form) && oldForm.Visible)
                    oldForm.Hide();
            }
            // 必须设置：将窗口改为非顶层窗口（才能嵌入Panel）
            form.TopLevel = false;
            // 隐藏窗口边框（嵌入后更美观）
            form.FormBorderStyle = FormBorderStyle.None;
            // 根据当前笔记本分辨率和 DPI 进行运行时布局适配
            // 让窗口自动填满 panel
            form.Dock = DockStyle.Fill;
            // 第一次打开时加入容器，后续切换复用同一个窗体实例。
            if (!panel.Controls.Contains(form))
                panel.Controls.Add(form);
            // 显示窗口
            form.Show();
            form.BringToFront();

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
                显示参数页面();
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

        private void 显示参数页面()
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

        private void 配方界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFormInPanel(liaohaoshezhijiemian, splitContainer1.Panel1);

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
        private void 底层页面_FormClosing(object sender, FormClosingEventArgs e)
        {
            zidongjiemian.关闭相机();
            xiangjishezhijiemian.关闭实时预览();
        }

        private void 底层页面_Shown(object sender, EventArgs e)
        {
            信息1.AddLog(0, "检测参数与相机设备初始化中.......");
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
