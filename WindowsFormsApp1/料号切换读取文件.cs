using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static  class 料号切换读取文件
    {
        private static string filePath = Path.Combine(Application.StartupPath, "料号参数.txt");

        //=============================================================================
        // 加载料号到全局变量（你原来的）
        //=============================================================================
        public static bool 加载料号到全局变量(string 料号,bool 只显示)
        {
            if (!File.Exists(filePath)) return false;
            var lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "[配方开始]")
                {
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        string line = lines[j].Trim();
                        if (line.StartsWith("料号="))
                        {
                            string 当前行料号 = line.Split('=')[1].Trim();
                            if (当前行料号 == 料号)
                            {
                                读取当前配方到全局变量(lines, j,只显示);
                                return true;
                            }
                            break;
                        }
                    }
                }
            }
            return false;
        }

        //=============================================================================
        // 读取到全局变量（你原来的）
        //=============================================================================
        private static void 读取当前配方到全局变量(string[] lines, int 料号行索引,bool 是否显示)
        {
            数据变量.料号名称 = lines[料号行索引].Split('=')[1].Trim();

            for (int i = 料号行索引; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == "[配方结束]") break;
                if (!line.Contains("=")) continue;

                string[] kv = line.Split('=');
                if (kv.Length < 2) continue;
                string key = kv[0].Trim();
                string val = kv[1].Trim();
                if (!是否显示)
                {
                    switch (key)
                    {
                        case "料号": 数据变量.料号名称 = val; break;
                        case "相机1曝光时间": 数据变量.相机1曝光时间 = val; break;
                        case "相机1增益": 数据变量.相机1增益 = val; break;
                        case "相机2曝光时间": 数据变量.相机2曝光时间 = val; break;
                        case "相机2增益": 数据变量.相机2增益 = val; break;
                        case "相机3曝光时间": 数据变量.相机3曝光时间 = val; break;
                        case "相机3增益": 数据变量.相机3增益 = val; break;
                        case "相机4曝光时间": 数据变量.相机4曝光时间 = val; break;
                        case "相机4增益": 数据变量.相机4增益 = val; break;

                        case "相机1模板数量": 数据变量.相机1模板数量 = val; break;
                        case "相机1对比度": 数据变量.相机1对比度 = val; break;
                        case "相机1匹配度": 数据变量.相机1匹配度 = val; break;
                        case "相机2模板数量": 数据变量.相机2模板数量 = val; break;
                        case "相机2对比度": 数据变量.相机2对比度 = val; break;
                        case "相机2匹配度": 数据变量.相机2匹配度 = val; break;
                        case "相机3模板数量": 数据变量.相机3模板数量 = val; break;
                        case "相机3对比度": 数据变量.相机3对比度 = val; break;
                        case "相机3匹配度": 数据变量.相机3匹配度 = val; break;
                        case "相机4模板数量": 数据变量.相机4模板数量 = val; break;
                        case "相机4对比度": 数据变量.相机4对比度 = val; break;
                        case "相机4匹配度": 数据变量.相机4匹配度 = val; break;


                        case "相机1原图保存路径": 数据变量.相机1原图保存路径 = val; break;
                        case "相机1OK图保存路径": 数据变量.相机1OK图保存路径 = val; break;
                        case "相机1NG图保存路径": 数据变量.相机1NG图保存路径 = val; break;
                        case "相机2原图保存路径": 数据变量.相机2原图保存路径 = val; break;
                        case "相机2OK图保存路径": 数据变量.相机2OK图保存路径 = val; break;
                        case "相机2NG图保存路径": 数据变量.相机2NG图保存路径 = val; break;
                        case "相机3原图保存路径": 数据变量.相机3原图保存路径 = val; break;
                        case "相机3OK图保存路径": 数据变量.相机3OK图保存路径 = val; break;
                        case "相机3NG图保存路径": 数据变量.相机3NG图保存路径 = val; break;
                        case "相机4原图保存路径": 数据变量.相机4原图保存路径 = val; break;
                        case "相机4OK图保存路径": 数据变量.相机4OK图保存路径 = val; break;
                        case "相机4NG图保存路径": 数据变量.相机4NG图保存路径 = val; break;




                        case "相机1hdl文件路径": 数据变量.相机1hdl文件路径 = val; break;
                        case "相机1hdict文件路径": 数据变量.相机1hdict文件路径 = val; break;
                        case "相机2hdl文件路径": 数据变量.相机2hdl文件路径 = val; break;
                        case "相机2hdict文件路径": 数据变量.相机2hdict文件路径 = val; break;
                        case "相机3hdl文件路径": 数据变量.相机3hdl文件路径 = val; break;
                        case "相机3hdict文件路径": 数据变量.相机3hdict文件路径 = val; break;
                        case "相机4hdl文件路径": 数据变量.相机4hdl文件路径 = val; break;
                        case "相机4hdict文件路径": 数据变量.相机4hdict文件路径 = val; break;

                        case "相机1模板保存路径": 数据变量.相机1模板保存路径 = val; break;
                        case "相机2模板保存路径": 数据变量.相机2模板保存路径 = val; break;
                        case "相机3模板保存路径": 数据变量.相机3模板保存路径 = val; break;
                        case "相机4模板保存路径": 数据变量.相机4模板保存路径 = val; break;


                        case "权限时间": 数据变量.权限时间 = val; break;
                        case "图片删除日期": 数据变量.图片删除日期 = val; break;
                        case "相机数量设置": 数据变量.相机数量设置 = val; break;
                    }
                }
                else
                {
                    switch (key)
                    { 
                    case "料号": 只显示.料号名称 = val; break;
                    case "相机1曝光时间": 只显示.相机1曝光时间 = val; break;
                    case "相机1增益": 只显示.相机1增益 = val; break;
                    case "相机2曝光时间": 只显示.相机2曝光时间 = val; break;
                    case "相机2增益": 只显示.相机2增益 = val; break;
                    case "相机3曝光时间": 只显示.相机3曝光时间 = val; break;
                    case "相机3增益": 只显示.相机3增益 = val; break;
                    case "相机4曝光时间": 只显示.相机4曝光时间 = val; break;
                    case "相机4增益": 只显示.相机4增益 = val; break;

                    case "相机1模板数量":只显示.相机1模板数量 = val; break;
                    case "相机1对比度": 只显示.相机1对比度 = val; break;
                    case "相机1匹配度": 只显示.相机1匹配度 = val; break;
                    case "相机2模板数量": 只显示.相机2模板数量 = val; break;
                    case "相机2对比度": 只显示.相机2对比度 = val; break;
                    case "相机2匹配度": 只显示.相机2匹配度 = val; break;
                    case "相机3模板数量": 只显示.相机3模板数量 = val; break;
                    case "相机3对比度": 只显示.相机3对比度 = val; break;
                    case "相机3匹配度": 只显示.相机3匹配度 = val; break;
                    case "相机4模板数量": 只显示.相机4模板数量 = val; break;
                    case "相机4对比度": 只显示.相机4对比度 = val; break;
                    case "相机4匹配度": 只显示.相机4匹配度 = val; break;


                    case "相机1原图保存路径": 只显示.相机1原图保存路径 = val; break;
                    case "相机1OK图保存路径": 只显示.相机1OK图保存路径 = val; break;
                    case "相机1NG图保存路径": 只显示.相机1NG图保存路径 = val; break;
                    case "相机2原图保存路径": 只显示.相机2原图保存路径 = val; break;
                    case "相机2OK图保存路径": 只显示.相机2OK图保存路径 = val; break;
                    case "相机2NG图保存路径": 只显示.相机2NG图保存路径 = val; break;
                    case "相机3原图保存路径": 只显示.相机3原图保存路径 = val; break;
                    case "相机3OK图保存路径": 只显示.相机3OK图保存路径 = val; break;
                    case "相机3NG图保存路径": 只显示.相机3NG图保存路径 = val; break;
                    case "相机4原图保存路径": 只显示.相机4原图保存路径 = val; break;
                    case "相机4OK图保存路径": 只显示.相机4OK图保存路径 = val; break;
                    case "相机4NG图保存路径": 只显示.相机4NG图保存路径 = val; break;




                    case "相机1hdl文件路径": 只显示.相机1hdl文件路径 = val; break;
                    case "相机1hdict文件路径": 只显示.相机1hdict文件路径 = val; break;
                    case "相机2hdl文件路径": 只显示.相机2hdl文件路径 = val; break;
                    case "相机2hdict文件路径": 只显示.相机2hdict文件路径 = val; break;
                    case "相机3hdl文件路径": 只显示.相机3hdl文件路径 = val; break;
                    case "相机3hdict文件路径": 只显示.相机3hdict文件路径 = val; break;
                    case "相机4hdl文件路径": 只显示.相机4hdl文件路径 = val; break;
                    case "相机4hdict文件路径": 只显示.相机4hdict文件路径 = val; break;

                    case "相机1模板保存路径": 只显示.相机1模板保存路径 = val; break;
                    case "相机2模板保存路径": 只显示.相机2模板保存路径 = val; break;
                    case "相机3模板保存路径": 只显示.相机3模板保存路径 = val; break;
                    case "相机4模板保存路径": 只显示.相机4模板保存路径 = val; break;


                    case "权限时间": 只显示.权限时间 = val; break;
                    case "图片删除日期": 只显示.图片删除日期 = val; break;
                    case "相机数量设置": 只显示.相机数量设置 = val; break;
                    }
                }
            }
        }

        //=============================================================================
        // 保存为新料号（你原来的）
        //=============================================================================
        public static bool 保存全局变量为新料号(string 新料号, out string 消息)
        {
            消息 = "";
            if (string.IsNullOrWhiteSpace(新料号)) { 消息 = "料号不能为空"; return false; }
            if (检查料号是否存在(新料号)) { 消息 = "料号已存在"; return false; }

            List<string> 新配方 = new List<string>
            {
                "", "[配方开始]",
                $"料号={新料号}",
                $"相机1曝光时间={数据变量.相机1曝光时间}",
                $"相机1增益={数据变量.相机1增益}",
                $"相机2曝光时间={数据变量.相机2曝光时间}",
                $"相机2增益={数据变量.相机2增益}",
                $"相机3曝光时间={数据变量.相机3曝光时间}",
                $"相机3增益={数据变量.相机3增益}",
                $"相机4曝光时间={数据变量.相机4曝光时间}",
                $"相机4增益={数据变量.相机4增益}",

                $"相机1模板数量={数据变量.相机1模板数量}",
                $"相机1对比度={数据变量.相机1对比度}",
                $"相机1匹配度={数据变量.相机1匹配度}",
                $"相机2模板数量={数据变量.相机2模板数量}",
                $"相机2对比度={数据变量.相机2对比度}",
                $"相机2匹配度={数据变量.相机2匹配度}",
                $"相机3模板数量={数据变量.相机3模板数量}",
                $"相机3对比度={数据变量.相机3对比度}",
                $"相机3匹配度={数据变量.相机3匹配度}",
                $"相机4模板数量={数据变量.相机4模板数量}",
                $"相机4对比度={数据变量.相机4对比度}",
                $"相机4匹配度={数据变量.相机4匹配度}",

                $"相机1原图保存路径={数据变量.相机1原图保存路径}",
                $"相机1OK图保存路径={数据变量.相机1OK图保存路径}",
                $"相机1NG图保存路径={数据变量.相机1NG图保存路径}",
                $"相机2原图保存路径={数据变量.相机2原图保存路径}",
                $"相机2OK图保存路径={数据变量.相机2OK图保存路径}",
                $"相机2NG图保存路径={数据变量.相机2NG图保存路径}",
                $"相机3原图保存路径={数据变量.相机3原图保存路径}",
                $"相机3OK图保存路径={数据变量.相机3OK图保存路径}",
                $"相机3NG图保存路径={数据变量.相机3NG图保存路径}",
                $"相机4原图保存路径={数据变量.相机4原图保存路径}",
                $"相机4OK图保存路径={数据变量.相机4OK图保存路径}",
                $"相机4NG图保存路径={数据变量.相机4NG图保存路径}",

                $"相机1hdl文件路径={数据变量.相机1hdl文件路径}",
                $"相机1hdict文件路径={数据变量.相机1hdict文件路径}",
                $"相机2hdl文件路径={数据变量.相机2hdl文件路径}",
                $"相机2hdict文件路径={数据变量.相机2hdict文件路径}",
                $"相机3hdl文件路径={数据变量.相机3hdl文件路径}",
                $"相机3hdict文件路径={数据变量.相机3hdict文件路径}",
                $"相机4hdl文件路径={数据变量.相机4hdl文件路径}",
                $"相机4hdict文件路径={数据变量.相机4hdict文件路径}",

                $"相机1模板保存路径={数据变量.相机1模板保存路径}",
                $"相机2模板保存路径={数据变量.相机2模板保存路径}",
                $"相机3模板保存路径={数据变量.相机3模板保存路径}",
                $"相机4模板保存路径={数据变量.相机4模板保存路径}",

               
                $"权限时间={数据变量.权限时间}",
                $"图片删除日期={数据变量.图片删除日期}",
                $"相机数量设置={数据变量.相机数量设置}",
                "[配方结束]"
            };

            File.AppendAllLines(filePath, 新配方);
            消息 = "保存成功";
            return true;
        }

        //=============================================================================
        // ✅ 新增：保存修改（覆盖当前料号）
        //=============================================================================
        public static bool 保存当前料号修改(out string 消息)
        {
            消息 = "";
            string targetName = 数据变量.料号名称;
            if (string.IsNullOrEmpty(targetName)) { 消息 = "未选择料号"; return false; }
            if (!File.Exists(filePath)) { 消息 = "文件不存在"; return false; }

            var lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();
            var newLines = new List<string>();
            bool inRecipe = false;
            bool isTargetRecipe = false;

            foreach (var line in lines)
            {
                string trimLine = line.Trim();

                // 进入配方
                if (trimLine == "[配方开始]")
                {
                    inRecipe = true;
                    isTargetRecipe = false;
                    newLines.Add(line);
                    continue;
                }

                // 离开配方
                if (trimLine == "[配方结束]")
                {
                    inRecipe = false;
                    newLines.Add(line);
                    continue;
                }

                // 不在配方里 → 原样保留
                if (!inRecipe)
                {
                    newLines.Add(line);
                    continue;
                }

                // ==============================================
                // 判断是不是【当前要修改的料号】
                // ==============================================
                if (trimLine.StartsWith("料号="))
                {
                    string name = trimLine.Split('=')[1].Trim();
                    if (name == targetName)
                    {
                        // ✅ 找到目标：写入新内容
                        isTargetRecipe = true;
                        newLines.Add($"料号={targetName}");
                        newLines.Add($"相机1曝光时间={数据变量.相机1曝光时间}");
                        newLines.Add($"相机1增益={数据变量.相机1增益}");
                        newLines.Add($"相机2曝光时间={数据变量.相机2曝光时间}");
                        newLines.Add($"相机2增益={数据变量.相机2增益}");
                        newLines.Add($"相机3曝光时间={数据变量.相机3曝光时间}");
                        newLines.Add($"相机3增益={数据变量.相机3增益}");
                        newLines.Add($"相机4曝光时间={数据变量.相机4曝光时间}");
                        newLines.Add($"相机4增益={数据变量.相机4增益}");

                        newLines.Add($"相机1模板数量={数据变量.相机1模板数量}");
                        newLines.Add($"相机1对比度={数据变量.相机1对比度}");
                        newLines.Add($"相机1匹配度={数据变量.相机1匹配度}");
                        newLines.Add($"相机2模板数量={数据变量.相机2模板数量}");
                        newLines.Add($"相机2对比度={数据变量.相机2对比度}");
                        newLines.Add($"相机2匹配度={数据变量.相机2匹配度}");
                        newLines.Add($"相机3模板数量={数据变量.相机3模板数量}");
                        newLines.Add($"相机3对比度={数据变量.相机3对比度}");
                        newLines.Add($"相机3匹配度={数据变量.相机3匹配度}");
                        newLines.Add($"相机4模板数量={数据变量.相机4模板数量}");
                        newLines.Add($"相机4对比度={数据变量.相机4对比度}");
                        newLines.Add($"相机4匹配度={数据变量.相机4匹配度}");

                        newLines.Add($"相机1原图保存路径={数据变量.相机1原图保存路径}");
                        newLines.Add($"相机1OK图保存路径={数据变量.相机1OK图保存路径}");
                        newLines.Add($"相机1NG图保存路径={数据变量.相机1NG图保存路径}");
                        newLines.Add($"相机2原图保存路径={数据变量.相机2原图保存路径}");
                        newLines.Add($"相机2OK图保存路径={数据变量.相机2OK图保存路径}");
                        newLines.Add($"相机2NG图保存路径={数据变量.相机2NG图保存路径}");
                        newLines.Add($"相机3原图保存路径={数据变量.相机3原图保存路径}");
                        newLines.Add($"相机3OK图保存路径={数据变量.相机3OK图保存路径}");
                        newLines.Add($"相机3NG图保存路径={数据变量.相机3NG图保存路径}");
                        newLines.Add($"相机4原图保存路径={数据变量.相机4原图保存路径}");
                        newLines.Add($"相机4OK图保存路径={数据变量.相机4OK图保存路径}");
                        newLines.Add($"相机4NG图保存路径={数据变量.相机4NG图保存路径}");

                        newLines.Add($"相机1hdl文件路径={数据变量.相机1hdl文件路径}");
                        newLines.Add($"相机1hdict文件路径={数据变量.相机1hdict文件路径}");
                        newLines.Add($"相机2hdl文件路径={数据变量.相机2hdl文件路径}");
                        newLines.Add($"相机2hdict文件路径={数据变量.相机2hdict文件路径}");
                        newLines.Add($"相机3hdl文件路径={数据变量.相机3hdl文件路径}");
                        newLines.Add($"相机3hdict文件路径={数据变量.相机3hdict文件路径}");
                        newLines.Add($"相机4hdl文件路径={数据变量.相机4hdl文件路径}");
                        newLines.Add($"相机4hdict文件路径={数据变量.相机4hdict文件路径}");

                        newLines.Add($"相机1模板保存路径={数据变量.相机1模板保存路径}");
                        newLines.Add($"相机2模板保存路径={数据变量.相机2模板保存路径}");
                        newLines.Add($"相机3模板保存路径={数据变量.相机3模板保存路径}");
                        newLines.Add($"相机4模板保存路径={数据变量.相机4模板保存路径}");

                        newLines.Add($"权限时间={数据变量.权限时间}");
                        newLines.Add($"图片删除日期={数据变量.图片删除日期}");
                        newLines.Add($"相机数量设置={数据变量.相机数量设置}");
                        continue;
                    }
                }

                // ==============================================
                // 其他料号：完全原样保留，绝对不动！
                // ==============================================
                if (!isTargetRecipe)
                    newLines.Add(line);
            }

            File.WriteAllLines(filePath, newLines, Encoding.UTF8);
            消息 = "保存修改成功！";
            return true;
        }

        //=============================================================================
        // ✅ 新增：删除料号
        //=============================================================================
        public static bool 删除料号(string 料号, out string 消息)
        {
            消息 = "";
            if (string.IsNullOrEmpty(料号)) { 消息 = "未选择料号"; return false; }
            if (!File.Exists(filePath)) { 消息 = "文件不存在"; return false; }

            var allLines = File.ReadAllLines(filePath).ToList();
            List<string> newLines = new List<string>();
            bool inRecipe = false;
            bool needDelete = false;

            foreach (var line in allLines)
            {
                string trim = line.Trim();
                if (trim == "[配方开始]") inRecipe = true;

                if (inRecipe && trim.StartsWith("料号="))
                {
                    string no = trim.Split('=')[1].Trim();
                    needDelete = (no == 料号);
                }

                if (needDelete)
                {
                    if (trim == "[配方结束]")
                    {
                        needDelete = false;
                        inRecipe = false;
                    }
                    continue;
                }

                newLines.Add(line);
                if (trim == "[配方结束]") inRecipe = false;
            }

            File.WriteAllLines(filePath, newLines);
            消息 = "删除成功";
            return true;
        }

        //=============================================================================
        // 获取所有料号（你原来的）
        //=============================================================================
        public static List<string> 获取所有料号()
        {
            List<string> list = new List<string>();
            if (!File.Exists(filePath)) return list;
            foreach (var line in File.ReadAllLines(filePath))
            {
                string t = line.Trim();
                if (t.StartsWith("料号=")) list.Add(t.Split('=')[1].Trim());
            }
            return list;
        }

        //=============================================================================
        // 检查料号是否存在（你原来的）
        //=============================================================================
        public static bool 检查料号是否存在(string 料号)
        {
            return 获取所有料号().Contains(料号);
        }
    }
}
