using OpenCvSharp;
using System;
using System.IO;
using System.Linq;

namespace WindowsFormsApp1
{
    /// <summary>
    /// OpenCV离线数据整理工具。DEIM直接检测整图，不再依赖Halcon形状模板裁图。
    /// </summary>
    public class 裁图类
    {
        public static 信息 日志信息;

        public void 截图分类(int 相机, string 保存路径)
        {
            string sourcePath = 获取原图路径(相机);
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                日志信息?.AddLog(0, $"相机{相机}原图目录不存在。");
                return;
            }
            if (string.IsNullOrWhiteSpace(保存路径))
            {
                日志信息?.AddLog(0, "数据集保存目录不能为空。");
                return;
            }

            Directory.CreateDirectory(保存路径);
            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff" };
            string[] files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)
                .Where(x => extensions.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
                .ToArray();

            int success = 0;
            foreach (string file in files)
            {
                try
                {
                    using (Mat image = OpenCvImageHelper.LoadImage(file))
                    {
                        string target = Path.Combine(保存路径,
                            $"相机{相机}_{DateTime.Now:yyyyMMddHHmmssfff}_{success:D6}.jpg");
                        OpenCvImageHelper.SaveImage(image, target);
                        success++;
                    }
                }
                catch (Exception ex)
                {
                    日志信息?.AddLog(0, "图片整理失败：" + ex.Message);
                }
            }
            日志信息?.AddLog(1, $"相机{相机}数据整理完成，共{success}张。");
        }

        private static string 获取原图路径(int cameraNumber)
        {
            switch (cameraNumber)
            {
                case 1: return 数据变量.相机1原图保存路径;
                case 2: return 数据变量.相机2原图保存路径;
                case 3: return 数据变量.相机3原图保存路径;
                case 4: return 数据变量.相机4原图保存路径;
                default: return null;
            }
        }
    }
}
