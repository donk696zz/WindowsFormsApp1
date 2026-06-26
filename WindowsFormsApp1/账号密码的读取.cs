using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WindowsFormsApp1
{
  public static class 账号密码的读取
    {

      public static readonly string 文件保存地址 = Path.Combine(Application.StartupPath, "账号密码.txt");
        public  static  Dictionary<string,string> 账号密码文本读取()
        {
            Dictionary<string,string> 账号密码=new Dictionary<string,string>();
            if(!File.Exists(文件保存地址))
            {
                File.Create(文件保存地址).Close();
                return 账号密码;
            }
            string[] lines = File.ReadAllLines(文件保存地址);
            foreach(string line in lines)
            {
                if(string.IsNullOrEmpty(line)) continue;
                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if(parts.Length == 2)
                {
                    string key=parts[0].Trim();
                    string value = parts[1].Trim();
                    账号密码.Add(key,value);
                }
            }
            return 账号密码 ;
        }
        public static void 账号密码文本保存(Dictionary<string, string> 账号密码)
        {
            try
            {
                // 自动创建文件目录
                string dir = Path.GetDirectoryName(文件保存地址);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // 拼接每行内容：账号 密码
                StringBuilder sb = new StringBuilder();
                foreach (var item in 账号密码)
                {
                    sb.AppendLine($"{item.Key} {item.Value}");
                }

                // 写入文件（UTF8 不乱码）
                File.WriteAllText(文件保存地址, sb.ToString(), Encoding.UTF8);
                账号密码文本读取();
            }
            catch
            {
                // 保存失败可自行处理
            }
        }


    }
}
