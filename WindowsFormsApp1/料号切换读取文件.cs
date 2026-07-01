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
            if (!获取所有料号().Contains(
                normalized, StringComparer.OrdinalIgnoreCase))
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

            if (normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                normalized.Contains("=") || normalized.Contains("\r") ||
                normalized.Contains("\n"))
            {
                消息 = "料号包含不允许的字符";
                return false;
            }

            List<string> materials = 获取所有料号();
            if (materials.Contains(normalized, StringComparer.OrdinalIgnoreCase))
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
            if (!materials.Contains(material, StringComparer.OrdinalIgnoreCase))
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
            int index = materials.FindIndex(x => string.Equals(
                x, normalized, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                消息 = "料号不存在";
                return false;
            }
            string removed = materials[index];
            materials.RemoveAt(index);
            if (materials.Count == 0)
                materials.Add("默认料号");
            SaveMaterials(materials);

            bool removedCurrent = string.Equals(
                数据变量.料号名称,
                removed,
                StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    Properties.Settings.Default.当前料号,
                    removed,
                    StringComparison.OrdinalIgnoreCase);
            if (removedCurrent)
            {
                string next = materials[0];
                加载料号到全局变量(next, false);
                消息 = $"料号删除成功，当前料号已切换为：{next}";
            }
            else
            {
                消息 = "料号删除成功";
            }
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
                if (!string.IsNullOrWhiteSpace(material) &&
                    !result.Contains(material, StringComparer.OrdinalIgnoreCase))
                    result.Add(material);
            }
            return result;
        }

        public static bool 检查料号是否存在(string 料号)
        {
            return 获取所有料号().Contains(
                Normalize(料号), StringComparer.OrdinalIgnoreCase);
        }

        private static void SaveMaterials(IEnumerable<string> materials)
        {
            var lines = new List<string>();
            foreach (string material in materials
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase))
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
