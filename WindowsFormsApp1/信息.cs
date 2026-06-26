using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class 信息 : UserControl
    {
        private static readonly object _locker = new object();
        public 信息()
        {
            InitializeComponent();
            // 只在初始化时设置一次字体！！！（关键）
            richTextBox1.Font = new Font("微软雅黑", 12);
        }
        public void AddLog(int 字体颜色,string 内容)
        {
            if (richTextBox1.InvokeRequired)
            {
                if (!richTextBox1.IsDisposed && richTextBox1.IsHandleCreated)
                    richTextBox1.BeginInvoke(new Action<int, string>(AddLog), 字体颜色, 内容);
                return;
            }

            // 文件保存
            string fileName = $"生产文件_{DateTime.Now:yyyyMMdd}.txt";
            string logDir = @"D:\生产信息";
            string savePath = Path.Combine(logDir, fileName);
            Directory.CreateDirectory(logDir);

            try
            {
                lock (_locker)
                {
                    using (StreamWriter sw = new StreamWriter(savePath, true, Encoding.UTF8))
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {内容}");
                    }
                }
            }
            catch { }

            // 核心：只给新文字上色，绝不重置字体
            string logText = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {内容}{Environment.NewLine}";
            int startPos = richTextBox1.TextLength;

            // 1. 先加文字
            richTextBox1.AppendText(logText);

            // 2. 选中刚加的这段
            richTextBox1.Select(startPos, logText.Length);

            // 3. 只给这段上色
            richTextBox1.SelectionColor = 字体颜色 == 0 ? Color.Red : Color.Black;

            // 4. 光标归位
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.SelectionLength = 0;
            richTextBox1.ScrollToCaret();

        }
    }
}
