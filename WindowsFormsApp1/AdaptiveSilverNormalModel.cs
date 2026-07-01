using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace WindowsFormsApp1
{
    public sealed class AdaptiveSilverRoundedRegion
    {
        public string Side { get; set; }
        public Rect Box { get; set; }
        public int CornerRadius { get; set; }
        public double MeanBrightness { get; set; }
    }

    public sealed class AdaptiveSilverEvaluation
    {
        public AdaptiveSilverEvaluation()
        {
            RoundedRegions = new List<AdaptiveSilverRoundedRegion>();
        }

        public double LeftCoverage { get; set; }
        public double RightCoverage { get; set; }
        public double LeftTopCoverage { get; set; }
        public double LeftBottomCoverage { get; set; }
        public double RightTopCoverage { get; set; }
        public double RightBottomCoverage { get; set; }
        public List<AdaptiveSilverRoundedRegion> RoundedRegions { get; private set; }
    }

    public sealed class DarkDefectDebugStages : IDisposable
    {
        public Mat GradientMask { get; internal set; }
        public Mat SilverBodyMask { get; internal set; }
        public Mat SilverBodyEnvelope { get; internal set; }
        public Mat SilverBodySymmetricReference { get; internal set; }
        public Mat EdgeConcavityMask { get; internal set; }
        public Mat DarkInsideEnvelope { get; internal set; }
        public Mat ErodedDarkMask { get; internal set; }
        public Mat OpenedDarkMask { get; internal set; }
        public Mat EnvelopeBoundaryBand { get; internal set; }
        public Mat FinalDefectMask { get; internal set; }

        public void Dispose()
        {
            GradientMask?.Dispose();
            SilverBodyMask?.Dispose();
            SilverBodyEnvelope?.Dispose();
            SilverBodySymmetricReference?.Dispose();
            EdgeConcavityMask?.Dispose();
            DarkInsideEnvelope?.Dispose();
            ErodedDarkMask?.Dispose();
            OpenedDarkMask?.Dispose();
            EnvelopeBoundaryBand?.Dispose();
            FinalDefectMask?.Dispose();
        }
    }

    public sealed class AdaptiveSilverDarkDefect
    {
        public Rect Box { get; set; }
        public int PixelArea { get; set; }
    }

    public sealed class AdaptiveSilverEdgeConcavity
    {
        public Rect Box { get; set; }
        public int PixelArea { get; set; }
        public double MaximumDepth { get; set; }
        public double OpeningWidth { get; set; }
        public double AverageThickness { get; set; }
        public double EffectiveArea { get; set; }
    }

    /// <summary>银区覆盖率、内部缺陷和边缘凹陷检测。</summary>
    public static class AdaptiveSilverNormalModel
    {
        public static AdaptiveSilverEvaluation Evaluate(
            Mat gray,
            Rect leftSilver,
            Rect rightSilver,
            InspectionParameters parameters)
        {
            var output = new AdaptiveSilverEvaluation();
            SilverCoverageMeasurement left = MeasureGradientCoverage(
                gray, leftSilver, "L", parameters);
            SilverCoverageMeasurement right = MeasureGradientCoverage(
                gray, rightSilver, "R", parameters);

            output.LeftCoverage = left.Total;
            output.RightCoverage = right.Total;
            output.LeftTopCoverage = left.Top;
            output.LeftBottomCoverage = left.Bottom;
            output.RightTopCoverage = right.Top;
            output.RightBottomCoverage = right.Bottom;
            output.RoundedRegions.Add(left.RoundedRegion);
            output.RoundedRegions.Add(right.RoundedRegion);
            return output;
        }

        public static Mat CreateDebugGradientMask(
            Mat gray,
            Rect silverRect,
            byte blackThreshold = 90,
            byte whiteThreshold = 155)
        {
            if (gray == null || gray.Empty())
                throw new ArgumentException("gray image is empty.", nameof(gray));
            if (whiteThreshold <= blackThreshold)
                throw new ArgumentException("whiteThreshold must be greater than blackThreshold.");

            Rect bounded = BoundRect(silverRect, gray.Width, gray.Height);
            using (Mat roi = new Mat(gray, bounded))
            using (Mat blur = new Mat())
            {
                Cv2.GaussianBlur(roi, blur, new Size(3, 3), 0);
                using (Mat roundedMask = Mat.Zeros(blur.Rows, blur.Cols, MatType.CV_8UC1))
                {
                    int radius = Math.Max(2, (int)Math.Round(
                        Math.Min(blur.Width, blur.Height) * 0.15));
                    FillRoundedRectangle(
                        roundedMask,
                        new Rect(0, 0, blur.Width, blur.Height),
                        radius);

                    int median = MaskMedian(blur, roundedMask, whiteThreshold + 20);
                    whiteThreshold = (byte)Math.Max(145, Math.Min(180, median - 20));
                    blackThreshold = (byte)(whiteThreshold - 35);
                }

                Mat output = new Mat(blur.Rows, blur.Cols, MatType.CV_8UC1, Scalar.Black);
                for (int y = 0; y < blur.Rows; y++)
                for (int x = 0; x < blur.Cols; x++)
                {
                    byte value = blur.At<byte>(y, x);
                    byte mapped;
                    if (value >= whiteThreshold)
                        mapped = 255;
                    else if (value <= blackThreshold)
                        mapped = 0;
                    else
                        mapped = value;
                    output.Set(y, x, mapped);
                }
                return output;
            }
        }

        private static SilverCoverageMeasurement MeasureGradientCoverage(
            Mat gray,
            Rect silverRect,
            string side,
            InspectionParameters parameters)
        {
            Rect bounded = BoundRect(silverRect, gray.Width, gray.Height);
            using (Mat gradient = CreateDebugGradientMask(gray, bounded))
            using (Mat observed = BuildSilverBodyMask(gradient, parameters))
            {
                Rect box = new Rect(0, 0, observed.Width, observed.Height);
                int radius = Math.Max(2, (int)Math.Round(
                    Math.Min(box.Width, box.Height) * parameters.AdaptiveRoundedCornerRatio));
                radius = Math.Min(radius, Math.Min(box.Width, box.Height) / 2);

                using (Mat ideal = Mat.Zeros(observed.Rows, observed.Cols, MatType.CV_8UC1))
                using (Mat roi = new Mat(gray, bounded))
                {
                    FillRoundedRectangle(ideal, box, radius);
                    int middle = box.Y + box.Height / 2;
                    return new SilverCoverageMeasurement
                    {
                        Total = RoundedMaskRatio(observed, ideal, box.Y, box.Bottom),
                        Top = RoundedMaskRatio(observed, ideal, box.Y, middle),
                        Bottom = RoundedMaskRatio(observed, ideal, middle, box.Bottom),
                        RoundedRegion = new AdaptiveSilverRoundedRegion
                        {
                            Side = side,
                            Box = bounded,
                            CornerRadius = radius,
                            MeanBrightness = Cv2.Mean(roi, ideal).Val0
                        }
                    };
                }
            }
        }

        private static double RoundedMaskRatio(
            Mat observed,
            Mat ideal,
            int firstRow,
            int endRow)
        {
            int silver = 0;
            int total = 0;
            int y1 = Math.Max(0, firstRow);
            int y2 = Math.Min(ideal.Rows, endRow);
            for (int y = y1; y < y2; y++)
            for (int x = 0; x < ideal.Cols; x++)
            {
                if (ideal.At<byte>(y, x) == 0)
                    continue;
                total++;
                if (observed.At<byte>(y, x) != 0)
                    silver++;
            }
            return total == 0 ? 0 : silver / (double)total;
        }

        private static int MaskMedian(Mat gray, Mat mask, int fallback)
        {
            int[] histogram = new int[256];
            int count = 0;
            for (int y = 0; y < gray.Rows; y++)
            for (int x = 0; x < gray.Cols; x++)
            {
                if (mask.At<byte>(y, x) == 0)
                    continue;
                histogram[gray.At<byte>(y, x)]++;
                count++;
            }

            if (count == 0)
                return fallback;

            int middle = (count + 1) / 2;
            int accumulated = 0;
            for (int value = 0; value < histogram.Length; value++)
            {
                accumulated += histogram[value];
                if (accumulated >= middle)
                    return value;
            }
            return fallback;
        }

        public static Mat CreateDebugGradientMaskImage(
            Mat gray,
            Rect leftSilver,
            Rect rightSilver,
            byte blackThreshold = 120,
            byte whiteThreshold = 155)
        {
            if (gray == null || gray.Empty())
                throw new ArgumentException("gray image is empty.", nameof(gray));

            Mat output = new Mat(gray.Rows, gray.Cols, MatType.CV_8UC1, Scalar.Black);
            using (Mat left = CreateDebugGradientMask(gray, leftSilver, blackThreshold, whiteThreshold))
            using (Mat right = CreateDebugGradientMask(gray, rightSilver, blackThreshold, whiteThreshold))
            using (Mat leftRoi = new Mat(output, BoundRect(leftSilver, gray.Width, gray.Height)))
            using (Mat rightRoi = new Mat(output, BoundRect(rightSilver, gray.Width, gray.Height)))
            {
                left.CopyTo(leftRoi);
                right.CopyTo(rightRoi);
            }
            return output;
        }

        public static Mat CreateSilverBodyGradientMask(
            Mat gray,
            Rect silverRect,
            InspectionParameters parameters)
        {
            if (gray == null || gray.Empty())
                throw new ArgumentException("gray image is empty.", nameof(gray));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            parameters.Validate();
            Rect bounded = BoundRect(silverRect, gray.Width, gray.Height);
            using (Mat gradient = CreateDebugGradientMask(gray, bounded))
            using (Mat body = BuildSilverBodyMask(gradient, parameters))
            {
                Mat output = Mat.Zeros(gradient.Rows, gradient.Cols, MatType.CV_8UC1);
                gradient.CopyTo(output, body);
                return output;
            }
        }

        public static Mat CreateSilverBodyGradientMaskImage(
            Mat gray,
            Rect leftSilver,
            Rect rightSilver,
            InspectionParameters parameters)
        {
            Rect leftBox = BoundRect(leftSilver, gray.Width, gray.Height);
            Rect rightBox = BoundRect(rightSilver, gray.Width, gray.Height);
            Mat output = new Mat(gray.Rows, gray.Cols, MatType.CV_8UC1, Scalar.Black);
            using (Mat left = CreateSilverBodyGradientMask(gray, leftBox, parameters))
            using (Mat right = CreateSilverBodyGradientMask(gray, rightBox, parameters))
            using (Mat leftOutput = new Mat(output, leftBox))
            using (Mat rightOutput = new Mat(output, rightBox))
            {
                left.CopyTo(leftOutput);
                right.CopyTo(rightOutput);
            }
            return output;
        }

        private static Mat BuildSilverBodyMask(
            Mat gradient,
            InspectionParameters parameters)
        {
            int minimumDimension = Math.Min(gradient.Width, gradient.Height);
            int closeSize = RatioKernel(
                minimumDimension, parameters.AdaptiveMaskCloseRatio);
            int openSize = RatioKernel(
                minimumDimension, parameters.AdaptiveMaskOpenRatio);
            using (Mat nonBlack = new Mat())
            using (Mat rounded = Mat.Zeros(
                gradient.Rows, gradient.Cols, MatType.CV_8UC1))
            using (Mat closed = new Mat())
            using (Mat opened = new Mat())
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            using (Mat closeKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse, new Size(closeSize, closeSize)))
            using (Mat openKernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse, new Size(openSize, openSize)))
            {
                Cv2.Threshold(gradient, nonBlack, 0, 255, ThresholdTypes.Binary);
                int radius = Math.Max(2, (int)Math.Round(
                    minimumDimension * parameters.AdaptiveRoundedCornerRatio));
                FillRoundedRectangle(
                    rounded,
                    new Rect(0, 0, gradient.Width, gradient.Height),
                    radius);
                Cv2.BitwiseAnd(nonBlack, rounded, nonBlack);
                Cv2.MorphologyEx(nonBlack, closed, MorphTypes.Close, closeKernel);
                Cv2.MorphologyEx(closed, opened, MorphTypes.Open, openKernel);

                int componentCount = Cv2.ConnectedComponentsWithStats(
                    opened, labels, stats, centroids);
                int seedX1 = gradient.Width / 5;
                int seedX2 = gradient.Width - seedX1;
                int seedY1 = gradient.Height / 4;
                int seedY2 = gradient.Height - seedY1;
                int[] seedSupport = new int[componentCount];
                for (int y = seedY1; y < seedY2; y++)
                for (int x = seedX1; x < seedX2; x++)
                {
                    int label = labels.At<int>(y, x);
                    if (label > 0)
                        seedSupport[label]++;
                }

                int bestLabel = 0;
                int bestSupport = 0;
                int bestArea = 0;
                for (int label = 1; label < componentCount; label++)
                {
                    int area = stats.At<int>(label, (int)ConnectedComponentsTypes.Area);
                    if (seedSupport[label] < bestSupport ||
                        (seedSupport[label] == bestSupport && area <= bestArea))
                        continue;
                    bestLabel = label;
                    bestSupport = seedSupport[label];
                    bestArea = area;
                }

                Mat body = Mat.Zeros(gradient.Rows, gradient.Cols, MatType.CV_8UC1);
                if (bestLabel == 0)
                    return body;

                for (int y = 0; y < labels.Rows; y++)
                for (int x = 0; x < labels.Cols; x++)
                    if (labels.At<int>(y, x) == bestLabel)
                        body.Set(y, x, (byte)255);

                using (Mat recovered = new Mat())
                using (Mat recoverKernel = Cv2.GetStructuringElement(
                    MorphShapes.Ellipse, new Size(3, 3)))
                {
                    Cv2.Dilate(body, recovered, recoverKernel);
                    Cv2.BitwiseAnd(recovered, closed, body);
                }
                return body;
            }
        }

        private static Mat BuildSilverBodyEnvelope(Mat bodyMask)
        {
            using (Mat contourSource = bodyMask.Clone())
            {
                Cv2.FindContours(
                    contourSource,
                    out OpenCvSharp.Point[][] contours,
                    out HierarchyIndex[] _,
                    RetrievalModes.External,
                    ContourApproximationModes.ApproxSimple);
                Mat envelope = Mat.Zeros(bodyMask.Rows, bodyMask.Cols, MatType.CV_8UC1);
                if (contours.Length == 0)
                    return envelope;

                int bestIndex = 0;
                double bestArea = 0;
                for (int i = 0; i < contours.Length; i++)
                {
                    double area = Cv2.ContourArea(contours[i]);
                    if (area <= bestArea)
                        continue;
                    bestArea = area;
                    bestIndex = i;
                }
                Cv2.DrawContours(envelope, contours, bestIndex, Scalar.White, -1);
                return envelope;
            }
        }

        private static Mat BuildConvexHullMask(Mat bodyMask)
        {
            using (Mat contourSource = bodyMask.Clone())
            {
                Cv2.FindContours(
                    contourSource,
                    out OpenCvSharp.Point[][] contours,
                    out HierarchyIndex[] _,
                    RetrievalModes.External,
                    ContourApproximationModes.ApproxSimple);
                Mat hullMask = Mat.Zeros(
                    bodyMask.Rows, bodyMask.Cols, MatType.CV_8UC1);
                if (contours.Length == 0)
                    return hullMask;

                int bestIndex = 0;
                double bestArea = 0;
                for (int i = 0; i < contours.Length; i++)
                {
                    double area = Cv2.ContourArea(contours[i]);
                    if (area <= bestArea)
                        continue;
                    bestArea = area;
                    bestIndex = i;
                }

                OpenCvSharp.Point[] hull = Cv2.ConvexHull(contours[bestIndex]);
                if (hull.Length > 0)
                    Cv2.FillConvexPoly(hullMask, hull, Scalar.White);
                return hullMask;
            }
        }

        private static Mat BuildSilverBodySymmetricReference(Mat filledBodyMask)
        {
            int[] left = new int[filledBodyMask.Rows];
            int[] right = new int[filledBodyMask.Rows];
            int[] top = new int[filledBodyMask.Cols];
            int[] bottom = new int[filledBodyMask.Cols];
            for (int x = 0; x < filledBodyMask.Cols; x++)
            {
                top[x] = -1;
                bottom[x] = -1;
            }

            int maximumWidth = 0;
            for (int y = 0; y < filledBodyMask.Rows; y++)
            {
                left[y] = -1;
                right[y] = -1;
                for (int x = 0; x < filledBodyMask.Cols; x++)
                {
                    if (filledBodyMask.At<byte>(y, x) == 0)
                        continue;
                    if (left[y] < 0)
                        left[y] = x;
                    right[y] = x;
                    if (top[x] < 0)
                        top[x] = y;
                    bottom[x] = y;
                }
                if (left[y] >= 0)
                    maximumWidth = Math.Max(maximumWidth, right[y] - left[y] + 1);
            }

            Mat reference = filledBodyMask.Clone();
            if (maximumWidth <= 0)
                return reference;

            var doubledCenters = new List<int>();
            for (int y = 0; y < filledBodyMask.Rows; y++)
            {
                int width = left[y] < 0 ? 0 : right[y] - left[y] + 1;
                if (width >= maximumWidth / 2)
                    doubledCenters.Add(left[y] + right[y]);
            }
            doubledCenters.Sort();
            int doubledCenter = doubledCenters.Count == 0
                ? filledBodyMask.Cols - 1
                : doubledCenters[doubledCenters.Count / 2];

            int shortSide = Math.Min(filledBodyMask.Rows, filledBodyMask.Cols);
            int smoothRadius = Math.Max(5, (int)Math.Round(shortSide * 0.06));
            int maximumCorrection = Math.Max(3, (int)Math.Round(shortSide * 0.30));
            int[] fittedLeft = SmoothBoundaryProfile(left, smoothRadius);
            int[] fittedRight = SmoothBoundaryProfile(right, smoothRadius);
            int[] fittedTop = SmoothBoundaryProfile(top, smoothRadius);
            int[] fittedBottom = SmoothBoundaryProfile(bottom, smoothRadius);

            int firstRow = Array.FindIndex(left, value => value >= 0);
            int lastRow = Array.FindLastIndex(left, value => value >= 0);
            int cornerHeight = Math.Max(8,
                (int)Math.Round((lastRow - firstRow + 1) * 0.30));
            int topEnd = Math.Min(lastRow, firstRow + cornerHeight - 1);
            int bottomStart = Math.Max(firstRow, lastRow - cornerHeight + 1);
            double leftTopWeight = CornerRepairWeight(
                left, right, fittedLeft, doubledCenter,
                firstRow, topEnd, true);
            double rightTopWeight = CornerRepairWeight(
                left, right, fittedRight, doubledCenter,
                firstRow, topEnd, false);
            double leftBottomWeight = CornerRepairWeight(
                left, right, fittedLeft, doubledCenter,
                bottomStart, lastRow, true);
            double rightBottomWeight = CornerRepairWeight(
                left, right, fittedRight, doubledCenter,
                bottomStart, lastRow, false);

            using (Mat horizontalReference = Mat.Zeros(
                filledBodyMask.Rows, filledBodyMask.Cols, MatType.CV_8UC1))
            using (Mat verticalReference = Mat.Zeros(
                filledBodyMask.Rows, filledBodyMask.Cols, MatType.CV_8UC1))
            {
                for (int y = 0; y < filledBodyMask.Rows; y++)
                {
                    if (left[y] < 0 || right[y] < 0)
                        continue;
                    int mirrorLeft = doubledCenter - right[y];
                    int mirrorRight = doubledCenter - left[y];
                    double leftCornerWeight = y <= topEnd
                        ? leftTopWeight
                        : y >= bottomStart ? leftBottomWeight : 0;
                    double rightCornerWeight = y <= topEnd
                        ? rightTopWeight
                        : y >= bottomStart ? rightBottomWeight : 0;
                    int expectedLeft = AdaptiveOuterCoordinate(
                        left, fittedLeft, y, mirrorLeft, true,
                        maximumCorrection, leftCornerWeight);
                    int expectedRight = AdaptiveOuterCoordinate(
                        right, fittedRight, y, mirrorRight, false,
                        maximumCorrection, rightCornerWeight);
                    expectedLeft = Math.Max(0, expectedLeft);
                    expectedRight = Math.Min(filledBodyMask.Cols - 1, expectedRight);
                    if (expectedRight >= expectedLeft)
                        Cv2.Line(horizontalReference,
                            new Point(expectedLeft, y),
                            new Point(expectedRight, y), Scalar.White, 1);
                }

                for (int x = 0; x < filledBodyMask.Cols; x++)
                {
                    if (top[x] < 0 || bottom[x] < 0)
                        continue;
                    int mirroredX = doubledCenter - x;
                    int mirrorTop = mirroredX >= 0 && mirroredX < top.Length
                        ? top[mirroredX]
                        : top[x];
                    int mirrorBottom = mirroredX >= 0 && mirroredX < bottom.Length
                        ? bottom[mirroredX]
                        : bottom[x];
                    bool leftHalf = x * 2 <= doubledCenter;
                    double topCornerWeight = leftHalf
                        ? leftTopWeight
                        : rightTopWeight;
                    double bottomCornerWeight = leftHalf
                        ? leftBottomWeight
                        : rightBottomWeight;
                    int expectedTop = AdaptiveOuterCoordinate(
                        top, fittedTop, x, mirrorTop, true,
                        maximumCorrection, topCornerWeight);
                    int expectedBottom = AdaptiveOuterCoordinate(
                        bottom, fittedBottom, x, mirrorBottom, false,
                        maximumCorrection, bottomCornerWeight);
                    expectedTop = Math.Max(0, expectedTop);
                    expectedBottom = Math.Min(filledBodyMask.Rows - 1, expectedBottom);
                    if (expectedBottom >= expectedTop)
                        Cv2.Line(verticalReference,
                            new Point(x, expectedTop),
                            new Point(x, expectedBottom), Scalar.White, 1);
                }

                using (Mat fittedReference = new Mat())
                using (Mat convexHullMask = BuildConvexHullMask(filledBodyMask))
                {
                    Cv2.BitwiseAnd(
                        horizontalReference,
                        verticalReference,
                        fittedReference);
                    // 正常圆角本身是凸边界。镜像/平滑参考不得越过主体凸包，
                    // 否则轻微不对称的正常圆角会被错误地当作缺口填补。
                    Cv2.BitwiseAnd(
                        fittedReference,
                        convexHullMask,
                        fittedReference);
                    double minimumRepairDepth = Math.Max(
                        4.0, shortSide * 0.03);
                    AddDeepConcavityRepairs(
                        reference,
                        filledBodyMask,
                        fittedReference,
                        minimumRepairDepth);
                }
            }
            return reference;
        }

        private static void AddDeepConcavityRepairs(
            Mat reference,
            Mat bodyMask,
            Mat fittedReference,
            double minimumDepth)
        {
            using (Mat inverseBody = new Mat())
            using (Mat additions = new Mat())
            using (Mat distance = new Mat())
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                Cv2.BitwiseNot(bodyMask, inverseBody);
                Cv2.BitwiseAnd(fittedReference, inverseBody, additions);
                Cv2.DistanceTransform(
                    inverseBody,
                    distance,
                    DistanceTypes.L2,
                    DistanceTransformMasks.Mask3);
                int count = Cv2.ConnectedComponentsWithStats(
                    additions, labels, stats, centroids);

                for (int label = 1; label < count; label++)
                {
                    int x = stats.At<int>(label, (int)ConnectedComponentsTypes.Left);
                    int y = stats.At<int>(label, (int)ConnectedComponentsTypes.Top);
                    int width = stats.At<int>(label, (int)ConnectedComponentsTypes.Width);
                    int height = stats.At<int>(label, (int)ConnectedComponentsTypes.Height);
                    double maximumDepth = 0;
                    for (int row = y; row < y + height; row++)
                    for (int column = x; column < x + width; column++)
                    {
                        if (labels.At<int>(row, column) != label)
                            continue;
                        maximumDepth = Math.Max(
                            maximumDepth,
                            distance.At<float>(row, column));
                    }

                    if (maximumDepth < minimumDepth)
                        continue;

                    for (int row = y; row < y + height; row++)
                    for (int column = x; column < x + width; column++)
                        if (labels.At<int>(row, column) == label)
                            reference.Set(row, column, (byte)255);
                }
            }
        }

        private static int[] SmoothBoundaryProfile(int[] values, int radius)
        {
            int[] firstPass = new int[values.Length];
            int[] output = new int[values.Length];
            SmoothBoundaryPass(values, firstPass, radius);
            SmoothBoundaryPass(firstPass, output, Math.Max(2, radius / 2));
            return output;
        }

        private static void SmoothBoundaryPass(int[] source, int[] target, int radius)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] < 0)
                {
                    target[i] = -1;
                    continue;
                }
                int sum = 0;
                int count = 0;
                for (int j = Math.Max(0, i - radius);
                     j <= Math.Min(source.Length - 1, i + radius); j++)
                {
                    if (source[j] < 0)
                        continue;
                    sum += source[j];
                    count++;
                }
                target[i] = count == 0
                    ? source[i]
                    : (int)Math.Round(sum / (double)count);
            }
        }

        private static int AdaptiveOuterCoordinate(
            int[] actual,
            int[] fitted,
            int index,
            int mirrored,
            bool smallerIsOutward,
            int maximumCorrection,
            double cornerWeight)
        {
            int current = actual[index];
            int fit = fitted[index] < 0 ? current : fitted[index];
            int previous = index > 0 && actual[index - 1] >= 0
                ? actual[index - 1]
                : current;
            int previousFit = index > 0 && fitted[index - 1] >= 0
                ? fitted[index - 1]
                : fit;

            int inwardDepth = smallerIsOutward
                ? Math.Max(0, current - fit)
                : Math.Max(0, fit - current);
            int slopeBreak = Math.Abs(
                (current - previous) - (fit - previousFit));
            double severity = Math.Max(inwardDepth, slopeBreak * 1.5);
            double localWeight = Math.Max(0, Math.Min(1, (severity - 2.0) / 10.0));
            double weight = Math.Max(localWeight, cornerWeight);
            if (weight <= 0)
                return current;

            int target;
            if (cornerWeight > localWeight)
                target = mirrored;
            else
                target = smallerIsOutward
                    ? Math.Min(fit, mirrored)
                    : Math.Max(fit, mirrored);
            int delta = target - current;
            if ((smallerIsOutward && delta >= 0) ||
                (!smallerIsOutward && delta <= 0))
                return current;

            delta = Math.Max(-maximumCorrection, Math.Min(maximumCorrection, delta));
            return (int)Math.Round(current + delta * weight);
        }

        private static double CornerRepairWeight(
            int[] left,
            int[] right,
            int[] fittedSide,
            int doubledCenter,
            int first,
            int last,
            bool leftSide)
        {
            if (first < 0 || last < first)
                return 0;

            int maximumGap = 0;
            int maximumFitFailure = 0;
            for (int i = first; i <= last; i++)
            {
                if (left[i] < 0 || right[i] < 0 || fittedSide[i] < 0)
                    continue;
                int actual = leftSide ? left[i] : right[i];
                int mirrored = leftSide
                    ? doubledCenter - right[i]
                    : doubledCenter - left[i];
                int gap = leftSide
                    ? Math.Max(0, actual - mirrored)
                    : Math.Max(0, mirrored - actual);
                maximumGap = Math.Max(maximumGap, gap);

                int previous = i > first &&
                    (leftSide ? left[i - 1] : right[i - 1]) >= 0
                    ? (leftSide ? left[i - 1] : right[i - 1])
                    : actual;
                int previousFit = i > first && fittedSide[i - 1] >= 0
                    ? fittedSide[i - 1]
                    : fittedSide[i];
                int inward = leftSide
                    ? Math.Max(0, actual - fittedSide[i])
                    : Math.Max(0, fittedSide[i] - actual);
                int slopeBreak = Math.Abs(
                    (actual - previous) - (fittedSide[i] - previousFit));
                maximumFitFailure = Math.Max(
                    maximumFitFailure,
                    Math.Max(inward, slopeBreak * 2));
            }

            double fitFailureWeight = Math.Max(0, Math.Min(1,
                (maximumFitFailure - 3.0) / 9.0));
            double missingDepthWeight = Math.Max(0, Math.Min(1,
                (maximumGap - 5.0) / 25.0));
            return fitFailureWeight * missingDepthWeight;
        }

        public static Mat CreateDarkDefectMask(
            Mat gray,
            Rect silverRect,
            InspectionParameters parameters)
        {
            if (gray == null || gray.Empty())
                throw new ArgumentException("gray image is empty.", nameof(gray));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (DarkDefectDebugStages stages = CreateDarkDefectDebugStages(
                gray, silverRect, parameters))
            {
                return stages.FinalDefectMask.Clone();
            }
        }

        public static DarkDefectDebugStages CreateDarkDefectDebugStages(
            Mat gray,
            Rect silverRect,
            InspectionParameters parameters)
        {
            if (gray == null || gray.Empty())
                throw new ArgumentException("gray image is empty.", nameof(gray));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            parameters.Validate();
            Rect bounded = BoundRect(silverRect, gray.Width, gray.Height);
            int maximumKernel = Math.Min(bounded.Width, bounded.Height);
            int morphSize = Math.Min(
                parameters.DefectMorphKernelSize,
                (maximumKernel & 1) == 0 ? maximumKernel - 1 : maximumKernel);
            morphSize = Math.Max(3, morphSize);
            int minimumArea = parameters.InnerDefectReviewArea;

            var stages = new DarkDefectDebugStages();
            try
            {
                stages.GradientMask = CreateDebugGradientMask(gray, bounded);
                stages.SilverBodyMask = BuildSilverBodyMask(
                    stages.GradientMask, parameters);
                stages.SilverBodyEnvelope = BuildSilverBodyEnvelope(
                    stages.SilverBodyMask);
                stages.SilverBodySymmetricReference = BuildSilverBodySymmetricReference(
                    stages.SilverBodyEnvelope);
                stages.EdgeConcavityMask = new Mat();
                Cv2.Subtract(
                    stages.SilverBodySymmetricReference,
                    stages.SilverBodyEnvelope,
                    stages.EdgeConcavityMask);
                stages.DarkInsideEnvelope = new Mat();
                Cv2.Subtract(
                    stages.SilverBodyEnvelope,
                    stages.SilverBodyMask,
                    stages.DarkInsideEnvelope);

                using (Mat morphKernel = Cv2.GetStructuringElement(
                    MorphShapes.Ellipse, new Size(morphSize, morphSize)))
                using (Mat boundaryKernel = Cv2.GetStructuringElement(
                    MorphShapes.Ellipse, new Size(3, 3)))
                using (Mat envelopeInterior = new Mat())
                {
                    stages.ErodedDarkMask = new Mat();
                    stages.OpenedDarkMask = new Mat();
                    Cv2.Erode(
                        stages.DarkInsideEnvelope,
                        stages.ErodedDarkMask,
                        morphKernel);
                    Cv2.Dilate(
                        stages.ErodedDarkMask,
                        stages.OpenedDarkMask,
                        morphKernel);

                    Cv2.Erode(
                        stages.SilverBodyEnvelope,
                        envelopeInterior,
                        boundaryKernel);
                    stages.EnvelopeBoundaryBand = new Mat();
                    Cv2.Subtract(
                        stages.SilverBodyEnvelope,
                        envelopeInterior,
                        stages.EnvelopeBoundaryBand);
                }

                stages.FinalDefectMask = FilterInternalComponents(
                    stages.OpenedDarkMask,
                    stages.SilverBodyEnvelope,
                    minimumArea);
                return stages;
            }
            catch
            {
                stages.Dispose();
                throw;
            }
        }

        private static Mat FilterInternalComponents(
            Mat source,
            Mat envelope,
            int minimumArea)
        {
            using (Mat eroded = new Mat())
            using (Mat boundary = new Mat())
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            using (Mat kernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse, new Size(3, 3)))
            {
                Cv2.Erode(envelope, eroded, kernel);
                Cv2.Subtract(envelope, eroded, boundary);
                int count = Cv2.ConnectedComponentsWithStats(
                    source, labels, stats, centroids);
                Mat output = Mat.Zeros(source.Rows, source.Cols, MatType.CV_8UC1);
                for (int label = 1; label < count; label++)
                {
                    int area = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Area);
                    if (area < minimumArea)
                        continue;

                    int x = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Left);
                    int y = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Top);
                    int width = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Width);
                    int height = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Height);
                    bool touchesBoundary = false;
                    for (int row = y; row < y + height && !touchesBoundary; row++)
                    for (int column = x; column < x + width; column++)
                    {
                        if (labels.At<int>(row, column) == label &&
                            boundary.At<byte>(row, column) != 0)
                        {
                            touchesBoundary = true;
                            break;
                        }
                    }
                    if (touchesBoundary)
                        continue;

                    for (int row = y; row < y + height; row++)
                    for (int column = x; column < x + width; column++)
                        if (labels.At<int>(row, column) == label)
                            output.Set(row, column, (byte)255);
                }
                return output;
            }
        }

        public static Mat CreateDarkDefectMaskImage(
            Mat gray,
            Rect leftSilver,
            Rect rightSilver,
            InspectionParameters parameters)
        {
            if (gray == null || gray.Empty())
                throw new ArgumentException("gray image is empty.", nameof(gray));

            Rect leftBox = BoundRect(leftSilver, gray.Width, gray.Height);
            Rect rightBox = BoundRect(rightSilver, gray.Width, gray.Height);
            Mat output = new Mat(gray.Rows, gray.Cols, MatType.CV_8UC1, Scalar.Black);
            using (Mat left = CreateDarkDefectMask(gray, leftBox, parameters))
            using (Mat right = CreateDarkDefectMask(gray, rightBox, parameters))
            using (Mat leftOutput = new Mat(output, leftBox))
            using (Mat rightOutput = new Mat(output, rightBox))
            {
                left.CopyTo(leftOutput);
                right.CopyTo(rightOutput);
            }
            return output;
        }

        public static List<AdaptiveSilverDarkDefect> LocateDarkDefects(
            Mat gray,
            Rect silverRect,
            InspectionParameters parameters)
        {
            Rect bounded = BoundRect(silverRect, gray.Width, gray.Height);
            var defects = new List<AdaptiveSilverDarkDefect>();
            using (Mat mask = CreateDarkDefectMask(gray, bounded, parameters))
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                int count = Cv2.ConnectedComponentsWithStats(
                    mask, labels, stats, centroids);
                for (int label = 1; label < count; label++)
                {
                    int area = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Area);
                    defects.Add(new AdaptiveSilverDarkDefect
                    {
                        PixelArea = area,
                        Box = new Rect(
                            bounded.X + stats.At<int>(
                                label, (int)ConnectedComponentsTypes.Left),
                            bounded.Y + stats.At<int>(
                                label, (int)ConnectedComponentsTypes.Top),
                            stats.At<int>(
                                label, (int)ConnectedComponentsTypes.Width),
                            stats.At<int>(
                                label, (int)ConnectedComponentsTypes.Height))
                    });
                }
            }
            defects.Sort((first, second) =>
                second.PixelArea.CompareTo(first.PixelArea));
            return defects;
        }

        public static List<AdaptiveSilverEdgeConcavity> MeasureEdgeConcavities(
            Mat gray,
            Rect silverRect,
            InspectionParameters parameters)
        {
            Rect bounded = BoundRect(silverRect, gray.Width, gray.Height);
            using (DarkDefectDebugStages stages = CreateDarkDefectDebugStages(
                gray, bounded, parameters))
            using (Mat inverseBody = new Mat())
            using (Mat distance = new Mat())
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                Cv2.BitwiseNot(stages.SilverBodyEnvelope, inverseBody);
                Cv2.DistanceTransform(
                    inverseBody,
                    distance,
                    DistanceTypes.L2,
                    DistanceTransformMasks.Mask3);
                int count = Cv2.ConnectedComponentsWithStats(
                    stages.EdgeConcavityMask,
                    labels,
                    stats,
                    centroids);
                var output = new List<AdaptiveSilverEdgeConcavity>();
                for (int label = 1; label < count; label++)
                {
                    int x = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Left);
                    int y = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Top);
                    int width = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Width);
                    int height = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Height);
                    double maximumDepth = 0;
                    for (int row = y; row < y + height; row++)
                    for (int column = x; column < x + width; column++)
                    {
                        if (labels.At<int>(row, column) != label)
                            continue;
                        maximumDepth = Math.Max(
                            maximumDepth,
                            distance.At<float>(row, column));
                    }
                    output.Add(new AdaptiveSilverEdgeConcavity
                    {
                        PixelArea = stats.At<int>(
                            label, (int)ConnectedComponentsTypes.Area),
                        MaximumDepth = maximumDepth,
                        OpeningWidth = Math.Max(width, height),
                        Box = new Rect(
                            bounded.X + x,
                            bounded.Y + y,
                            width,
                            height)
                    });
                }
                foreach (AdaptiveSilverEdgeConcavity item in output)
                    CalculateEdgeConcavityShape(item, parameters);
                output.Sort((first, second) =>
                    second.EffectiveArea.CompareTo(first.EffectiveArea));
                return output;
            }
        }

        private static void CalculateEdgeConcavityShape(
            AdaptiveSilverEdgeConcavity concavity,
            InspectionParameters parameters)
        {
            double opening = Math.Max(1.0, concavity.OpeningWidth);
            concavity.AverageThickness = concavity.PixelArea / opening;
            concavity.EffectiveArea = concavity.PixelArea *
                concavity.MaximumDepth / opening *
                parameters.EdgeConcavityShapeScale;
        }

        public static int ClassifyEdgeConcavity(
            AdaptiveSilverEdgeConcavity concavity,
            InspectionParameters parameters)
        {
            if (concavity == null || parameters == null)
                return 0;
            CalculateEdgeConcavityShape(concavity, parameters);
            if (concavity.AverageThickness <
                parameters.EdgeConcavityMinimumThickness)
                return 0;
            if (concavity.EffectiveArea >= parameters.EdgeConcavityNgArea)
                return 2;
            return concavity.EffectiveArea >= parameters.EdgeConcavityReviewArea
                ? 1
                : 0;
        }

        public static Mat CreateEdgeConcavityClassificationImage(
            Mat gray,
            Rect leftSilver,
            Rect rightSilver,
            InspectionParameters parameters)
        {
            Rect leftBox = BoundRect(leftSilver, gray.Width, gray.Height);
            Rect rightBox = BoundRect(rightSilver, gray.Width, gray.Height);
            Mat output = new Mat(
                gray.Rows, gray.Cols, MatType.CV_8UC3, Scalar.Black);
            using (DarkDefectDebugStages leftStages = CreateDarkDefectDebugStages(
                gray, leftBox, parameters))
            using (DarkDefectDebugStages rightStages = CreateDarkDefectDebugStages(
                gray, rightBox, parameters))
            using (Mat leftOutput = new Mat(output, leftBox))
            using (Mat rightOutput = new Mat(output, rightBox))
            {
                PaintEdgeConcavityClasses(
                    leftStages.EdgeConcavityMask,
                    leftStages.SilverBodyEnvelope,
                    leftOutput,
                    parameters);
                PaintEdgeConcavityClasses(
                    rightStages.EdgeConcavityMask,
                    rightStages.SilverBodyEnvelope,
                    rightOutput,
                    parameters);
            }
            return output;
        }

        private static void PaintEdgeConcavityClasses(
            Mat concavityMask,
            Mat bodyMask,
            Mat output,
            InspectionParameters parameters)
        {
            using (Mat inverseBody = new Mat())
            using (Mat distance = new Mat())
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                Cv2.BitwiseNot(bodyMask, inverseBody);
                Cv2.DistanceTransform(
                    inverseBody,
                    distance,
                    DistanceTypes.L2,
                    DistanceTransformMasks.Mask3);
                int count = Cv2.ConnectedComponentsWithStats(
                    concavityMask, labels, stats, centroids);
                for (int label = 1; label < count; label++)
                {
                    int area = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Area);
                    int x = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Left);
                    int y = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Top);
                    int width = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Width);
                    int height = stats.At<int>(
                        label, (int)ConnectedComponentsTypes.Height);
                    double maximumDepth = 0;
                    for (int row = y; row < y + height; row++)
                    for (int column = x; column < x + width; column++)
                    {
                        if (labels.At<int>(row, column) == label)
                            maximumDepth = Math.Max(
                                maximumDepth,
                                distance.At<float>(row, column));
                    }

                    var concavity = new AdaptiveSilverEdgeConcavity
                    {
                        PixelArea = area,
                        MaximumDepth = maximumDepth,
                        OpeningWidth = Math.Max(width, height)
                    };
                    int classification = ClassifyEdgeConcavity(
                        concavity, parameters);
                    Vec3b color;
                    if (classification == 2)
                        color = new Vec3b(0, 0, 255);
                    else if (classification == 1)
                        color = new Vec3b(0, 255, 0);
                    else
                        color = new Vec3b(0, 255, 255);

                    for (int row = y; row < y + height; row++)
                    for (int column = x; column < x + width; column++)
                        if (labels.At<int>(row, column) == label)
                            output.Set(row, column, color);
                }
            }
        }

        private static void FillRoundedRectangle(Mat mask, Rect box, int radius)
        {
            radius = Math.Max(1, Math.Min(radius, Math.Min(box.Width, box.Height) / 2));
            Scalar white = Scalar.White;
            Cv2.Rectangle(mask,
                new Rect(box.X + radius, box.Y, Math.Max(1, box.Width - radius * 2), box.Height),
                white, -1);
            Cv2.Rectangle(mask,
                new Rect(box.X, box.Y + radius, box.Width, Math.Max(1, box.Height - radius * 2)),
                white, -1);

            int right = box.X + box.Width - radius - 1;
            int bottom = box.Y + box.Height - radius - 1;
            Cv2.Circle(mask, new Point(box.X + radius, box.Y + radius), radius, white, -1);
            Cv2.Circle(mask, new Point(right, box.Y + radius), radius, white, -1);
            Cv2.Circle(mask, new Point(right, bottom), radius, white, -1);
            Cv2.Circle(mask, new Point(box.X + radius, bottom), radius, white, -1);
        }
        private static Rect BoundRect(Rect rect, int width, int height)
        {
            int x = Math.Max(0, Math.Min(width - 1, rect.X));
            int y = Math.Max(0, Math.Min(height - 1, rect.Y));
            int right = Math.Max(x + 1, Math.Min(width, rect.Right));
            int bottom = Math.Max(y + 1, Math.Min(height, rect.Bottom));
            return new Rect(x, y, right - x, bottom - y);
        }
        private static int RatioKernel(int dimension, double ratio)
        {
            int value = Math.Max(3, Math.Min(dimension, (int)Math.Round(dimension * ratio)));
            if ((value & 1) == 0) value--;
            return Math.Max(3, value);
        }
        private sealed class SilverCoverageMeasurement
        {
            public double Total;
            public double Top;
            public double Bottom;
            public AdaptiveSilverRoundedRegion RoundedRegion;
        }
    }
}
