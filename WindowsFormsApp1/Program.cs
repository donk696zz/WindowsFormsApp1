using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
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
            if (args.Length >= 2 && string.Equals(args[0], "--test-image", StringComparison.OrdinalIgnoreCase))
            {
                运行离线检测(args[1]);
                return;
            }
            if (args.Length >= 2 && string.Equals(args[0], "--inspect-module", StringComparison.OrdinalIgnoreCase))
            {
                string outputPath = args.Length >= 3 ? args[2] : null;
                运行模块检测(args[1], outputPath);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EnsureTwoCameraMode();
            Application.Run(new 底层页面());
        }

        private static void EnsureTwoCameraMode()
        {
            const string cameraCountSettingName = "\u76f8\u673a\u6570\u91cf\u8bbe\u7f6e";
            string value = Convert.ToString(Properties.Settings.Default[cameraCountSettingName]);
            if (int.TryParse(value, out int count) && count >= 1 && count <= 2)
                return;

            Properties.Settings.Default[cameraCountSettingName] = "2";
            Properties.Settings.Default.Save();
        }

        private static void 运行离线检测(string imagePath)
        {
            const string modelPath = @"E:\source\DEIM\checkpoints\deim_dfine_hgnetv2_n_coco_160e.onnx";
            using (var detector = new DeimOnnxDetector(modelPath, 0.4f))
            using (Mat image = OpenCvImageHelper.LoadImage(imagePath))
            {
                DetectionResult result = detector.Detect(image);
                using (result.AnnotatedImage)
                {
                    string output = Path.Combine(
                        Path.GetDirectoryName(imagePath) ?? Application.StartupPath,
                        Path.GetFileNameWithoutExtension(imagePath) + "_deim_result.jpg");
                    OpenCvImageHelper.SaveImage(result.AnnotatedImage, output);
                }
            }
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
                        $"Result={(result.IsOk ? "OK" : "NG")}{Environment.NewLine}" +
                        $"Reason={result.ReasonText}{Environment.NewLine}" +
                        $"MiddleSilver={result.MiddleSilverRatio:P3}{Environment.NewLine}" +
                        $"LeftCoverage={result.LeftSilverCoverage:P3}{Environment.NewLine}" +
                        $"RightCoverage={result.RightSilverCoverage:P3}{Environment.NewLine}" +
                        $"LeftMissing={result.LeftMaxMissingRatio:P3}{Environment.NewLine}" +
                        $"RightMissing={result.RightMaxMissingRatio:P3}{Environment.NewLine}" +
                        $"LeftInnerDefect={result.LeftInnerDefectRatio:P3}{Environment.NewLine}" +
                        $"RightInnerDefect={result.RightInnerDefectRatio:P3}{Environment.NewLine}" +
                        $"MaxEdgeSilver={result.EdgeSilverRegion} {result.EdgeSilverRatio:P3}{Environment.NewLine}";

                    if (!string.IsNullOrWhiteSpace(outputPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
                        File.WriteAllText(outputPath, report);
                        string imageOutput = Path.Combine(
                            Path.GetDirectoryName(outputPath) ?? ".",
                            Path.GetFileNameWithoutExtension(outputPath) + "_annotated.bmp");
                        OpenCvImageHelper.SaveImage(result.AnnotatedImage, imageOutput);
                        string errorImageOutput = Path.Combine(
                            Path.GetDirectoryName(outputPath) ?? ".",
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
    }
}
