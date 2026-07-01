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
        // 1. 相机裸数据转Mat
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
        // 2. Mat转图片控件显示
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
        // 3. 读取本地图片
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
        // 4. 保存处理后的图像
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
        private const double MinimumAlignmentAngle = 0.3;
        private const double MaximumAlignmentAngle = 10.0;

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

        public static Mat AlignToModule(
            Mat source,
            RegionParameters parameters,
            out double correctionAngle)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("source image is empty.", nameof(source));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            parameters.Validate();
            correctionAngle = 0;
            Mat bgr = OpenCvImageHelper.EnsureBgr8(source);
            using (Mat gray = new Mat())
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                if (!TryFindModuleContour(gray, parameters, out OpenCvSharp.Point[] contour))
                    return bgr;

                RotatedRect rotated = Cv2.MinAreaRect(contour);
                Point2f[] corners = rotated.Points();
                double longestSquared = 0;
                double angle = 0;
                for (int i = 0; i < corners.Length; i++)
                {
                    Point2f first = corners[i];
                    Point2f second = corners[(i + 1) % corners.Length];
                    double dx = second.X - first.X;
                    double dy = second.Y - first.Y;
                    double squared = dx * dx + dy * dy;
                    if (squared <= longestSquared)
                        continue;
                    longestSquared = squared;
                    angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                }

                while (angle > 90) angle -= 180;
                while (angle <= -90) angle += 180;
                if (Math.Abs(angle) < MinimumAlignmentAngle ||
                    Math.Abs(angle) > MaximumAlignmentAngle)
                    return bgr;

                using (Mat matrix = Cv2.GetRotationMatrix2D(rotated.Center, angle, 1.0))
                {
                    Mat aligned = new Mat();
                    Cv2.WarpAffine(
                        bgr,
                        aligned,
                        matrix,
                        bgr.Size(),
                        InterpolationFlags.Linear,
                        BorderTypes.Replicate);
                    bgr.Dispose();
                    correctionAngle = angle;
                    return aligned;
                }
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
            if (TryLocateModuleByContour(gray, parameters, out Rect contourBox))
                return ClampRect(contourBox, width, height);

            // 外轮廓无法稳定获得时，回退到原有的行列投影定位。
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

        private static bool TryLocateModuleByContour(
            Mat gray,
            RegionParameters parameters,
            out Rect moduleBox)
        {
            moduleBox = Rect.Empty;
            if (!TryFindModuleContour(gray, parameters, out OpenCvSharp.Point[] contour))
                return false;

            Rect best = Cv2.BoundingRect(contour);
            int shortSide = Math.Min(gray.Width, gray.Height);
            int padding = Math.Max(2, (int)Math.Round(shortSide * 0.004));
            moduleBox = new Rect(
                best.X - padding,
                best.Y - padding,
                best.Width + padding * 2,
                best.Height + padding * 2);
            return true;
        }

        private static bool TryFindModuleContour(
            Mat gray,
            RegionParameters parameters,
            out OpenCvSharp.Point[] moduleContour)
        {
            moduleContour = null;
            int strictThreshold = Math.Max(40, Math.Min(135, parameters.ModuleDarkThreshold));
            int shortSide = Math.Min(gray.Width, gray.Height);
            int closeSize = Math.Max(3, (int)Math.Round(shortSide * 0.012));
            if ((closeSize & 1) == 0) closeSize++;

            using (Mat blur = new Mat())
            using (Mat dark = new Mat())
            using (Mat openKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3)))
            using (Mat closeKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse, new OpenCvSharp.Size(closeSize, closeSize)))
            {
                Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(5, 5), 0);
                Cv2.Threshold(blur, dark, strictThreshold, 255, ThresholdTypes.BinaryInv);
                Cv2.MorphologyEx(dark, dark, MorphTypes.Open, openKernel);
                Cv2.MorphologyEx(dark, dark, MorphTypes.Close, closeKernel);
                Cv2.FindContours(dark, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] _,
                    RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                double minimumArea = gray.Width * gray.Height * 0.12;
                double bestArea = 0;
                foreach (OpenCvSharp.Point[] contour in contours)
                {
                    double area = Cv2.ContourArea(contour);
                    if (area < minimumArea || area <= bestArea)
                        continue;
                    Rect box = Cv2.BoundingRect(contour);
                    if (box.Width < gray.Width * 0.45 || box.Height < gray.Height * 0.35)
                        continue;
                    bestArea = area;
                    moduleContour = contour;
                }

                return bestArea > 0 && moduleContour != null;
            }
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
            RoundedSilverRegions = new System.Collections.Generic.List<AdaptiveSilverRoundedRegion>();
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
        public int LeftInnerDefectArea { get; set; }
        public int RightInnerDefectArea { get; set; }
        public int LeftMaxEdgeConcavityArea { get; set; }
        public int RightMaxEdgeConcavityArea { get; set; }
        public double LeftMaxEdgeConcavityDepth { get; set; }
        public double RightMaxEdgeConcavityDepth { get; set; }
        public bool ModuleFullyVisible { get; set; } = true;
        public double ModuleAlignmentAngle { get; set; }
        public ModuleRegionResult Regions { get; set; }
        public System.Collections.Generic.List<ModuleErrorRegion> ErrorRegions { get; private set; }
        public System.Collections.Generic.List<AdaptiveSilverRoundedRegion> RoundedSilverRegions { get; private set; }
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

            using (Mat aligned = ModuleRegionLocator.AlignToModule(
                source, regionParameters, out double alignmentAngle))
            {
                ModuleRegionResult regions = ModuleRegionLocator.Locate(aligned, regionParameters);
                using (Mat bgr = OpenCvImageHelper.EnsureBgr8(aligned))
                using (Mat gray = new Mat())
                {
                    Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

                Rect middle = GetRegion(regions, ModuleRegionType.Middle);
                Rect middleInspect = GetMiddleInspectRect(middle, regionParameters.MiddleInspectHorizontalInsetRatio);
                Rect leftSilver = GetRegion(regions, ModuleRegionType.LeftSilver);
                Rect rightSilver = GetRegion(regions, ModuleRegionType.RightSilver);
                var result = new ModuleInspectionResult
                {
                    Regions = regions,
                    ModuleAlignmentAngle = alignmentAngle
                };
                AdaptiveSilverEvaluation adaptive = AdaptiveSilverNormalModel.Evaluate(
                    gray,
                    leftSilver,
                    rightSilver,
                    inspectionParameters);
                Rect middleErrorBox;
                using (Mat middleSilverMask = CreateMiddleSilverMask(gray, middleInspect, inspectionParameters))
                {
                    result.MiddleSilverRatio = MaskRatio(middleSilverMask, middleInspect);
                    middleErrorBox = FindLargestMaskBlobBox(middleSilverMask, middleInspect);
                }
                if (result.MiddleSilverRatio >= inspectionParameters.MiddleSilverNgRatio)
                {
                    result.Reasons.Add(
                        $"middle silver {result.MiddleSilverRatio:P2}");
                    AddErrorRegion(
                        result, middleErrorBox,
                        $"middle {result.MiddleSilverRatio:P2}");
                }
                else if (result.MiddleSilverRatio >= inspectionParameters.MiddleSilverReviewRatio)
                {
                    AddReviewReason(
                        result,
                        $"middle silver review {result.MiddleSilverRatio:P2}",
                        middleErrorBox,
                        $"middle review {result.MiddleSilverRatio:P2}");
                }
                result.LeftSilverCoverage = adaptive.LeftCoverage;
                result.RightSilverCoverage = adaptive.RightCoverage;
                result.LeftSilverTopCoverage = adaptive.LeftTopCoverage;
                result.LeftSilverBottomCoverage = adaptive.LeftBottomCoverage;
                result.RightSilverTopCoverage = adaptive.RightTopCoverage;
                result.RightSilverBottomCoverage = adaptive.RightBottomCoverage;
                result.RoundedSilverRegions.AddRange(adaptive.RoundedRegions);
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

                // 固定左右银区ROI，并分别判定每侧的上、下半区覆盖率。
                ApplySilverCoverage(result, result.LeftSilverTopCoverage,
                    TopHalf(leftSilver), "L top",
                    inspectionParameters.SilverCoverageNgRatio,
                    inspectionParameters.SilverCoverageOkRatio);
                ApplySilverCoverage(result, result.LeftSilverBottomCoverage,
                    BottomHalf(leftSilver), "L bottom",
                    inspectionParameters.SilverCoverageNgRatio,
                    inspectionParameters.SilverCoverageOkRatio);
                ApplySilverCoverage(result, result.RightSilverTopCoverage,
                    TopHalf(rightSilver), "R top",
                    inspectionParameters.SilverCoverageNgRatio,
                    inspectionParameters.SilverCoverageOkRatio);
                ApplySilverCoverage(result, result.RightSilverBottomCoverage,
                    BottomHalf(rightSilver), "R bottom",
                    inspectionParameters.SilverCoverageNgRatio,
                    inspectionParameters.SilverCoverageOkRatio);

                System.Collections.Generic.List<AdaptiveSilverDarkDefect> leftDarkDefects =
                    AdaptiveSilverNormalModel.LocateDarkDefects(
                        gray, leftSilver, inspectionParameters);
                System.Collections.Generic.List<AdaptiveSilverDarkDefect> rightDarkDefects =
                    AdaptiveSilverNormalModel.LocateDarkDefects(
                        gray, rightSilver, inspectionParameters);
                result.LeftInnerDefectArea = MaximumDarkDefectArea(leftDarkDefects);
                result.RightInnerDefectArea = MaximumDarkDefectArea(rightDarkDefects);
                ApplyDarkDefectAreaDecisions(
                    result, leftDarkDefects, "L", inspectionParameters);
                ApplyDarkDefectAreaDecisions(
                    result, rightDarkDefects, "R", inspectionParameters);

                System.Collections.Generic.List<AdaptiveSilverEdgeConcavity> leftConcavities =
                    AdaptiveSilverNormalModel.MeasureEdgeConcavities(
                        gray, leftSilver, inspectionParameters);
                System.Collections.Generic.List<AdaptiveSilverEdgeConcavity> rightConcavities =
                    AdaptiveSilverNormalModel.MeasureEdgeConcavities(
                        gray, rightSilver, inspectionParameters);
                SetMaximumEdgeConcavity(
                    result, leftConcavities, true);
                SetMaximumEdgeConcavity(
                    result, rightConcavities, false);
                ApplyEdgeConcavityDecisions(
                    result, leftConcavities, "L", inspectionParameters);
                ApplyEdgeConcavityDecisions(
                    result, rightConcavities, "R", inspectionParameters);

                result.Decision = result.Reasons.Count > 0
                    ? ModuleInspectionDecision.Ng
                    : result.ReviewReasons.Count > 0
                        ? ModuleInspectionDecision.Review
                        : ModuleInspectionDecision.Ok;
                result.IsOk = result.Decision == ModuleInspectionDecision.Ok;
                    result.AnnotatedImage = DrawInspection(aligned, result);
                    result.ErrorImage = DrawErrorInspection(aligned, result);
                    return result;
                }
            }
        }

        private static void ApplySilverCoverage(
            ModuleInspectionResult result,
            double coverage,
            Rect errorBox,
            string name,
            double ngThreshold,
            double okThreshold)
        {
            if (coverage < ngThreshold)
            {
                result.Reasons.Add($"{name} silver {coverage:P1} < {ngThreshold:P0}");
                AddErrorRegion(result, errorBox, $"{name} {coverage:P1}");
            }
            else if (coverage <= okThreshold)
            {
                result.ReviewReasons.Add(
                    $"{name} silver {coverage:P1} is {ngThreshold:P0}-{okThreshold:P0}");
            }
        }

        private static int MaximumDarkDefectArea(
            System.Collections.Generic.IList<AdaptiveSilverDarkDefect> defects)
        {
            return defects == null || defects.Count == 0
                ? 0
                : defects[0].PixelArea;
        }

        private static void ApplyDarkDefectAreaDecisions(
            ModuleInspectionResult result,
            System.Collections.Generic.IEnumerable<AdaptiveSilverDarkDefect> defects,
            string side,
            InspectionParameters parameters)
        {
            foreach (AdaptiveSilverDarkDefect defect in defects)
            {
                if (defect.PixelArea >= parameters.MinimumDefectArea)
                {
                    result.Reasons.Add(
                        $"{side} inner defect {defect.PixelArea}px >= {parameters.MinimumDefectArea}px");
                    AddErrorRegion(
                        result, defect.Box, $"{side} defect {defect.PixelArea}px");
                }
                else if (defect.PixelArea >= parameters.InnerDefectReviewArea)
                {
                    AddReviewReason(
                        result,
                        $"{side} inner defect review {defect.PixelArea}px",
                        defect.Box,
                        $"{side} defect review {defect.PixelArea}px");
                }
            }
        }

        private static void SetMaximumEdgeConcavity(
            ModuleInspectionResult result,
            System.Collections.Generic.IList<AdaptiveSilverEdgeConcavity> concavities,
            bool isLeft)
        {
            AdaptiveSilverEdgeConcavity largest = concavities == null || concavities.Count == 0
                ? null
                : concavities[0];
            if (isLeft)
            {
                result.LeftMaxEdgeConcavityArea = largest?.PixelArea ?? 0;
                result.LeftMaxEdgeConcavityDepth = largest?.MaximumDepth ?? 0;
            }
            else
            {
                result.RightMaxEdgeConcavityArea = largest?.PixelArea ?? 0;
                result.RightMaxEdgeConcavityDepth = largest?.MaximumDepth ?? 0;
            }
        }

        private static void ApplyEdgeConcavityDecisions(
            ModuleInspectionResult result,
            System.Collections.Generic.IEnumerable<AdaptiveSilverEdgeConcavity> concavities,
            string side,
            InspectionParameters parameters)
        {
            foreach (AdaptiveSilverEdgeConcavity concavity in concavities)
            {
                int classification = AdaptiveSilverNormalModel.ClassifyEdgeConcavity(
                    concavity, parameters);
                if (classification == 2)
                {
                    result.Reasons.Add(
                        $"{side} edge concavity area={concavity.PixelArea}px, depth={concavity.MaximumDepth:F1}px, " +
                        $"opening={concavity.OpeningWidth:F1}px, score={concavity.EffectiveArea:F0}");
                    AddErrorRegion(
                        result,
                        concavity.Box,
                        $"{side} edge NG {concavity.EffectiveArea:F0}");
                }
                else if (classification == 1)
                {
                    AddReviewReason(
                        result,
                        $"{side} edge concavity review area={concavity.PixelArea}px, depth={concavity.MaximumDepth:F1}px, " +
                        $"opening={concavity.OpeningWidth:F1}px, score={concavity.EffectiveArea:F0}",
                        concavity.Box,
                        $"{side} edge review {concavity.EffectiveArea:F0}");
                }
            }
        }

        private static Rect TopHalf(Rect rect)
        {
            int height = Math.Max(1, rect.Height / 2);
            return new Rect(rect.X, rect.Y, rect.Width, height);
        }

        private static Rect BottomHalf(Rect rect)
        {
            int topHeight = Math.Max(1, rect.Height / 2);
            return new Rect(rect.X, rect.Y + topHeight,
                rect.Width, Math.Max(1, rect.Height - topHeight));
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
        private static Mat DrawInspection(Mat source, ModuleInspectionResult result)
        {
            Mat output = ModuleRegionLocator.DrawRegions(source, result.Regions);
            DrawCoverageOverlay(output, result);
            DrawRoundedSilverRegions(output, result.RoundedSilverRegions);
            return output;
        }

        private static Mat DrawErrorInspection(Mat source, ModuleInspectionResult result)
        {
            Mat output = ModuleRegionLocator.DrawRegions(source, result.Regions);
            DrawCoverageOverlay(output, result);
            DrawRoundedSilverRegions(output, result.RoundedSilverRegions);

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

        private static void DrawRoundedSilverRegions(
            Mat output,
            System.Collections.Generic.IEnumerable<AdaptiveSilverRoundedRegion> regions)
        {
            foreach (AdaptiveSilverRoundedRegion region in regions)
            {
                Rect box = ClampRectToImage(region.Box, output.Width, output.Height);
                int radius = Math.Max(1, Math.Min(
                    region.CornerRadius,
                    Math.Min(box.Width, box.Height) / 2));
                Scalar color = Scalar.Cyan;
                DrawRoundedRectangle(output, box, radius, color, 3);
                Cv2.PutText(output,
                    $"{region.Side} round B{region.MeanBrightness:F0}",
                    new OpenCvSharp.Point(box.X + 4, Math.Max(20, box.Y - 8)),
                    HersheyFonts.HersheySimplex, 0.45, color, 1);
            }
        }

        private static void DrawRoundedRectangle(
            Mat output,
            Rect box,
            int radius,
            Scalar color,
            int thickness)
        {
            int left = box.X;
            int top = box.Y;
            int right = box.X + box.Width - 1;
            int bottom = box.Y + box.Height - 1;

            Cv2.Line(output, new OpenCvSharp.Point(left + radius, top),
                new OpenCvSharp.Point(right - radius, top), color, thickness);
            Cv2.Line(output, new OpenCvSharp.Point(left + radius, bottom),
                new OpenCvSharp.Point(right - radius, bottom), color, thickness);
            Cv2.Line(output, new OpenCvSharp.Point(left, top + radius),
                new OpenCvSharp.Point(left, bottom - radius), color, thickness);
            Cv2.Line(output, new OpenCvSharp.Point(right, top + radius),
                new OpenCvSharp.Point(right, bottom - radius), color, thickness);
            Cv2.Ellipse(output, new OpenCvSharp.Point(left + radius, top + radius),
                new OpenCvSharp.Size(radius, radius), 0, 180, 270, color, thickness);
            Cv2.Ellipse(output, new OpenCvSharp.Point(right - radius, top + radius),
                new OpenCvSharp.Size(radius, radius), 0, 270, 360, color, thickness);
            Cv2.Ellipse(output, new OpenCvSharp.Point(right - radius, bottom - radius),
                new OpenCvSharp.Size(radius, radius), 0, 0, 90, color, thickness);
            Cv2.Ellipse(output, new OpenCvSharp.Point(left + radius, bottom - radius),
                new OpenCvSharp.Size(radius, radius), 0, 90, 180, color, thickness);
        }

        private static void DrawCoverageOverlay(Mat output, ModuleInspectionResult result)
        {
            Rect leftSilver = GetRegion(result.Regions, ModuleRegionType.LeftSilver);
            Rect rightSilver = GetRegion(result.Regions, ModuleRegionType.RightSilver);
            DrawCoverageText(output, leftSilver,
                $"L top {result.LeftSilverTopCoverage:P1}",
                $"L bottom {result.LeftSilverBottomCoverage:P1}");
            DrawCoverageText(output, rightSilver,
                $"R top {result.RightSilverTopCoverage:P1}",
                $"R bottom {result.RightSilverBottomCoverage:P1}");
        }

        private static void DrawCoverageText(
            Mat output,
            Rect rect,
            string topText,
            string bottomText)
        {
            int x = rect.X + 4;
            int y = rect.Y + 18;
            Cv2.PutText(output, topText, new OpenCvSharp.Point(x, y),
                HersheyFonts.HersheySimplex, 0.38, Scalar.Yellow, 1);
            Cv2.PutText(output, bottomText, new OpenCvSharp.Point(x, y + 16),
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
            // 画面边缘可能因照明渐变呈灰色；完整性检查只统计接近工件黑胶的深色，
            // 避免把正常的灰色背景当作工件被截断。
            int strictDarkThreshold = Math.Max(40, Math.Min(110, darkThreshold - 45));
            // 相机原图最外侧可能带有数个像素的黑边，向内偏移后再检查。
            int inset = Math.Max(10, borderMargin);
            inset = Math.Min(inset, Math.Min(gray.Width, gray.Height) / 4);
            int thickness = Math.Max(3, Math.Min(8, inset / 2));
            Rect[] borders =
            {
                new Rect(inset, inset, gray.Width - inset * 2, thickness),
                new Rect(inset, gray.Height - inset - thickness, gray.Width - inset * 2, thickness),
                new Rect(inset, inset, thickness, gray.Height - inset * 2),
                new Rect(gray.Width - inset - thickness, inset, thickness, gray.Height - inset * 2)
            };

            foreach (Rect border in borders)
            {
                double darkRatio = ThresholdRatio(
                    gray,
                    border,
                    strictDarkThreshold,
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

    }
}
