using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class 料号切换读取文件
    {
        private static readonly string FilePath = Path.Combine(Application.StartupPath, "料号参数.txt");

        public static bool 加载料号到全局变量(string 料号, bool 只显示)
        {
            string normalized = Normalize(料号);
            if (!获取所有料号().Contains(normalized))
                return false;

            if (!只显示)
            {
                数据变量.料号名称 = normalized;
                Properties.Settings.Default.当前料号 = normalized;
                Properties.Settings.Default.Save();
                VisionParameterStore.ActivateMaterial(normalized);
            }
            return true;
        }

        public static bool 保存全局变量为新料号(string 新料号, out string 消息)
        {
            string normalized = Normalize(新料号);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                消息 = "料号不能为空";
                return false;
            }

            List<string> materials = 获取所有料号();
            if (materials.Contains(normalized))
            {
                消息 = "料号已存在";
                return false;
            }

            materials.Add(normalized);
            SaveMaterials(materials);
            数据变量.料号名称 = normalized;
            Properties.Settings.Default.当前料号 = normalized;
            Properties.Settings.Default.Save();
            VisionParameterStore.ActivateMaterial(normalized);
            VisionParameterStore.SaveMaterialProfile(VisionParameterStore.CurrentMaterialProfile);
            消息 = "料号创建成功";
            return true;
        }

        public static bool 保存当前料号修改(out string 消息)
        {
            string material = Normalize(数据变量.料号名称);
            List<string> materials = 获取所有料号();
            if (!materials.Contains(material))
            {
                materials.Add(material);
                SaveMaterials(materials);
            }
            VisionParameterStore.SaveMaterialProfile(VisionParameterStore.CurrentMaterialProfile);
            消息 = "料号保存成功";
            return true;
        }

        public static bool 删除料号(string 料号, out string 消息)
        {
            string normalized = Normalize(料号);
            List<string> materials = 获取所有料号();
            if (!materials.Remove(normalized))
            {
                消息 = "料号不存在";
                return false;
            }
            SaveMaterials(materials);
            消息 = "料号删除成功";
            return true;
        }

        public static List<string> 获取所有料号()
        {
            if (!File.Exists(FilePath))
                return new List<string>();

            var result = new List<string>();
            foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (!line.StartsWith("料号=", StringComparison.Ordinal))
                    continue;
                string material = Normalize(line.Substring("料号=".Length));
                if (!string.IsNullOrWhiteSpace(material) && !result.Contains(material))
                    result.Add(material);
            }
            return result;
        }

        public static bool 检查料号是否存在(string 料号)
        {
            return 获取所有料号().Contains(Normalize(料号));
        }

        private static void SaveMaterials(IEnumerable<string> materials)
        {
            var lines = new List<string>();
            foreach (string material in materials.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            {
                lines.Add("[料号开始]");
                lines.Add("料号=" + Normalize(material));
                lines.Add("[料号结束]");
                lines.Add(string.Empty);
            }
            File.WriteAllLines(FilePath, lines, new UTF8Encoding(false));
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
