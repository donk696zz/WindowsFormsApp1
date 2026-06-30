using System;
using System.IO;
using System.Windows.Forms;
using OpenCvSharp;

namespace WindowsFormsApp1
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length >= 2 &&
                (string.Equals(args[0], "--test-image", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(args[0], "--inspect-module", StringComparison.OrdinalIgnoreCase)))
            {
                string outputPath = args.Length >= 3 ? args[2] : null;
                运行模块检测(args[1], outputPath);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            数据变量.从参数模型同步();
            var materials = 料号切换读取文件.获取所有料号();
            string material = Properties.Settings.Default.当前料号;
            if (materials.Count == 0)
            {
                料号切换读取文件.保存全局变量为新料号("默认料号", out _);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(material) || !materials.Contains(material))
                    material = materials[0];
                料号切换读取文件.加载料号到全局变量(material, false);
            }
            Application.Run(new 底层页面());
        }

        private static void 运行模块检测(string imagePath, string outputPath)
        {
            using (Mat image = OpenCvImageHelper.LoadImage(imagePath))
            {
                ModuleInspectionResult result = ModuleInspector.Inspect(image);
                using (result.AnnotatedImage)
                using (result.ErrorImage)
                {
                    string report =
                        $"Result={GetDecisionName(result.Decision)}{Environment.NewLine}" +
                        $"Reason={result.ReasonText}{Environment.NewLine}" +
                        $"ReviewReason={string.Join("; ", result.ReviewReasons)}{Environment.NewLine}" +
                        $"MiddleSilver={result.MiddleSilverRatio:P3}{Environment.NewLine}" +
                        $"LeftCoverage={result.LeftSilverCoverage:P3}{Environment.NewLine}" +
                        $"RightCoverage={result.RightSilverCoverage:P3}{Environment.NewLine}" +
                        $"LeftTopCoverage={result.LeftSilverTopCoverage:P3}{Environment.NewLine}" +
                        $"LeftBottomCoverage={result.LeftSilverBottomCoverage:P3}{Environment.NewLine}" +
                        $"RightTopCoverage={result.RightSilverTopCoverage:P3}{Environment.NewLine}" +
                        $"RightBottomCoverage={result.RightSilverBottomCoverage:P3}{Environment.NewLine}" +
                        $"LeftMissing={result.LeftMaxMissingRatio:P3}{Environment.NewLine}" +
                        $"RightMissing={result.RightMaxMissingRatio:P3}{Environment.NewLine}" +
                        $"LeftInnerDefect={result.LeftInnerDefectRatio:P3}{Environment.NewLine}" +
                        $"RightInnerDefect={result.RightInnerDefectRatio:P3}{Environment.NewLine}" +
                        $"MaxEdgeSilver={result.EdgeSilverRegion} {result.EdgeSilverRatio:P3}{Environment.NewLine}";

                    if (!string.IsNullOrWhiteSpace(outputPath))
                    {
                        string outputDirectory = GetOutputDirectory(outputPath);
                        Directory.CreateDirectory(outputDirectory);
                        File.WriteAllText(outputPath, report);
                        string imageOutput = Path.Combine(
                            outputDirectory,
                            Path.GetFileNameWithoutExtension(outputPath) + "_annotated.bmp");
                        OpenCvImageHelper.SaveImage(result.AnnotatedImage, imageOutput);
                        string errorImageOutput = Path.Combine(
                            outputDirectory,
                            Path.GetFileNameWithoutExtension(outputPath) + "_errors.bmp");
                        OpenCvImageHelper.SaveImage(result.ErrorImage, errorImageOutput);
                    }
                    else
                    {
                        Console.WriteLine(report);
                    }
                }
            }
        }

        private static string GetDecisionName(ModuleInspectionDecision decision)
        {
            switch (decision)
            {
                case ModuleInspectionDecision.Ok: return "OK";
                case ModuleInspectionDecision.Ng: return "NG";
                default: return "REVIEW";
            }
        }

        private static string GetOutputDirectory(string outputPath)
        {
            string directory = Path.GetDirectoryName(outputPath);
            return string.IsNullOrWhiteSpace(directory) ? "." : directory;
        }
    }
}
