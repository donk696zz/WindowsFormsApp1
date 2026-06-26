using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace WindowsFormsApp1
{
    public static class OpenCvImageHelper
    {
        public static Mat ConvertPtrToMat(IntPtr imageData, int width, int height, int channels = 1)
        {
            if (imageData == IntPtr.Zero)
                throw new ArgumentException("图像内存地址为空。", nameof(imageData));
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "图像尺寸必须大于0。");
            if (channels != 1 && channels != 3)
                throw new NotSupportedException($"当前只支持1或3通道图像，实际通道数：{channels}。");

            MatType type = channels == 1 ? MatType.CV_8UC1 : MatType.CV_8UC3;
            using (var source = new Mat(height, width, type, imageData))
                return source.Clone();
        }

        public static Bitmap ConvertMatToBitmap(Mat source)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("待显示图像为空。", nameof(source));

            using (var bgr = EnsureBgr8(source))
            {
                var bitmap = new Bitmap(bgr.Width, bgr.Height, PixelFormat.Format24bppRgb);
                BitmapData target = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                try
                {
                    int bytesPerRow = bgr.Width * 3;
                    byte[] row = new byte[bytesPerRow];
                    for (int y = 0; y < bgr.Height; y++)
                    {
                        Marshal.Copy(bgr.Ptr(y), row, 0, bytesPerRow);
                        Marshal.Copy(row, 0, IntPtr.Add(target.Scan0, y * target.Stride), bytesPerRow);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(target);
                }
                return bitmap;
            }
        }

        public static Mat LoadImage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("图片文件不存在。", filePath);
            Mat image = Cv2.ImRead(filePath, ImreadModes.Color);
            if (image.Empty())
            {
                image.Dispose();
                throw new InvalidDataException("OpenCV无法读取该图片。");
            }
            return image;
        }

        public static void SaveImage(Mat image, string filePath)
        {
            if (image == null || image.Empty())
                throw new ArgumentException("待保存图像为空。", nameof(image));
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            if (!Cv2.ImWrite(filePath, image))
                throw new IOException("OpenCV保存图片失败：" + filePath);
        }

        public static Mat EnsureBgr8(Mat source)
        {
            var result = new Mat();
            if (source.Type() == MatType.CV_8UC3)
                source.CopyTo(result);
            else if (source.Type() == MatType.CV_8UC1)
                Cv2.CvtColor(source, result, ColorConversionCodes.GRAY2BGR);
            else if (source.Channels() == 4)
                Cv2.CvtColor(source, result, ColorConversionCodes.BGRA2BGR);
            else
                source.ConvertTo(result, MatType.CV_8UC3);
            return result;
        }
    }
    //区域框定义
    public enum ModuleRegionType
    {
        Module,
        LeftSilverTopEdge,
        LeftSilverBottomEdge,
        LeftSilverSideEdge,
        LeftSilver,
        Middle,
        RightSilver,
        RightSilverTopEdge,
        RightSilverBottomEdge,
        RightSilverSideEdge
    }

    public sealed class ModuleRegion
    {
        public ModuleRegionType Type { get; set; }
        public Rect Box { get; set; }
    }

    public sealed class ModuleRegionResult
    {
        public ModuleRegionResult()
        {
            Regions = new System.Collections.Generic.List<ModuleRegion>();
        }

        public Rect ModuleBox { get; set; }
        public System.Collections.Generic.List<ModuleRegion> Regions { get; private set; }
    }

    public static class ModuleRegionLocator
    {
        private const int DarkThreshold = 145;
        private const double SilverOuterEdgeRatio = 0.029;
        private const double SilverInnerEdgeRatio = 0.335;
        private const double MiddleWidthRatio = 0.27;
        private const double TopEdgeRatio = 0.055;
        private const double BottomEdgeRatio = 0.055;

        public static ModuleRegionResult Locate(Mat source)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("source image is empty.", nameof(source));

            using (Mat bgr = OpenCvImageHelper.EnsureBgr8(source))
            using (Mat gray = new Mat())
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                Rect module = LocateModuleBox(gray);
                return BuildRegions(module, gray.Width, gray.Height);
            }
        }

        public static Mat DrawRegions(Mat source, ModuleRegionResult result)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("source image is empty.", nameof(source));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            Mat output = OpenCvImageHelper.EnsureBgr8(source);
            foreach (ModuleRegion region in result.Regions)
            {
                Scalar color = GetColor(region.Type);
                Cv2.Rectangle(output, region.Box, color, 2);
                Cv2.PutText(output, GetLabel(region.Type),
                    new OpenCvSharp.Point(region.Box.X + 3, Math.Max(14, region.Box.Y - 4)),
                    HersheyFonts.HersheySimplex, 0.42, color, 1);
            }
            return output;
        }

        private static Rect LocateModuleBox(Mat gray)
        {
            int width = gray.Width;
            int height = gray.Height;
            int[] columns = new int[width];
            int[] rows = new int[height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (gray.At<byte>(y, x) < DarkThreshold)
                {
                    columns[x]++;
                    rows[y]++;
                }
            }

            int columnThreshold = Math.Max(8, (int)(height * 0.025));
            int rowThreshold = Math.Max(20, (int)(width * 0.04));
            Rect xRun = FindOuterSpan(Smooth(columns, 8), columnThreshold, 8, 5);
            Rect yRun = FindOuterSpan(Smooth(rows, 5), rowThreshold, 6, 4);

            return ClampRect(new Rect(xRun.X, yRun.X, xRun.Width, yRun.Width), width, height);
        }

        private static ModuleRegionResult BuildRegions(Rect module, int imageWidth, int imageHeight)
        {
            int silverOuterOffset = Math.Max(0, (int)Math.Round(module.Width * SilverOuterEdgeRatio));
            int silverInnerOffset = Math.Max(silverOuterOffset + 1, (int)Math.Round(module.Width * SilverInnerEdgeRatio));
            int topEdgeHeight = Math.Max(1, (int)Math.Round(module.Height * TopEdgeRatio));
            int bottomEdgeHeight = Math.Max(1, (int)Math.Round(module.Height * BottomEdgeRatio));
            int middleWidth = Math.Max(1, (int)Math.Round(module.Width * MiddleWidthRatio));
            int contentY = module.Y + topEdgeHeight;
            int contentHeight = Math.Max(1, module.Height - topEdgeHeight - bottomEdgeHeight);

            Rect leftSilver = ClampRect(new Rect(
                module.X + silverOuterOffset,
                contentY,
                silverInnerOffset - silverOuterOffset,
                contentHeight), imageWidth, imageHeight);
            Rect rightSilver = ClampRect(new Rect(
                module.X + module.Width - silverInnerOffset,
                contentY,
                silverInnerOffset - silverOuterOffset,
                contentHeight), imageWidth, imageHeight);
            Rect middle = ClampRect(new Rect(
                module.X + (module.Width - middleWidth) / 2,
                contentY,
                middleWidth,
                contentHeight), imageWidth, imageHeight);
            Rect leftTopEdge = ClampRect(new Rect(
                module.X,
                module.Y,
                silverInnerOffset,
                topEdgeHeight), imageWidth, imageHeight);
            Rect leftBottomEdge = ClampRect(new Rect(
                module.X,
                module.Y + module.Height - bottomEdgeHeight,
                silverInnerOffset,
                bottomEdgeHeight), imageWidth, imageHeight);
            Rect leftSideEdge = ClampRect(new Rect(
                module.X,
                contentY,
                leftSilver.X - module.X,
                contentHeight), imageWidth, imageHeight);
            Rect rightTopEdge = ClampRect(new Rect(
                module.X + module.Width - silverInnerOffset,
                module.Y,
                silverInnerOffset,
                topEdgeHeight), imageWidth, imageHeight);
            Rect rightBottomEdge = ClampRect(new Rect(
                module.X + module.Width - silverInnerOffset,
                module.Y + module.Height - bottomEdgeHeight,
                silverInnerOffset,
                bottomEdgeHeight), imageWidth, imageHeight);
            Rect rightSideEdge = ClampRect(new Rect(
                rightSilver.X + rightSilver.Width,
                contentY,
                module.X + module.Width - (rightSilver.X + rightSilver.Width),
                contentHeight), imageWidth, imageHeight);

            var result = new ModuleRegionResult { ModuleBox = module };
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.Module, Box = module });
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.LeftSilverTopEdge, Box = leftTopEdge });
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.LeftSilverBottomEdge, Box = leftBottomEdge });
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.LeftSilverSideEdge, Box = leftSideEdge });
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.LeftSilver, Box = leftSilver });
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.Middle, Box = middle });
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.RightSilver, Box = rightSilver });
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.RightSilverTopEdge, Box = rightTopEdge });
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.RightSilverBottomEdge, Box = rightBottomEdge });
            result.Regions.Add(new ModuleRegion { Type = ModuleRegionType.RightSilverSideEdge, Box = rightSideEdge });
            return result;
        }

        private static Rect RatioRect(Rect parent, float x, float y, float width, float height,
            int imageWidth, int imageHeight)
        {
            var rect = new Rect(
                parent.X + (int)Math.Round(parent.Width * x),
                parent.Y + (int)Math.Round(parent.Height * y),
                Math.Max(1, (int)Math.Round(parent.Width * width)),
                Math.Max(1, (int)Math.Round(parent.Height * height)));
            return ClampRect(rect, imageWidth, imageHeight);
        }

        private static int[] Smooth(int[] values, int radius)
        {
            int[] output = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                int sum = 0;
                int count = 0;
                for (int j = Math.Max(0, i - radius); j <= Math.Min(values.Length - 1, i + radius); j++)
                {
                    sum += values[j];
                    count++;
                }
                output[i] = sum / count;
            }
            return output;
        }

        private static Rect FindLongestRun(int[] values, int threshold)
        {
            int bestStart = 0;
            int bestLength = 1;
            int start = -1;
            for (int i = 0; i <= values.Length; i++)
            {
                bool active = i < values.Length && values[i] >= threshold;
                if (active && start < 0)
                    start = i;
                if ((!active || i == values.Length) && start >= 0)
                {
                    int length = i - start;
                    if (length > bestLength)
                    {
                        bestStart = start;
                        bestLength = length;
                    }
                    start = -1;
                }
            }
            return new Rect(bestStart, 0, bestLength, 1);
        }

        private static Rect FindOuterSpan(int[] values, int threshold, int supportRadius, int minSupport)
        {
            int first = -1;
            int last = -1;
            for (int i = 0; i < values.Length; i++)
            {
                if (!HasLocalSupport(values, i, threshold, supportRadius, minSupport))
                    continue;

                if (first < 0)
                    first = i;
                last = i;
            }

            if (first >= 0 && last >= first)
                return new Rect(first, 0, last - first + 1, 1);

            return FindLongestRun(values, threshold);
        }

        private static bool HasLocalSupport(int[] values, int center, int threshold, int radius, int minSupport)
        {
            int support = 0;
            int start = Math.Max(0, center - radius);
            int end = Math.Min(values.Length - 1, center + radius);
            for (int i = start; i <= end; i++)
            {
                if (values[i] >= threshold)
                    support++;
            }
            return support >= minSupport;
        }

        private static Rect ClampRect(Rect rect, int width, int height)
        {
            int x1 = Math.Max(0, Math.Min(width - 1, rect.X));
            int y1 = Math.Max(0, Math.Min(height - 1, rect.Y));
            int x2 = Math.Max(x1 + 1, Math.Min(width, rect.X + rect.Width));
            int y2 = Math.Max(y1 + 1, Math.Min(height, rect.Y + rect.Height));
            return new Rect(x1, y1, x2 - x1, y2 - y1);
        }

        private static Scalar GetColor(ModuleRegionType type)
        {
            switch (type)
            {
                case ModuleRegionType.Module:
                    return Scalar.DodgerBlue;
                case ModuleRegionType.LeftSilverTopEdge:
                case ModuleRegionType.LeftSilverBottomEdge:
                case ModuleRegionType.LeftSilverSideEdge:
                case ModuleRegionType.RightSilverTopEdge:
                case ModuleRegionType.RightSilverBottomEdge:
                case ModuleRegionType.RightSilverSideEdge:
                    return Scalar.Orange;
                case ModuleRegionType.Middle:
                    return Scalar.Lime;
                default:
                    return Scalar.Red;
            }
        }

        private static string GetLabel(ModuleRegionType type)
        {
            switch (type)
            {
                case ModuleRegionType.Module:
                    return "module";
                case ModuleRegionType.LeftSilverTopEdge:
                    return "L top";
                case ModuleRegionType.LeftSilverBottomEdge:
                    return "L bottom";
                case ModuleRegionType.LeftSilverSideEdge:
                    return "L edge";
                case ModuleRegionType.LeftSilver:
                    return "L silver";
                case ModuleRegionType.Middle:
                    return "middle";
                case ModuleRegionType.RightSilver:
                    return "R silver";
                case ModuleRegionType.RightSilverTopEdge:
                    return "R top";
                case ModuleRegionType.RightSilverBottomEdge:
                    return "R bottom";
                case ModuleRegionType.RightSilverSideEdge:
                    return "R edge";
                default:
                    return type.ToString();
            }
        }
    }

    public sealed class ModuleInspectionResult
    {
        public ModuleInspectionResult()
        {
            Reasons = new System.Collections.Generic.List<string>();
            ErrorRegions = new System.Collections.Generic.List<ModuleErrorRegion>();
        }

        public bool IsOk { get; set; }
        public System.Collections.Generic.List<string> Reasons { get; private set; }
        public double MiddleSilverRatio { get; set; }
        public double LeftSilverCoverage { get; set; }
        public double RightSilverCoverage { get; set; }
        public double LeftMaxMissingRatio { get; set; }
        public double RightMaxMissingRatio { get; set; }
        public double LeftInnerDefectRatio { get; set; }
        public double RightInnerDefectRatio { get; set; }
        public double EdgeSilverRatio { get; set; }
        public string EdgeSilverRegion { get; set; }
        public ModuleRegionResult Regions { get; set; }
        public System.Collections.Generic.List<ModuleErrorRegion> ErrorRegions { get; private set; }
        public Mat AnnotatedImage { get; set; }
        public Mat ErrorImage { get; set; }

        public string ReasonText
        {
            get { return Reasons.Count == 0 ? "OK" : string.Join("; ", Reasons); }
        }
    }

    public sealed class ModuleErrorRegion
    {
        public Rect Box { get; set; }
        public string Label { get; set; }
    }

    public static class ModuleInspector
    {
        private const double MiddleSilverMaxRatio = 0.01;
        private const double SilverCoverageMinRatio = 0.80;
        private const double SilverMaxMissingBlobRatio = 0.10;
        private const double SilverInnerDefectMaxRatio = 0.006;
        private const double EdgeSilverMaxRatio = 0.05;

        public static ModuleInspectionResult Inspect(Mat source)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("source image is empty.", nameof(source));

            ModuleRegionResult regions = ModuleRegionLocator.Locate(source);
            using (Mat bgr = OpenCvImageHelper.EnsureBgr8(source))
            using (Mat gray = new Mat())
            using (Mat silverMask = CreateSilverMask(bgr))
            using (Mat edgeSilverMask = CreateEdgeSilverMask(bgr))
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

                Rect middle = GetRegion(regions, ModuleRegionType.Middle);
                Rect leftSilver = GetRegion(regions, ModuleRegionType.LeftSilver);
                Rect rightSilver = GetRegion(regions, ModuleRegionType.RightSilver);
                ModuleRegionType[] edgeTypes =
                {
                    ModuleRegionType.LeftSilverTopEdge,
                    ModuleRegionType.LeftSilverBottomEdge,
                    ModuleRegionType.LeftSilverSideEdge,
                    ModuleRegionType.RightSilverTopEdge,
                    ModuleRegionType.RightSilverBottomEdge,
                    ModuleRegionType.RightSilverSideEdge
                };
                Rect[] edgeRects =
                {
                    GetRegion(regions, edgeTypes[0]),
                    GetRegion(regions, edgeTypes[1]),
                    GetRegion(regions, edgeTypes[2]),
                    GetRegion(regions, edgeTypes[3]),
                    GetRegion(regions, edgeTypes[4]),
                    GetRegion(regions, edgeTypes[5])
                };
                string[] edgeNames =
                {
                    "L top",
                    "L bottom",
                    "L edge",
                    "R top",
                    "R bottom",
                    "R edge"
                };

                var result = new ModuleInspectionResult { Regions = regions };
                SilverDefectStats leftDefects = AnalyzeSilverDefects(gray, leftSilver);
                SilverDefectStats rightDefects = AnalyzeSilverDefects(gray, rightSilver);
                result.MiddleSilverRatio = MaskRatio(silverMask, middle);
                result.LeftSilverCoverage = MaskRatio(silverMask, leftSilver);
                result.RightSilverCoverage = MaskRatio(silverMask, rightSilver);
                result.LeftMaxMissingRatio = leftDefects.MissingRatio;
                result.RightMaxMissingRatio = rightDefects.MissingRatio;
                result.LeftInnerDefectRatio = leftDefects.InnerDefectRatio;
                result.RightInnerDefectRatio = rightDefects.InnerDefectRatio;
                CheckEdgeSilver(edgeSilverMask, edgeRects, edgeTypes, edgeNames, result);

                if (result.MiddleSilverRatio > MiddleSilverMaxRatio)
                {
                    result.Reasons.Add($"middle silver {result.MiddleSilverRatio:P1}");
                    AddErrorRegion(result, middle, $"middle {result.MiddleSilverRatio:P1}");
                }
                if (result.LeftSilverCoverage < SilverCoverageMinRatio)
                {
                    result.Reasons.Add($"L silver coverage low {result.LeftSilverCoverage:P1}");
                    AddErrorRegion(result, leftSilver, $"L cover {result.LeftSilverCoverage:P1}");
                }
                if (result.RightSilverCoverage < SilverCoverageMinRatio)
                {
                    result.Reasons.Add($"R silver coverage low {result.RightSilverCoverage:P1}");
                    AddErrorRegion(result, rightSilver, $"R cover {result.RightSilverCoverage:P1}");
                }
                if (result.LeftMaxMissingRatio > SilverMaxMissingBlobRatio)
                {
                    result.Reasons.Add($"L silver missing {result.LeftMaxMissingRatio:P1}");
                    AddErrorRegion(result, leftDefects.MissingBox, $"L missing {result.LeftMaxMissingRatio:P1}");
                }
                if (result.RightMaxMissingRatio > SilverMaxMissingBlobRatio)
                {
                    result.Reasons.Add($"R silver missing {result.RightMaxMissingRatio:P1}");
                    AddErrorRegion(result, rightDefects.MissingBox, $"R missing {result.RightMaxMissingRatio:P1}");
                }
                if (result.LeftInnerDefectRatio > SilverInnerDefectMaxRatio)
                {
                    result.Reasons.Add($"L silver defect {result.LeftInnerDefectRatio:P1}");
                    AddErrorRegion(result, leftDefects.InnerDefectBox, $"L defect {result.LeftInnerDefectRatio:P1}");
                }
                if (result.RightInnerDefectRatio > SilverInnerDefectMaxRatio)
                {
                    result.Reasons.Add($"R silver defect {result.RightInnerDefectRatio:P1}");
                    AddErrorRegion(result, rightDefects.InnerDefectBox, $"R defect {result.RightInnerDefectRatio:P1}");
                }

                result.IsOk = result.Reasons.Count == 0;
                result.AnnotatedImage = DrawInspection(source, result);
                result.ErrorImage = DrawErrorInspection(source, result);
                return result;
            }
        }

        private static Mat CreateSilverMask(Mat bgr)
        {
            using (Mat gray = new Mat())
            using (Mat bright = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5)))
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(3, 3), 0);
                Cv2.Threshold(gray, bright, 145, 255, ThresholdTypes.Binary);
                Cv2.MorphologyEx(bright, bright, MorphTypes.Close, kernel, iterations: 1);
                Cv2.MorphologyEx(bright, bright, MorphTypes.Open, kernel, iterations: 1);
                return bright.Clone();
            }
        }

        private static Mat CreateEdgeSilverMask(Mat bgr)
        {
            using (Mat gray = new Mat())
            using (Mat strictSilver = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(3, 3), 0);
                Cv2.Threshold(gray, strictSilver, 225, 255, ThresholdTypes.Binary);
                Cv2.MorphologyEx(strictSilver, strictSilver, MorphTypes.Open, kernel, iterations: 1);
                return strictSilver.Clone();
            }
        }

        private static Mat DrawInspection(Mat source, ModuleInspectionResult result)
        {
            Mat output = ModuleRegionLocator.DrawRegions(source, result.Regions);
            Scalar color = result.IsOk ? Scalar.LimeGreen : Scalar.Red;
            string text = result.IsOk ? "OK" : "NG";
            Cv2.PutText(output, text, new OpenCvSharp.Point(20, 45),
                HersheyFonts.HersheySimplex, 1.4, color, 3);
            if (!result.IsOk)
            {
                Cv2.PutText(output, result.Reasons[0], new OpenCvSharp.Point(20, 82),
                    HersheyFonts.HersheySimplex, 0.55, color, 2);
            }
            return output;
        }

        private static Mat DrawErrorInspection(Mat source, ModuleInspectionResult result)
        {
            Mat output = ModuleRegionLocator.DrawRegions(source, result.Regions);
            Scalar color = result.IsOk ? Scalar.LimeGreen : Scalar.Red;
            Cv2.PutText(output, result.IsOk ? "OK" : "NG", new OpenCvSharp.Point(20, 45),
                HersheyFonts.HersheySimplex, 1.4, color, 3);

            if (result.ErrorRegions.Count == 0)
            {
                Cv2.PutText(output, result.IsOk ? "no error" : result.ReasonText,
                    new OpenCvSharp.Point(20, 82), HersheyFonts.HersheySimplex, 0.55, color, 2);
                return output;
            }

            foreach (ModuleErrorRegion error in result.ErrorRegions)
            {
                Rect box = ClampRectToImage(error.Box, output.Width, output.Height);
                Cv2.Rectangle(output, box, Scalar.Red, 4);
                Cv2.PutText(output, error.Label ?? "error",
                    new OpenCvSharp.Point(box.X + 3, Math.Max(18, box.Y - 6)),
                    HersheyFonts.HersheySimplex, 0.62, Scalar.Red, 2);
            }

            return output;
        }

        private static void AddErrorRegion(ModuleInspectionResult result, Rect box, string label)
        {
            if (box.Width <= 0 || box.Height <= 0)
                return;
            result.ErrorRegions.Add(new ModuleErrorRegion { Box = box, Label = label });
        }

        private static Rect ClampRectToImage(Rect rect, int width, int height)
        {
            int x1 = Math.Max(0, Math.Min(width - 1, rect.X));
            int y1 = Math.Max(0, Math.Min(height - 1, rect.Y));
            int x2 = Math.Max(x1 + 1, Math.Min(width, rect.X + rect.Width));
            int y2 = Math.Max(y1 + 1, Math.Min(height, rect.Y + rect.Height));
            return new Rect(x1, y1, x2 - x1, y2 - y1);
        }

        private static Rect GetRegion(ModuleRegionResult regions, ModuleRegionType type)
        {
            foreach (ModuleRegion region in regions.Regions)
                if (region.Type == type)
                    return region.Box;
            throw new InvalidOperationException("region not found: " + type);
        }

        private static double MaskRatio(Mat mask, Rect rect)
        {
            using (Mat roi = new Mat(mask, rect))
            {
                return rect.Width <= 0 || rect.Height <= 0
                    ? 0
                    : Cv2.CountNonZero(roi) / (double)(rect.Width * rect.Height);
            }
        }

        private static void CheckEdgeSilver(
            Mat mask,
            Rect[] rects,
            ModuleRegionType[] types,
            string[] names,
            ModuleInspectionResult result)
        {
            result.EdgeSilverRatio = 0;
            result.EdgeSilverRegion = string.Empty;
            for (int i = 0; i < rects.Length; i++)
            {
                Rect inspectRect = GetEdgeInspectRect(rects[i], types[i]);
                double ratio = MaskRatio(mask, inspectRect);
                if (ratio > result.EdgeSilverRatio)
                {
                    result.EdgeSilverRatio = ratio;
                    result.EdgeSilverRegion = names[i];
                }
                if (ratio > EdgeSilverMaxRatio)
                {
                    result.Reasons.Add($"{names[i]} silver {ratio:P1}");
                    AddErrorRegion(result, inspectRect, $"{names[i]} {ratio:P1}");
                }
            }
        }

        private static Rect GetEdgeInspectRect(Rect rect, ModuleRegionType type)
        {
            int trim = Math.Max(2, (int)Math.Round(Math.Min(rect.Width, rect.Height) * 0.18));
            switch (type)
            {
                case ModuleRegionType.LeftSilverTopEdge:
                case ModuleRegionType.RightSilverTopEdge:
                    return new Rect(rect.X, rect.Y, rect.Width, Math.Max(1, rect.Height - trim));
                case ModuleRegionType.LeftSilverBottomEdge:
                case ModuleRegionType.RightSilverBottomEdge:
                    return new Rect(rect.X, rect.Y + Math.Min(trim, rect.Height - 1), rect.Width, Math.Max(1, rect.Height - trim));
                case ModuleRegionType.LeftSilverSideEdge:
                    return new Rect(rect.X, rect.Y, Math.Max(1, rect.Width - trim), rect.Height);
                case ModuleRegionType.RightSilverSideEdge:
                    return new Rect(rect.X + Math.Min(trim, rect.Width - 1), rect.Y, Math.Max(1, rect.Width - trim), rect.Height);
                default:
                    return rect;
            }
        }

        private sealed class SilverDefectStats
        {
            public double MissingRatio { get; set; }
            public double InnerDefectRatio { get; set; }
            public Rect MissingBox { get; set; }
            public Rect InnerDefectBox { get; set; }
        }

        private static SilverDefectStats AnalyzeSilverDefects(Mat gray, Rect rect)
        {
            Rect inner = ShrinkRect(rect, 0.08, 0.06);
            DefectCandidate missing = FindMissingBlob(gray, rect, inner);
            DefectCandidate innerDefect = FindLocalDarkDefect(gray, inner);
            return new SilverDefectStats
            {
                MissingRatio = missing.Ratio,
                InnerDefectRatio = innerDefect.Ratio,
                MissingBox = missing.Box,
                InnerDefectBox = innerDefect.Box
            };
        }

        private sealed class DefectCandidate
        {
            public double Ratio { get; set; }
            public Rect Box { get; set; }
        }

        private static DefectCandidate FindMissingBlob(Mat gray, Rect silverRect, Rect inspectRect)
        {
            using (Mat roi = new Mat(gray, inspectRect))
            using (Mat dark = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5)))
            {
                Cv2.Threshold(roi, dark, 95, 255, ThresholdTypes.BinaryInv);
                Cv2.MorphologyEx(dark, dark, MorphTypes.Close, kernel, iterations: 1);
                using (Mat labels = new Mat())
                using (Mat stats = new Mat())
                using (Mat centroids = new Mat())
                {
                    int count = Cv2.ConnectedComponentsWithStats(dark, labels, stats, centroids);
                    int maxScoreArea = 0;
                    Rect maxBox = Rect.Empty;
                    for (int i = 1; i < count; i++)
                    {
                        int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                        int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                        int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                        int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                        int height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);
                        int boxArea = width * height;
                        int scoreArea = Math.Max(area, boxArea);
                        if (scoreArea > maxScoreArea)
                        {
                            maxScoreArea = scoreArea;
                            maxBox = new Rect(inspectRect.X + x, inspectRect.Y + y, width, height);
                        }
                    }
                    double ratio = silverRect.Width <= 0 || silverRect.Height <= 0
                        ? 0
                        : maxScoreArea / (double)(silverRect.Width * silverRect.Height);
                    return new DefectCandidate { Ratio = ratio, Box = maxBox };
                }
            }
        }

        private static DefectCandidate FindLocalDarkDefect(Mat gray, Rect inner)
        {
            using (Mat roi = new Mat(gray, inner))
            using (Mat blur = new Mat())
            using (Mat background = new Mat())
            using (Mat localDiff = new Mat())
            using (Mat localDark = new Mat())
            using (Mat absoluteDark = new Mat())
            using (Mat semiDark = new Mat())
            using (Mat candidate = new Mat())
            using (Mat localCandidate = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
            {
                Cv2.GaussianBlur(roi, blur, new OpenCvSharp.Size(3, 3), 0);
                int backgroundKernel = MakeOdd(Math.Max(21, Math.Min(61, Math.Min(inner.Width, inner.Height) / 5)));
                Cv2.GaussianBlur(blur, background, new OpenCvSharp.Size(backgroundKernel, backgroundKernel), 0);
                Cv2.Subtract(background, blur, localDiff);
                Cv2.Threshold(localDiff, localDark, 30, 255, ThresholdTypes.Binary);
                Cv2.Threshold(blur, absoluteDark, 115, 255, ThresholdTypes.BinaryInv);
                Cv2.Threshold(blur, semiDark, 150, 255, ThresholdTypes.BinaryInv);
                Cv2.BitwiseAnd(localDark, semiDark, localCandidate);
                Cv2.BitwiseOr(absoluteDark, localCandidate, candidate);
                Cv2.MorphologyEx(candidate, candidate, MorphTypes.Close, kernel, iterations: 1);
                Cv2.MorphologyEx(candidate, candidate, MorphTypes.Open, kernel, iterations: 1);

                using (Mat labels = new Mat())
                using (Mat stats = new Mat())
                using (Mat centroids = new Mat())
                {
                    int count = Cv2.ConnectedComponentsWithStats(candidate, labels, stats, centroids);
                    int innerArea = Math.Max(1, inner.Width * inner.Height);
                    int minArea = Math.Max(18, (int)Math.Round(innerArea * 0.00035));
                    int maxScoreArea = 0;
                    Rect maxBox = Rect.Empty;
                    for (int i = 1; i < count; i++)
                    {
                        int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                        int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                        int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                        int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                        int height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);
                        if (area < minArea || width < 5 || height < 5)
                            continue;
                        if (IsNearSilverInnerBorder(x, y, width, height, inner.Width, inner.Height))
                            continue;

                        int boxArea = width * height;
                        int scoreArea = Math.Max(area, (int)Math.Round(boxArea * 0.65));
                        if (scoreArea > maxScoreArea)
                        {
                            maxScoreArea = scoreArea;
                            maxBox = new Rect(inner.X + x, inner.Y + y, width, height);
                        }
                    }
                    return new DefectCandidate
                    {
                        Ratio = maxScoreArea / (double)innerArea,
                        Box = maxBox
                    };
                }
            }
        }

        private static bool IsNearSilverInnerBorder(int x, int y, int width, int height, int roiWidth, int roiHeight)
        {
            int guardX = Math.Max(14, (int)Math.Round(roiWidth * 0.12));
            int guardY = Math.Max(14, (int)Math.Round(roiHeight * 0.12));
            return x <= guardX ||
                y <= guardY ||
                x + width >= roiWidth - guardX ||
                y + height >= roiHeight - guardY;
        }

        private static int MakeOdd(int value)
        {
            if (value < 3)
                value = 3;
            return value % 2 == 0 ? value + 1 : value;
        }

        private static Rect ShrinkRect(Rect rect, double xRatio, double yRatio)
        {
            int dx = Math.Max(1, (int)Math.Round(rect.Width * xRatio));
            int dy = Math.Max(1, (int)Math.Round(rect.Height * yRatio));
            int width = Math.Max(1, rect.Width - dx * 2);
            int height = Math.Max(1, rect.Height - dy * 2);
            return new Rect(rect.X + dx, rect.Y + dy, width, height);
        }
    }
}
