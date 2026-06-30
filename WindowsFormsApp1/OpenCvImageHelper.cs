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
        public static ModuleRegionResult Locate(Mat source)
        {
            return Locate(source, VisionParameterStore.CurrentMaterialProfile.Regions);
        }

        public static ModuleRegionResult Locate(Mat source, RegionParameters parameters)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("source image is empty.", nameof(source));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            parameters.Validate();

            using (Mat bgr = OpenCvImageHelper.EnsureBgr8(source))
            using (Mat gray = new Mat())
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                Rect module = LocateModuleBox(gray, parameters);
                return BuildRegions(module, gray.Width, gray.Height, parameters);
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

        private static Rect LocateModuleBox(Mat gray, RegionParameters parameters)
        {
            int width = gray.Width;
            int height = gray.Height;
            int[] columns = new int[width];
            int[] rows = new int[height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (gray.At<byte>(y, x) < parameters.ModuleDarkThreshold)
                {
                    columns[x]++;
                    rows[y]++;
                }
            }

            int columnThreshold = Math.Max(parameters.ColumnActivationMinimum,
                (int)(height * parameters.ColumnActivationRatio));
            int rowThreshold = Math.Max(parameters.RowActivationMinimum,
                (int)(width * parameters.RowActivationRatio));
            Rect xRun = FindOuterSpan(
                Smooth(columns, parameters.ColumnSmoothRadius), columnThreshold,
                parameters.ColumnSupportRadius, parameters.ColumnMinimumSupport);
            Rect yRun = FindOuterSpan(
                Smooth(rows, parameters.RowSmoothRadius), rowThreshold,
                parameters.RowSupportRadius, parameters.RowMinimumSupport);

            return ClampRect(new Rect(xRun.X, yRun.X, xRun.Width, yRun.Width), width, height);
        }

        private static ModuleRegionResult BuildRegions(
            Rect module,
            int imageWidth,
            int imageHeight,
            RegionParameters parameters)
        {
            int silverOuterOffset = Math.Max(0, (int)Math.Round(module.Width * parameters.SilverOuterEdgeRatio));
            int silverInnerOffset = Math.Max(silverOuterOffset + 1, (int)Math.Round(module.Width * parameters.SilverInnerEdgeRatio));
            int silverTopOffset = Math.Max(1, (int)Math.Round(module.Height * parameters.SilverTopRatio));
            int silverBottomOffset = Math.Max(1, (int)Math.Round(module.Height * parameters.SilverBottomRatio));
            int topInspectHeight = Math.Max(1, (int)Math.Round(module.Height * parameters.TopInspectHeightRatio));
            int bottomInspectHeight = Math.Max(1, (int)Math.Round(module.Height * parameters.BottomInspectHeightRatio));
            int sideInspectWidth = Math.Max(1, (int)Math.Round(module.Width * parameters.SideInspectWidthRatio));
            int middleWidth = Math.Max(1, (int)Math.Round(module.Width * parameters.MiddleWidthRatio));
            int contentY = module.Y + silverTopOffset;
            int contentHeight = Math.Max(1, module.Height - silverTopOffset - silverBottomOffset);

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
                topInspectHeight), imageWidth, imageHeight);
            Rect leftBottomEdge = ClampRect(new Rect(
                module.X,
                module.Y + module.Height - bottomInspectHeight,
                silverInnerOffset,
                bottomInspectHeight), imageWidth, imageHeight);
            Rect leftSideEdge = ClampRect(new Rect(
                module.X,
                contentY,
                sideInspectWidth,
                contentHeight), imageWidth, imageHeight);
            Rect rightTopEdge = ClampRect(new Rect(
                module.X + module.Width - silverInnerOffset,
                module.Y,
                silverInnerOffset,
                topInspectHeight), imageWidth, imageHeight);
            Rect rightBottomEdge = ClampRect(new Rect(
                module.X + module.Width - silverInnerOffset,
                module.Y + module.Height - bottomInspectHeight,
                silverInnerOffset,
                bottomInspectHeight), imageWidth, imageHeight);
            Rect rightSideEdge = ClampRect(new Rect(
                module.X + module.Width - sideInspectWidth,
                contentY,
                sideInspectWidth,
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
                case ModuleRegionType.LeftSilver:
                case ModuleRegionType.RightSilver:
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

    public enum ModuleInspectionDecision
    {
        Ok,
        Ng,
        Review
    }

    public sealed class ModuleInspectionResult
    {
        public ModuleInspectionResult()
        {
            Reasons = new System.Collections.Generic.List<string>();
            ReviewReasons = new System.Collections.Generic.List<string>();
            ErrorRegions = new System.Collections.Generic.List<ModuleErrorRegion>();
        }

        public bool IsOk { get; set; }
        public ModuleInspectionDecision Decision { get; set; }
        public bool RequiresReview => Decision == ModuleInspectionDecision.Review;
        public System.Collections.Generic.List<string> Reasons { get; private set; }
        public System.Collections.Generic.List<string> ReviewReasons { get; private set; }
        public double MiddleSilverRatio { get; set; }
        public double LeftSilverCoverage { get; set; }
        public double RightSilverCoverage { get; set; }
        public double LeftSilverTopCoverage { get; set; }
        public double LeftSilverBottomCoverage { get; set; }
        public double RightSilverTopCoverage { get; set; }
        public double RightSilverBottomCoverage { get; set; }
        public double LeftMaxMissingRatio { get; set; }
        public double RightMaxMissingRatio { get; set; }
        public double LeftInnerDefectRatio { get; set; }
        public double RightInnerDefectRatio { get; set; }
        public double LeftEdgeMissingRatio { get; set; }
        public double RightEdgeMissingRatio { get; set; }
        public double LeftPairedEdgeRatio { get; set; }
        public double RightPairedEdgeRatio { get; set; }
        public double LeftLineDefectRatio { get; set; }
        public double RightLineDefectRatio { get; set; }
        public bool ModuleFullyVisible { get; set; } = true;
        public double EdgeSilverRatio { get; set; }
        public string EdgeSilverRegion { get; set; }
        public ModuleRegionResult Regions { get; set; }
        public System.Collections.Generic.List<ModuleErrorRegion> ErrorRegions { get; private set; }
        public Mat AnnotatedImage { get; set; }
        public Mat ErrorImage { get; set; }

        public string ReasonText
        {
            get
            {
                if (Decision == ModuleInspectionDecision.Ng)
                    return string.Join("; ", Reasons);
                if (Decision == ModuleInspectionDecision.Review)
                    return string.Join("; ", ReviewReasons);
                return "OK";
            }
        }
    }

    public sealed class ModuleErrorRegion
    {
        public Rect Box { get; set; }
        public string Label { get; set; }
        public bool IsReview { get; set; }
    }

    public static class ModuleInspector
    {
        public static ModuleInspectionResult Inspect(Mat source)
        {
            return Inspect(
                source,
                VisionParameterStore.CurrentMaterialProfile.Regions,
                VisionParameterStore.ApplicationParameters.Inspection);
        }

        public static ModuleInspectionResult Inspect(
            Mat source,
            RegionParameters regionParameters,
            InspectionParameters inspectionParameters)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("source image is empty.", nameof(source));
            if (regionParameters == null)
                throw new ArgumentNullException(nameof(regionParameters));
            if (inspectionParameters == null)
                throw new ArgumentNullException(nameof(inspectionParameters));

            regionParameters.Validate();
            inspectionParameters.Validate();

            ModuleRegionResult regions = ModuleRegionLocator.Locate(source, regionParameters);
            using (Mat bgr = OpenCvImageHelper.EnsureBgr8(source))
            using (Mat gray = new Mat())
            using (Mat silverMask = CreateSilverMask(bgr, inspectionParameters.SilverGrayThreshold))
            using (Mat edgeSilverMask = CreateEdgeSilverMask(bgr, inspectionParameters.EdgeSilverGrayThreshold))
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

                Rect middle = GetRegion(regions, ModuleRegionType.Middle);
                Rect middleInspect = GetMiddleInspectRect(middle, regionParameters.MiddleInspectHorizontalInsetRatio);
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
                AdaptiveSilverEvaluation adaptive = AdaptiveSilverNormalModel.Evaluate(
                    gray,
                    leftSilver,
                    rightSilver,
                    inspectionParameters);
                SplitSilverRectVertically(leftSilver, regionParameters.SilverVerticalSplitRatio, out Rect leftSilverTop, out Rect leftSilverBottom);
                SplitSilverRectVertically(rightSilver, regionParameters.SilverVerticalSplitRatio, out Rect rightSilverTop, out Rect rightSilverBottom);
                Rect middleErrorBox;
                using (Mat middleSilverMask = CreateMiddleSilverMask(gray, middleInspect, inspectionParameters))
                {
                    result.MiddleSilverRatio = MaskRatio(middleSilverMask, middleInspect);
                    middleErrorBox = FindLargestMaskBlobBox(middleSilverMask, middleInspect);
                }
                result.LeftSilverCoverage = MaskRatio(silverMask, leftSilver);
                result.RightSilverCoverage = MaskRatio(silverMask, rightSilver);
                result.LeftSilverTopCoverage = MaskRatio(silverMask, leftSilverTop);
                result.LeftSilverBottomCoverage = MaskRatio(silverMask, leftSilverBottom);
                result.RightSilverTopCoverage = MaskRatio(silverMask, rightSilverTop);
                result.RightSilverBottomCoverage = MaskRatio(silverMask, rightSilverBottom);
                result.LeftMaxMissingRatio = 0;
                result.RightMaxMissingRatio = 0;
                result.LeftInnerDefectRatio = 0;
                result.RightInnerDefectRatio = 0;
                result.LeftEdgeMissingRatio = 0;
                result.RightEdgeMissingRatio = 0;
                result.LeftPairedEdgeRatio = 0;
                result.RightPairedEdgeRatio = 0;
                result.LeftLineDefectRatio = 0;
                result.RightLineDefectRatio = 0;

                Rect moduleBox = regions.ModuleBox;
                result.ModuleFullyVisible = IsModuleFullyVisible(
                    gray,
                    regionParameters.ModuleDarkThreshold,
                    inspectionParameters.ModuleVisibleMargin,
                    inspectionParameters.ModuleBorderDarkRatio);
                if (!result.ModuleFullyVisible)
                {
                    result.Reasons.Add("module is not fully visible");
                    AddErrorRegion(result, moduleBox, "module incomplete");
                }

                if (result.MiddleSilverRatio > inspectionParameters.MiddleSilverNgRatio)
                {
                    result.Reasons.Add($"middle silver {result.MiddleSilverRatio:P1}");
                    AddErrorRegion(result, middleErrorBox, $"middle {result.MiddleSilverRatio:P1}");
                }
                else if (result.MiddleSilverRatio >= inspectionParameters.MiddleSilverReviewRatio)
                {
                    AddReviewReason(result,
                        $"middle silver review {result.MiddleSilverRatio:P1}",
                        middleErrorBox,
                        $"middle review {result.MiddleSilverRatio:P1}");
                }

                foreach (AdaptiveSilverRegion abnormal in adaptive.Regions)
                {
                    if (abnormal.IsNg)
                    {
                        result.Reasons.Add($"{abnormal.Kind} abnormal {abnormal.Ratio:P1}");
                        AddErrorRegion(result, abnormal.Box,
                            $"{abnormal.Kind} {abnormal.Ratio:P1}");
                    }
                    else
                    {
                        AddReviewReason(result,
                            $"{abnormal.Kind} review {abnormal.Ratio:P1}",
                            abnormal.Box,
                            $"{abnormal.Kind} review {abnormal.Ratio:P1}");
                    }
                }

                if (!adaptive.HasModel)
                {
                    result.ReviewReasons.Add("normal model unavailable");
                }
                else if (!adaptive.IsObviousOk && result.Reasons.Count == 0 && result.ReviewReasons.Count == 0)
                {
                    result.ReviewReasons.Add(
                        $"outside normal range {adaptive.NormalDistance:F4} > {adaptive.OkDistanceThreshold:F4}");
                }

                result.Decision = result.Reasons.Count > 0
                    ? ModuleInspectionDecision.Ng
                    : result.ReviewReasons.Count > 0
                        ? ModuleInspectionDecision.Review
                        : ModuleInspectionDecision.Ok;
                result.IsOk = result.Decision == ModuleInspectionDecision.Ok;
                result.AnnotatedImage = DrawInspection(source, result);
                result.ErrorImage = DrawErrorInspection(source, result);
                return result;
            }
        }

        private static Mat CreateMiddleSilverMask(
            Mat gray,
            Rect inspectRect,
            InspectionParameters parameters)
        {
            using (Mat blur = new Mat())
            using (Mat bright = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
            {
                byte median = RegionPercentile(gray, inspectRect, 0.50, 100);
                double threshold = Clamp(
                    median + parameters.MiddleSilverThresholdOffset,
                    parameters.MiddleSilverThresholdMinimum,
                    parameters.MiddleSilverThresholdMaximum);
                Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(3, 3), 0);
                Cv2.Threshold(blur, bright, threshold, 255, ThresholdTypes.Binary);
                Cv2.MorphologyEx(bright, bright, MorphTypes.Close, kernel, iterations: 1);
                Cv2.MorphologyEx(bright, bright, MorphTypes.Open, kernel, iterations: 1);
                return bright.Clone();
            }
        }

        private static Mat CreateSilverMask(Mat bgr, int grayThreshold)
        {
            using (Mat gray = new Mat())
            using (Mat bright = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5)))
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(3, 3), 0);

                // 原来这里固定写死 145，现在改为传入 grayThreshold
                Cv2.Threshold(gray, bright, grayThreshold, 255, ThresholdTypes.Binary);

                Cv2.MorphologyEx(bright, bright, MorphTypes.Close, kernel, iterations: 1);
                Cv2.MorphologyEx(bright, bright, MorphTypes.Open, kernel, iterations: 1);

                return bright.Clone();
            }
        }


        private static Mat CreateEdgeSilverMask(Mat bgr, int grayThreshold)
        {
            using (Mat gray = new Mat())
            using (Mat strictSilver = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)))
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(3, 3), 0);
                Cv2.Threshold(gray, strictSilver, grayThreshold, 255, ThresholdTypes.Binary);
                Cv2.MorphologyEx(strictSilver, strictSilver, MorphTypes.Open, kernel, iterations: 1);
                return strictSilver.Clone();
            }
        }

        private static Mat DrawInspection(Mat source, ModuleInspectionResult result)
        {
            Mat output = ModuleRegionLocator.DrawRegions(source, result.Regions);
            DrawCoverageOverlay(output, result);
            return output;
        }

        private static Mat DrawErrorInspection(Mat source, ModuleInspectionResult result)
        {
            Mat output = ModuleRegionLocator.DrawRegions(source, result.Regions);
            DrawCoverageOverlay(output, result);

            if (result.ErrorRegions.Count == 0)
                return output;

            foreach (ModuleErrorRegion error in result.ErrorRegions)
            {
                Rect box = ClampRectToImage(error.Box, output.Width, output.Height);
                Scalar color = error.IsReview ? new Scalar(0, 165, 255) : Scalar.Red;
                Cv2.Rectangle(output, box, color, 4);
                Cv2.PutText(output, error.Label ?? "error",
                    new OpenCvSharp.Point(box.X + 3, Math.Max(18, box.Y - 6)),
                    HersheyFonts.HersheySimplex, 0.62, color, 2);
            }

            return output;
        }

        private static void DrawCoverageOverlay(Mat output, ModuleInspectionResult result)
        {
            Rect leftSilver = GetRegion(result.Regions, ModuleRegionType.LeftSilver);
            Rect rightSilver = GetRegion(result.Regions, ModuleRegionType.RightSilver);
            DrawCoverageText(output, leftSilver,
                $"L cover {result.LeftSilverCoverage:P0}",
                $"L top {result.LeftSilverTopCoverage:P0}",
                $"L bottom {result.LeftSilverBottomCoverage:P0}");
            DrawCoverageText(output, rightSilver,
                $"R cover {result.RightSilverCoverage:P0}",
                $"R top {result.RightSilverTopCoverage:P0}",
                $"R bottom {result.RightSilverBottomCoverage:P0}");
        }

        private static void DrawCoverageText(Mat output, Rect rect, string coverText, string topText, string bottomText)
        {
            int x = rect.X + 4;
            int y = rect.Y + 18;
            Cv2.PutText(output, coverText, new OpenCvSharp.Point(x, y),
                HersheyFonts.HersheySimplex, 0.38, Scalar.Yellow, 1);
            Cv2.PutText(output, topText, new OpenCvSharp.Point(x, y + 16),
                HersheyFonts.HersheySimplex, 0.38, Scalar.Yellow, 1);
            Cv2.PutText(output, bottomText, new OpenCvSharp.Point(x, y + 32),
                HersheyFonts.HersheySimplex, 0.38, Scalar.Yellow, 1);
        }

        private static void AddErrorRegion(ModuleInspectionResult result, Rect box, string label)
        {
            if (box.Width <= 0 || box.Height <= 0)
                return;
            AddOrMergeErrorRegion(result, box, label, false);
        }

        private static void AddReviewReason(ModuleInspectionResult result, string reason, Rect box, string label)
        {
            result.ReviewReasons.Add(reason);
            if (box.Width <= 0 || box.Height <= 0)
                return;
            AddOrMergeErrorRegion(result, box, label, true);
        }

        private static void AddOrMergeErrorRegion(
            ModuleInspectionResult result,
            Rect box,
            string label,
            bool isReview)
        {
            int boxArea = Math.Max(1, box.Width * box.Height);
            foreach (ModuleErrorRegion existing in result.ErrorRegions)
            {
                Rect overlap = Intersect(existing.Box, box);
                if (overlap.Width <= 0 || overlap.Height <= 0)
                    continue;

                int existingArea = Math.Max(1, existing.Box.Width * existing.Box.Height);
                int overlapArea = overlap.Width * overlap.Height;
                double areaScale = Math.Max(existingArea, boxArea) /
                    (double)Math.Min(existingArea, boxArea);
                double overlapOfSmaller = overlapArea /
                    (double)Math.Min(existingArea, boxArea);
                if (areaScale > 4.0 || overlapOfSmaller < 0.55)
                    continue;

                existing.Box = Union(existing.Box, box);
                existing.Label = label;
                existing.IsReview = existing.IsReview && isReview;
                return;
            }

            result.ErrorRegions.Add(new ModuleErrorRegion
            {
                Box = box,
                Label = label,
                IsReview = isReview
            });
        }

        private static void CheckInnerDefectDecisions(
            ModuleInspectionResult result,
            System.Collections.Generic.IEnumerable<DefectCandidate> defects,
            string side,
            InspectionParameters parameters)
        {
            foreach (DefectCandidate defect in defects)
            {
                if (defect.Ratio > parameters.InnerDefectNgRatio)
                {
                    result.Reasons.Add($"{side} silver defect {defect.Ratio:P2}");
                    AddErrorRegion(result, defect.Box, $"{side} defect {defect.Ratio:P2}");
                }
                else if (defect.Ratio >= parameters.InnerDefectReviewRatio)
                {
                    AddReviewReason(
                        result,
                        $"{side} silver defect review {defect.Ratio:P2}",
                        defect.Box,
                        $"{side} defect review {defect.Ratio:P2}");
                }
            }
        }

        private static void CheckDefectDecisions(
            ModuleInspectionResult result,
            System.Collections.Generic.IEnumerable<DefectCandidate> defects,
            string name,
            double reviewRatio,
            double ngRatio)
        {
            foreach (DefectCandidate defect in defects)
            {
                if (defect.Ratio > ngRatio)
                {
                    result.Reasons.Add($"{name} {defect.Ratio:P2}");
                    AddErrorRegion(result, defect.Box, $"{name} {defect.Ratio:P2}");
                }
                else if (defect.Ratio >= reviewRatio)
                {
                    AddReviewReason(
                        result,
                        $"{name} review {defect.Ratio:P2}",
                        defect.Box,
                        $"{name} review {defect.Ratio:P2}");
                }
            }
        }

        private static double MaximumRatio(
            System.Collections.Generic.IList<DefectCandidate> defects)
        {
            return defects == null || defects.Count == 0 ? 0 : defects[0].Ratio;
        }

        private static DefectCandidate SelectLargestCandidate(
            params System.Collections.Generic.IList<DefectCandidate>[] groups)
        {
            DefectCandidate best = null;
            foreach (System.Collections.Generic.IList<DefectCandidate> group in groups)
            {
                if (group == null)
                    continue;
                foreach (DefectCandidate candidate in group)
                {
                    if (best == null || candidate.Ratio > best.Ratio)
                        best = candidate;
                }
            }
            return best;
        }

        private static void CheckCoverageDecision(
            ModuleInspectionResult result,
            double coverage,
            double ngMinimum,
            double okMinimum,
            Rect box,
            string reasonName,
            string labelName)
        {
            if (coverage < ngMinimum)
            {
                result.Reasons.Add($"{reasonName} coverage low {coverage:P1}");
                AddErrorRegion(result, box, $"{labelName} {coverage:P1}");
            }
            else if (coverage < okMinimum)
            {
                AddReviewReason(result,
                    $"{reasonName} coverage review {coverage:P1}",
                    box,
                    $"{labelName} review {coverage:P1}");
            }
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

        private static Rect GetMiddleInspectRect(Rect middle, double insetRatio)
        {
            int inset = Math.Max(1, (int)Math.Round(middle.Width * insetRatio));
            int width = Math.Max(1, middle.Width - inset * 2);
            return new Rect(middle.X + inset, middle.Y, width, middle.Height);
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

        private static Rect FindLargestMaskBlobBox(Mat mask, Rect searchRect)
        {
            using (Mat roi = new Mat(mask, searchRect))
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                int count = Cv2.ConnectedComponentsWithStats(roi, labels, stats, centroids);
                int maxArea = 0;
                Rect maxBox = searchRect;
                for (int i = 1; i < count; i++)
                {
                    int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                    if (area <= maxArea)
                        continue;

                    int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                    int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                    int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                    int height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);
                    maxArea = area;
                    maxBox = new Rect(searchRect.X + x, searchRect.Y + y, width, height);
                }

                return maxBox;
            }
        }

        private static void SplitSilverRectVertically(Rect silverRect, double splitRatio, out Rect topRect, out Rect bottomRect)
        {
            splitRatio = Math.Max(0.10, Math.Min(0.90, splitRatio));

            int topHeight = Math.Max(1, (int)Math.Round(silverRect.Height * splitRatio));
            int bottomHeight = Math.Max(1, silverRect.Height - topHeight);

            if (topHeight + bottomHeight > silverRect.Height)
                bottomHeight = Math.Max(1, silverRect.Height - topHeight);

            topRect = new Rect(
                silverRect.X,
                silverRect.Y,
                silverRect.Width,
                topHeight);

            bottomRect = new Rect(
                silverRect.X,
                silverRect.Y + topHeight,
                silverRect.Width,
                bottomHeight);
        }

        private static void CheckEdgeSilver(
            Mat mask,
            Rect[] rects,
            ModuleRegionType[] types,
            string[] names,
            ModuleInspectionResult result,
            RegionParameters regionParameters,
            InspectionParameters inspectionParameters)
        {
            result.EdgeSilverRatio = 0;
            result.EdgeSilverRegion = string.Empty;
            for (int i = 0; i < rects.Length; i++)
            {
                Rect inspectRect = GetEdgeInspectRect(
                    rects[i], types[i], regionParameters.EdgeInspectTrimRatio);
                double ratio = MaskRatio(mask, inspectRect);
                if (ratio > result.EdgeSilverRatio)
                {
                    result.EdgeSilverRatio = ratio;
                    result.EdgeSilverRegion = names[i];
                }
                if (ratio > inspectionParameters.EdgeSilverNgRatio)
                {
                    result.Reasons.Add($"{names[i]} silver {ratio:P1}");
                    AddErrorRegion(result, inspectRect, $"{names[i]} {ratio:P1}");
                }
                else if (ratio >= inspectionParameters.EdgeSilverReviewRatio)
                {
                    AddReviewReason(result,
                        $"{names[i]} silver review {ratio:P1}",
                        inspectRect,
                        $"{names[i]} review {ratio:P1}");
                }
            }
        }

        private static Rect GetEdgeInspectRect(Rect rect, ModuleRegionType type, double trimRatio)
        {
            int trim = Math.Max(2, (int)Math.Round(Math.Min(rect.Width, rect.Height) * trimRatio));
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
            public System.Collections.Generic.List<DefectCandidate> InnerDefects { get; set; }
        }

        private static SilverDefectStats AnalyzeSilverDefects(
            Mat gray,
            Rect rect,
            RegionParameters regionParameters,
            InspectionParameters inspectionParameters)
        {
            Rect inner = ShrinkRect(
                rect,
                regionParameters.SilverInnerHorizontalInsetRatio,
                regionParameters.SilverInnerVerticalInsetRatio);
            DefectCandidate missing = FindMissingBlob(gray, rect, inner, inspectionParameters);
            System.Collections.Generic.List<DefectCandidate> innerDefects =
                FindLocalDarkDefects(gray, inner, inspectionParameters);
            DefectCandidate innerDefect = innerDefects.Count > 0
                ? innerDefects[0]
                : new DefectCandidate();
            return new SilverDefectStats
            {
                MissingRatio = missing.Ratio,
                InnerDefectRatio = innerDefect.Ratio,
                MissingBox = missing.Box,
                InnerDefectBox = innerDefect.Box,
                InnerDefects = innerDefects
            };
        }

        private sealed class DefectCandidate
        {
            public double Ratio { get; set; }
            public Rect Box { get; set; }
        }

        private static DefectCandidate FindMissingBlob(
            Mat gray,
            Rect silverRect,
            Rect inspectRect,
            InspectionParameters parameters)
        {
            using (Mat roi = new Mat(gray, inspectRect))
            using (Mat dark = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5)))
            {
                Cv2.Threshold(roi, dark, parameters.MissingDarkThreshold, 255, ThresholdTypes.BinaryInv);
                Cv2.MorphologyEx(dark, dark, MorphTypes.Close, kernel, iterations: 1);
                using (Mat labels = new Mat())
                using (Mat stats = new Mat())
                using (Mat centroids = new Mat())
                {
                    int count = Cv2.ConnectedComponentsWithStats(dark, labels, stats, centroids);
                    int maxArea = 0;
                    Rect maxBox = Rect.Empty;
                    for (int i = 1; i < count; i++)
                    {
                        int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                        int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                        int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                        int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                        int height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);

                        int margin = parameters.MissingBoundaryMargin;
                        bool touchesOutside = x <= margin ||
                            y <= margin ||
                            x + width >= inspectRect.Width - margin ||
                            y + height >= inspectRect.Height - margin;
                        if (touchesOutside)
                            continue;

                        if (area > maxArea)
                        {
                            maxArea = area;
                            maxBox = new Rect(inspectRect.X + x, inspectRect.Y + y, width, height);
                        }
                    }
                    double ratio = silverRect.Width <= 0 || silverRect.Height <= 0
                        ? 0
                        : maxArea / (double)(silverRect.Width * silverRect.Height);
                    return new DefectCandidate { Ratio = ratio, Box = maxBox };
                }
            }
        }

        private static System.Collections.Generic.List<DefectCandidate> FindLocalDarkDefects(
            Mat gray,
            Rect inner,
            InspectionParameters parameters)
        {
            var defects = new System.Collections.Generic.List<DefectCandidate>();
            int backgroundKernelSize = FitOddKernel(
                parameters.DefectBackgroundKernelSize,
                Math.Min(inner.Width, inner.Height));
            int morphKernelSize = FitOddKernel(
                parameters.DefectMorphKernelSize,
                Math.Min(inner.Width, inner.Height));

            using (Mat roi = new Mat(gray, inner))
            using (Mat blur = new Mat())
            using (Mat blackHat = new Mat())
            using (Mat strongDark = new Mat())
            using (Mat localContrast = new Mat())
            using (Mat candidate = new Mat())
            using (Mat backgroundKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(backgroundKernelSize, backgroundKernelSize)))
            using (Mat morphKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(morphKernelSize, morphKernelSize)))
            {
                Cv2.GaussianBlur(roi, blur, new OpenCvSharp.Size(3, 3), 0);

                // Black Hat = 闭运算结果 - 原图，直接突出亮银面中的局部暗孔。
                Cv2.MorphologyEx(blur, blackHat, MorphTypes.BlackHat, backgroundKernel);
                Cv2.Threshold(
                    blur,
                    strongDark,
                    parameters.StrongDarkGrayThreshold,
                    255,
                    ThresholdTypes.BinaryInv);
                Cv2.Threshold(
                    blackHat,
                    localContrast,
                    parameters.LocalContrastThreshold,
                    255,
                    ThresholdTypes.Binary);
                Cv2.BitwiseAnd(strongDark, localContrast, candidate);

                Cv2.MorphologyEx(candidate, candidate, MorphTypes.Close, morphKernel, iterations: 1);
                Cv2.MorphologyEx(candidate, candidate, MorphTypes.Open, morphKernel, iterations: 1);

                using (Mat labels = new Mat())
                using (Mat stats = new Mat())
                using (Mat centroids = new Mat())
                {
                    int count = Cv2.ConnectedComponentsWithStats(candidate, labels, stats, centroids);
                    int innerArea = Math.Max(1, inner.Width * inner.Height);
                    int minimumArea = Math.Max(
                        parameters.MinimumDefectArea,
                        (int)Math.Round(innerArea * parameters.MinimumDefectAreaRatio));
                    int boundaryMargin = Math.Max(2, morphKernelSize);

                    for (int i = 1; i < count; i++)
                    {
                        int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                        int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                        int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                        int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                        int height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);

                        if (area < minimumArea ||
                            width < parameters.MinimumDefectWidth ||
                            height < parameters.MinimumDefectHeight)
                            continue;

                        if (x <= boundaryMargin || y <= boundaryMargin ||
                            x + width >= inner.Width - boundaryMargin ||
                            y + height >= inner.Height - boundaryMargin)
                            continue;

                        int boxArea = Math.Max(1, width * height);
                        double fillRatio = area / (double)boxArea;
                        double aspectRatio = width / (double)height;
                        if (fillRatio < parameters.MinimumDefectFillRatio ||
                            aspectRatio < parameters.MinimumDefectAspectRatio ||
                            aspectRatio > parameters.MaximumDefectAspectRatio)
                            continue;

                        Rect coreBox = new Rect(inner.X + x, inner.Y + y, width, height);
                        Rect displayBox = ExpandDefectRect(
                            coreBox,
                            parameters.DefectBoxPadding,
                            parameters.DefectBoxMinimumWidth,
                            parameters.DefectBoxMinimumHeight,
                            gray.Width,
                            gray.Height);
                        defects.Add(new DefectCandidate
                        {
                            Ratio = area / (double)innerArea,
                            Box = displayBox
                        });
                    }
                }
            }

            defects.Sort((left, right) => right.Ratio.CompareTo(left.Ratio));
            if (defects.Count > parameters.MaximumDefectsPerSilverRegion)
            {
                defects.RemoveRange(
                    parameters.MaximumDefectsPerSilverRegion,
                    defects.Count - parameters.MaximumDefectsPerSilverRegion);
            }
            return defects;
        }

        private static System.Collections.Generic.List<DefectCandidate> FindEdgeMissingDefects(
            Mat gray,
            Rect silverRect,
            InspectionParameters parameters)
        {
            var defects = new System.Collections.Generic.List<DefectCandidate>();
            int closeSize = FitOddKernel(
                parameters.EdgeMaskCloseKernelSize,
                Math.Min(silverRect.Width, silverRect.Height));
            int bandKernelSize = FitOddKernel(
                parameters.EdgeBandDepth * 2 + 1,
                Math.Min(silverRect.Width, silverRect.Height));
            int contactKernelSize = FitOddKernel(
                parameters.EdgeBoundaryContactDepth * 2 + 1,
                Math.Min(silverRect.Width, silverRect.Height));

            using (Mat roi = new Mat(gray, silverRect))
            using (Mat blur = new Mat())
            using (Mat actual = new Mat())
            using (Mat contourInput = new Mat())
            using (Mat expected = Mat.Zeros(roi.Size(), MatType.CV_8UC1))
            using (Mat erodedExpected = new Mat())
            using (Mat edgeBand = new Mat())
            using (Mat contactCore = new Mat())
            using (Mat contactBand = new Mat())
            using (Mat notActual = new Mat())
            using (Mat missing = new Mat())
            using (Mat closeKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(closeSize, closeSize)))
            using (Mat cleanKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(3, 3)))
            using (Mat bandKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(bandKernelSize, bandKernelSize)))
            using (Mat contactKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(contactKernelSize, contactKernelSize)))
            {
                Cv2.GaussianBlur(roi, blur, new OpenCvSharp.Size(3, 3), 0);
                Cv2.Threshold(
                    blur,
                    actual,
                    parameters.EdgeMaskGrayThreshold,
                    255,
                    ThresholdTypes.Binary);
                Cv2.MorphologyEx(actual, actual, MorphTypes.Close, closeKernel, iterations: 1);
                Cv2.MorphologyEx(actual, actual, MorphTypes.Open, cleanKernel, iterations: 1);

                actual.CopyTo(contourInput);
                Cv2.FindContours(
                    contourInput,
                    out OpenCvSharp.Point[][] contours,
                    out HierarchyIndex[] hierarchy,
                    RetrievalModes.External,
                    ContourApproximationModes.ApproxSimple);
                if (contours == null || contours.Length == 0)
                    return defects;

                int largestIndex = 0;
                double largestArea = 0;
                for (int i = 0; i < contours.Length; i++)
                {
                    double area = Cv2.ContourArea(contours[i]);
                    if (area > largestArea)
                    {
                        largestArea = area;
                        largestIndex = i;
                    }
                }
                if (largestArea <= 0)
                    return defects;

                OpenCvSharp.Point[] hull = Cv2.ConvexHull(contours[largestIndex]);
                Cv2.FillConvexPoly(expected, hull, Scalar.White);

                Cv2.Erode(expected, erodedExpected, bandKernel, iterations: 1);
                Cv2.Subtract(expected, erodedExpected, edgeBand);
                Cv2.Erode(expected, contactCore, contactKernel, iterations: 1);
                Cv2.Subtract(expected, contactCore, contactBand);
                Cv2.BitwiseNot(actual, notActual);
                Cv2.BitwiseAnd(notActual, edgeBand, missing);
                Cv2.MorphologyEx(missing, missing, MorphTypes.Close, cleanKernel, iterations: 1);
                Cv2.MorphologyEx(missing, missing, MorphTypes.Open, cleanKernel, iterations: 1);

                using (Mat labels = new Mat())
                using (Mat stats = new Mat())
                using (Mat centroids = new Mat())
                {
                    int count = Cv2.ConnectedComponentsWithStats(missing, labels, stats, centroids);
                    int silverArea = Math.Max(1, silverRect.Width * silverRect.Height);
                    for (int i = 1; i < count; i++)
                    {
                        int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                        int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                        int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                        int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                        int height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);
                        if (area < parameters.EdgeMinimumDefectArea ||
                            width < parameters.EdgeMinimumDefectWidth ||
                            height < parameters.EdgeMinimumDefectHeight)
                            continue;

                        double fillRatio = area / (double)Math.Max(1, width * height);
                        if (fillRatio < parameters.EdgeMinimumDefectFillRatio)
                            continue;

                        bool touchesBoundary = false;
                        for (int row = y; row < y + height && !touchesBoundary; row++)
                        for (int column = x; column < x + width; column++)
                        {
                            if (labels.At<int>(row, column) == i &&
                                contactBand.At<byte>(row, column) != 0)
                            {
                                touchesBoundary = true;
                                break;
                            }
                        }
                        if (!touchesBoundary)
                            continue;

                        Rect coreBox = new Rect(
                            silverRect.X + x,
                            silverRect.Y + y,
                            width,
                            height);
                        defects.Add(new DefectCandidate
                        {
                            Ratio = area / (double)silverArea,
                            Box = ExpandRect(
                                coreBox,
                                parameters.EdgeBoxPadding,
                                gray.Width,
                                gray.Height)
                        });
                    }
                }
            }

            defects.Sort((left, right) => right.Ratio.CompareTo(left.Ratio));
            if (defects.Count > parameters.MaximumEdgeDefectsPerSilverRegion)
            {
                defects.RemoveRange(
                    parameters.MaximumEdgeDefectsPerSilverRegion,
                    defects.Count - parameters.MaximumEdgeDefectsPerSilverRegion);
            }
            return defects;
        }

        private static void FindPairedEdgeDefects(
            Mat gray,
            Rect leftRect,
            Rect rightRect,
            InspectionParameters parameters,
            out System.Collections.Generic.List<DefectCandidate> leftDefects,
            out System.Collections.Generic.List<DefectCandidate> rightDefects)
        {
            leftDefects = new System.Collections.Generic.List<DefectCandidate>();
            rightDefects = new System.Collections.Generic.List<DefectCandidate>();

            using (Mat leftShape = CreateSilverShapeMask(gray, leftRect, parameters))
            using (Mat rightShapeRaw = CreateSilverShapeMask(gray, rightRect, parameters))
            using (Mat rightShape = new Mat())
            using (Mat expected = new Mat())
            using (Mat notLeft = new Mat())
            using (Mat notRight = new Mat())
            using (Mat leftMissing = new Mat())
            using (Mat rightMissing = new Mat())
            using (Mat cleanKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(3, 3)))
            {
                if (rightShapeRaw.Size() == leftShape.Size())
                    Cv2.Flip(rightShapeRaw, rightShape, FlipMode.Y);
                else
                {
                    using (Mat resized = new Mat())
                    {
                        Cv2.Resize(rightShapeRaw, resized, leftShape.Size(), 0, 0, InterpolationFlags.Nearest);
                        Cv2.Flip(resized, rightShape, FlipMode.Y);
                    }
                }

                Cv2.BitwiseOr(leftShape, rightShape, expected);
                Cv2.BitwiseNot(leftShape, notLeft);
                Cv2.BitwiseNot(rightShape, notRight);
                Cv2.BitwiseAnd(expected, notLeft, leftMissing);
                Cv2.BitwiseAnd(expected, notRight, rightMissing);
                Cv2.MorphologyEx(leftMissing, leftMissing, MorphTypes.Open, cleanKernel, iterations: 1);
                Cv2.MorphologyEx(rightMissing, rightMissing, MorphTypes.Open, cleanKernel, iterations: 1);

                leftDefects = ExtractMaskDefects(
                    leftMissing,
                    leftRect,
                    false,
                    gray.Width,
                    gray.Height,
                    parameters);
                rightDefects = ExtractMaskDefects(
                    rightMissing,
                    rightRect,
                    true,
                    gray.Width,
                    gray.Height,
                    parameters);
            }
        }

        private static Mat CreateSilverShapeMask(
            Mat gray,
            Rect rect,
            InspectionParameters parameters)
        {
            Mat shape = new Mat();
            int closeSize = FitOddKernel(
                parameters.PairedEdgeCloseKernelSize,
                Math.Min(rect.Width, rect.Height));
            using (Mat roi = new Mat(gray, rect))
            using (Mat blur = new Mat())
            using (Mat closeKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(closeSize, closeSize)))
            using (Mat openKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(3, 3)))
            {
                Cv2.GaussianBlur(roi, blur, new OpenCvSharp.Size(3, 3), 0);
                Cv2.Threshold(
                    blur,
                    shape,
                    parameters.EdgeMaskGrayThreshold,
                    255,
                    ThresholdTypes.Binary);
                Cv2.MorphologyEx(shape, shape, MorphTypes.Close, closeKernel, iterations: 1);
                Cv2.MorphologyEx(shape, shape, MorphTypes.Open, openKernel, iterations: 1);
            }
            return shape;
        }

        private static System.Collections.Generic.List<DefectCandidate> ExtractMaskDefects(
            Mat mask,
            Rect targetRect,
            bool horizontallyMirrored,
            int imageWidth,
            int imageHeight,
            InspectionParameters parameters)
        {
            var defects = new System.Collections.Generic.List<DefectCandidate>();
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                int count = Cv2.ConnectedComponentsWithStats(mask, labels, stats, centroids);
                int silverArea = Math.Max(1, targetRect.Width * targetRect.Height);
                for (int i = 1; i < count; i++)
                {
                    int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                    int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                    int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                    int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                    int height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);
                    if (area < parameters.EdgeMinimumDefectArea ||
                        width < parameters.EdgeMinimumDefectWidth ||
                        height < parameters.EdgeMinimumDefectHeight)
                        continue;

                    double fillRatio = area / (double)Math.Max(1, width * height);
                    if (fillRatio < parameters.EdgeMinimumDefectFillRatio)
                        continue;

                    int mappedX = horizontallyMirrored
                        ? targetRect.Width - x - width
                        : x;
                    Rect coreBox = new Rect(
                        targetRect.X + mappedX,
                        targetRect.Y + y,
                        width,
                        height);
                    defects.Add(new DefectCandidate
                    {
                        Ratio = area / (double)silverArea,
                        Box = ExpandRect(
                            coreBox,
                            parameters.EdgeBoxPadding,
                            imageWidth,
                            imageHeight)
                    });
                }
            }

            defects.Sort((left, right) => right.Ratio.CompareTo(left.Ratio));
            if (defects.Count > parameters.MaximumEdgeDefectsPerSilverRegion)
            {
                defects.RemoveRange(
                    parameters.MaximumEdgeDefectsPerSilverRegion,
                    defects.Count - parameters.MaximumEdgeDefectsPerSilverRegion);
            }
            return defects;
        }

        private static System.Collections.Generic.List<DefectCandidate> FindLineDefects(
            Mat gray,
            Rect silverRect,
            RegionParameters regionParameters,
            InspectionParameters parameters)
        {
            Rect inner = ShrinkRect(
                silverRect,
                regionParameters.SilverInnerHorizontalInsetRatio,
                regionParameters.SilverInnerVerticalInsetRatio);
            var defects = new System.Collections.Generic.List<DefectCandidate>();
            int lineLength = FitOddKernel(
                parameters.LineKernelLength,
                Math.Min(inner.Width, inner.Height));

            using (Mat roi = new Mat(gray, inner))
            using (Mat blur = new Mat())
            using (Mat horizontalResponse = new Mat())
            using (Mat verticalResponse = new Mat())
            using (Mat horizontalMask = new Mat())
            using (Mat verticalMask = new Mat())
            using (Mat horizontalKernel = Cv2.GetStructuringElement(
                MorphShapes.Rect,
                new OpenCvSharp.Size(lineLength, 3)))
            using (Mat verticalKernel = Cv2.GetStructuringElement(
                MorphShapes.Rect,
                new OpenCvSharp.Size(3, lineLength)))
            using (Mat horizontalCleanKernel = Cv2.GetStructuringElement(
                MorphShapes.Rect,
                new OpenCvSharp.Size(5, 3)))
            using (Mat verticalCleanKernel = Cv2.GetStructuringElement(
                MorphShapes.Rect,
                new OpenCvSharp.Size(3, 5)))
            {
                Cv2.GaussianBlur(roi, blur, new OpenCvSharp.Size(3, 3), 0);
                Cv2.MorphologyEx(blur, horizontalResponse, MorphTypes.BlackHat, horizontalKernel);
                Cv2.MorphologyEx(blur, verticalResponse, MorphTypes.BlackHat, verticalKernel);
                Cv2.Threshold(
                    horizontalResponse,
                    horizontalMask,
                    parameters.LineContrastThreshold,
                    255,
                    ThresholdTypes.Binary);
                Cv2.Threshold(
                    verticalResponse,
                    verticalMask,
                    parameters.LineContrastThreshold,
                    255,
                    ThresholdTypes.Binary);
                Cv2.MorphologyEx(
                    horizontalMask,
                    horizontalMask,
                    MorphTypes.Close,
                    horizontalCleanKernel,
                    iterations: 1);
                Cv2.MorphologyEx(
                    verticalMask,
                    verticalMask,
                    MorphTypes.Close,
                    verticalCleanKernel,
                    iterations: 1);

                ExtractLineComponents(
                    horizontalMask,
                    inner,
                    true,
                    gray.Width,
                    gray.Height,
                    parameters,
                    defects);
                ExtractLineComponents(
                    verticalMask,
                    inner,
                    false,
                    gray.Width,
                    gray.Height,
                    parameters,
                    defects);
            }

            defects.Sort((left, right) => right.Ratio.CompareTo(left.Ratio));
            if (defects.Count > parameters.MaximumDefectsPerSilverRegion)
            {
                defects.RemoveRange(
                    parameters.MaximumDefectsPerSilverRegion,
                    defects.Count - parameters.MaximumDefectsPerSilverRegion);
            }
            return defects;
        }

        private static void ExtractLineComponents(
            Mat mask,
            Rect inner,
            bool horizontal,
            int imageWidth,
            int imageHeight,
            InspectionParameters parameters,
            System.Collections.Generic.List<DefectCandidate> output)
        {
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                int count = Cv2.ConnectedComponentsWithStats(mask, labels, stats, centroids);
                int innerArea = Math.Max(1, inner.Width * inner.Height);
                for (int i = 1; i < count; i++)
                {
                    int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                    int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                    int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                    int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                    int height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);
                    int length = horizontal ? width : height;
                    int thickness = horizontal ? height : width;
                    if (area < parameters.LineMinimumArea ||
                        length < parameters.LineMinimumLength ||
                        thickness > parameters.LineMaximumWidth)
                        continue;

                    Rect core = new Rect(inner.X + x, inner.Y + y, width, height);
                    output.Add(new DefectCandidate
                    {
                        Ratio = area / (double)innerArea,
                        Box = ExpandRect(
                            core,
                            parameters.LineBoxPadding,
                            imageWidth,
                            imageHeight)
                    });
                }
            }
        }

        private static int CountSilverSupportSides(
            Mat silverMask,
            Rect defectBox,
            double minimumSideSilverRatio)
        {
            int band = Math.Max(3, Math.Min(8, Math.Min(defectBox.Width, defectBox.Height) / 3));
            int extension = Math.Max(2, band / 2);

            Rect[] sideBands =
            {
                new Rect(defectBox.X - extension, defectBox.Y - band, defectBox.Width + extension * 2, band),
                new Rect(defectBox.X - extension, defectBox.Y + defectBox.Height, defectBox.Width + extension * 2, band),
                new Rect(defectBox.X - band, defectBox.Y - extension, band, defectBox.Height + extension * 2),
                new Rect(defectBox.X + defectBox.Width, defectBox.Y - extension, band, defectBox.Height + extension * 2)
            };

            int supportSides = 0;
            foreach (Rect sideBand in sideBands)
            {
                if (MaskRatioInsideImage(silverMask, sideBand) >= minimumSideSilverRatio)
                    supportSides++;
            }

            return supportSides;
        }

        private static double MaskRatioInsideImage(Mat mask, Rect rect)
        {
            int x1 = Math.Max(0, rect.X);
            int y1 = Math.Max(0, rect.Y);
            int x2 = Math.Min(mask.Width, rect.X + rect.Width);
            int y2 = Math.Min(mask.Height, rect.Y + rect.Height);
            if (x2 <= x1 || y2 <= y1)
                return 0;

            Rect clipped = new Rect(x1, y1, x2 - x1, y2 - y1);
            using (Mat roi = new Mat(mask, clipped))
            {
                return Cv2.CountNonZero(roi) / (double)(clipped.Width * clipped.Height);
            }
        }

        private static double ThresholdRatio(Mat gray, Rect rect, double threshold, ThresholdTypes thresholdType)
        {
            using (Mat roi = new Mat(gray, rect))
            using (Mat mask = new Mat())
            {
                Cv2.Threshold(roi, mask, threshold, 255, thresholdType);
                return rect.Width <= 0 || rect.Height <= 0
                    ? 0
                    : Cv2.CountNonZero(mask) / (double)(rect.Width * rect.Height);
            }
        }

        private static bool IsModuleFullyVisible(
            Mat gray,
            int darkThreshold,
            int borderMargin,
            double maximumDarkRatio)
        {
            int thickness = Math.Max(1, Math.Min(
                Math.Min(gray.Width, gray.Height) / 4,
                borderMargin + 1));
            Rect[] borders =
            {
                new Rect(0, 0, gray.Width, thickness),
                new Rect(0, gray.Height - thickness, gray.Width, thickness),
                new Rect(0, 0, thickness, gray.Height),
                new Rect(gray.Width - thickness, 0, thickness, gray.Height)
            };

            foreach (Rect border in borders)
            {
                double darkRatio = ThresholdRatio(
                    gray,
                    border,
                    darkThreshold,
                    ThresholdTypes.BinaryInv);
                if (darkRatio >= maximumDarkRatio)
                    return false;
            }
            return true;
        }

        private static byte RegionPercentile(Mat gray, Rect rect, double percentile, byte fallback)
        {
            using (Mat roi = new Mat(gray, rect))
            {
                return MaskedPercentile(roi, null, percentile, fallback);
            }
        }

        private static byte MaskedPercentile(Mat gray, Mat mask, double percentile, byte fallback)
        {
            int[] histogram = new int[256];
            int total = 0;

            for (int y = 0; y < gray.Rows; y++)
            for (int x = 0; x < gray.Cols; x++)
            {
                if (mask != null && mask.At<byte>(y, x) == 0)
                    continue;

                byte value = gray.At<byte>(y, x);
                histogram[value]++;
                total++;
            }

            if (total < Math.Max(20, gray.Rows * gray.Cols / 10))
            {
                Array.Clear(histogram, 0, histogram.Length);
                total = 0;
                for (int y = 0; y < gray.Rows; y++)
                for (int x = 0; x < gray.Cols; x++)
                {
                    byte value = gray.At<byte>(y, x);
                    histogram[value]++;
                    total++;
                }
            }

            if (total == 0)
                return fallback;

            int target = Math.Max(1, (int)Math.Ceiling(total * Math.Max(0, Math.Min(1, percentile))));
            int cumulative = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                cumulative += histogram[i];
                if (cumulative >= target)
                    return (byte)i;
            }

            return fallback;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static int MakeOdd(int value)
        {
            if (value < 3)
                value = 3;
            return value % 2 == 0 ? value + 1 : value;
        }

        private static int FitOddKernel(int requested, int maximum)
        {
            int limit = Math.Max(3, maximum);
            if (limit % 2 == 0)
                limit--;
            return Math.Min(MakeOdd(requested), limit);
        }

        private static Rect ExpandRect(Rect rect, int padding, int imageWidth, int imageHeight)
        {
            return ClampRectToImage(
                new Rect(
                    rect.X - padding,
                    rect.Y - padding,
                    rect.Width + padding * 2,
                    rect.Height + padding * 2),
                imageWidth,
                imageHeight);
        }

        private static Rect ExpandDefectRect(
            Rect core,
            int padding,
            int minimumWidth,
            int minimumHeight,
            int imageWidth,
            int imageHeight)
        {
            int width = Math.Max(minimumWidth, core.Width + padding * 2);
            int height = Math.Max(minimumHeight, core.Height + padding * 2);
            int centerX = core.X + core.Width / 2;
            int centerY = core.Y + core.Height / 2;
            return ClampRectToImage(
                new Rect(centerX - width / 2, centerY - height / 2, width, height),
                imageWidth,
                imageHeight);
        }

        private static Rect Intersect(Rect first, Rect second)
        {
            int left = Math.Max(first.X, second.X);
            int top = Math.Max(first.Y, second.Y);
            int right = Math.Min(first.X + first.Width, second.X + second.Width);
            int bottom = Math.Min(first.Y + first.Height, second.Y + second.Height);
            return right <= left || bottom <= top
                ? Rect.Empty
                : new Rect(left, top, right - left, bottom - top);
        }

        private static Rect Union(Rect first, Rect second)
        {
            int left = Math.Min(first.X, second.X);
            int top = Math.Min(first.Y, second.Y);
            int right = Math.Max(first.X + first.Width, second.X + second.Width);
            int bottom = Math.Max(first.Y + first.Height, second.Y + second.Height);
            return new Rect(left, top, right - left, bottom - top);
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
