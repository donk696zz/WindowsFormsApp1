using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace WindowsFormsApp1
{
    public sealed class AdaptiveSilverRegion
    {
        public Rect Box { get; set; }
        public double Ratio { get; set; }
        public string Kind { get; set; }
        public bool IsNg { get; set; }
    }

    public sealed class AdaptiveSilverEvaluation
    {
        public AdaptiveSilverEvaluation()
        {
            Regions = new List<AdaptiveSilverRegion>();
        }

        public bool HasModel { get; set; }
        public double NormalDistance { get; set; }
        public double OkDistanceThreshold { get; set; }
        public bool IsObviousOk => HasModel && NormalDistance <= OkDistanceThreshold;
        public List<AdaptiveSilverRegion> Regions { get; private set; }
    }

    /// <summary>
    /// 仅使用确认OK图片建立正常范围。模型负责安全放行明显OK；明显形状异常可定位，
    /// 其余图片只进入复检，不会为了画框而选择不可靠候选区域。
    /// </summary>
    public static class AdaptiveSilverNormalModel
    {
        private const int ModelVersion = 1;
        private const int NormalizedWidth = 64;
        private const int NormalizedHeight = 96;
        private const string ModelFileName = "silver_normal_model.bin";
        private static readonly object ModelSync = new object();
        private static ModelData cachedModel;
        private static string cachedPath;
        private static DateTime cachedWriteTimeUtc;

        public static string RuntimeModelPath => Path.Combine(
            Path.GetDirectoryName(typeof(AdaptiveSilverNormalModel).Assembly.Location),
            "检测模型",
            ModelFileName);

        public static AdaptiveSilverEvaluation Evaluate(
            Mat gray,
            Rect leftSilver,
            Rect rightSilver,
            InspectionParameters parameters)
        {
            var output = new AdaptiveSilverEvaluation();
            string path = RuntimeModelPath;
            if (!File.Exists(path))
                return output;

            try
            {
                ModelData model = GetCachedModel(path);
                SilverSample sample = ExtractSample(gray, leftSilver, rightSilver, parameters);
                output.HasModel = true;
                output.OkDistanceThreshold = model.OkDistanceThreshold;
                output.NormalDistance = FindNearestDistance(sample.Feature, model.Prototypes, model.FeatureLength);
                AddReliableRegions(output, sample, model, leftSilver, rightSilver, parameters);
            }
            catch
            {
                output.HasModel = false;
            }
            return output;
        }

        public static void Train(
            IEnumerable<string> okFiles,
            IEnumerable<string> nonOkCalibrationFiles,
            string modelPath,
            RegionParameters regionParameters,
            InspectionParameters parameters)
        {
            if (okFiles == null) throw new ArgumentNullException(nameof(okFiles));
            if (regionParameters == null) throw new ArgumentNullException(nameof(regionParameters));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            regionParameters.Validate();
            parameters.Validate();
            List<string> ok = UniqueExistingFiles(okFiles).ToList();
            if (ok.Count < 10)
                throw new InvalidOperationException("At least 10 confirmed OK images are required.");

            var samples = new List<SilverSample>(ok.Count);
            foreach (string file in ok)
                samples.Add(ExtractFile(file, regionParameters, parameters));

            int pixelCount = NormalizedWidth * NormalizedHeight;
            var leftProbability = new float[pixelCount];
            var rightProbability = new float[pixelCount];
            foreach (SilverSample sample in samples)
            {
                AccumulateMask(leftProbability, sample.Left.Mask);
                AccumulateMask(rightProbability, sample.Right.Mask);
            }
            Divide(leftProbability, samples.Count);
            Divide(rightProbability, samples.Count);

            var model = new ModelData
            {
                Width = NormalizedWidth,
                Height = NormalizedHeight,
                FeatureLength = samples[0].Feature.Length,
                Prototypes = samples.SelectMany(sample => sample.Feature).ToArray(),
                LeftShapeProbability = leftProbability,
                RightShapeProbability = rightProbability
            };

            double maximumShapeRatio = 0;
            double maximumTextureRatio = 0;
            foreach (SilverSample sample in samples)
            {
                maximumShapeRatio = Math.Max(maximumShapeRatio,
                    Math.Max(LargestShapeRatio(sample.Left, leftProbability, parameters),
                             LargestShapeRatio(sample.Right, rightProbability, parameters)));
                maximumTextureRatio = Math.Max(maximumTextureRatio,
                    Math.Max(LargestTextureRatio(sample.Left, parameters),
                             LargestTextureRatio(sample.Right, parameters)));
            }

            model.ShapeReviewRatio = Math.Max(0.001,
                maximumShapeRatio * parameters.AdaptiveCandidateSafetyFactor);
            model.ShapeNgRatio = Math.Max(parameters.AdaptiveShapeNgRatioMinimum,
                model.ShapeReviewRatio * parameters.AdaptiveNgMultiplier);
            model.TextureReviewRatio = Math.Max(0.001,
                maximumTextureRatio * parameters.AdaptiveCandidateSafetyFactor);
            model.TextureNgRatio = Math.Max(parameters.AdaptiveTextureNgRatioMinimum,
                model.TextureReviewRatio * parameters.AdaptiveNgMultiplier);

            double minimumBadDistance = double.PositiveInfinity;
            if (nonOkCalibrationFiles != null)
            {
                foreach (string file in nonOkCalibrationFiles.Where(File.Exists))
                {
                    SilverSample sample = ExtractFile(file, regionParameters, parameters);
                    minimumBadDistance = Math.Min(minimumBadDistance,
                        FindNearestDistance(sample.Feature, model.Prototypes, model.FeatureLength));
                }
            }
            model.OkDistanceThreshold = double.IsPositiveInfinity(minimumBadDistance)
                ? parameters.AdaptiveDefaultOkDistance
                : Math.Max(0.0001, minimumBadDistance * parameters.AdaptiveOkSafetyFactor);

            Save(modelPath, model);
            lock (ModelSync)
            {
                cachedModel = null;
                cachedPath = null;
                cachedWriteTimeUtc = DateTime.MinValue;
            }
        }

        private static SilverSample ExtractFile(
            string file,
            RegionParameters regionParameters,
            InspectionParameters parameters)
        {
            using (Mat image = OpenCvImageHelper.LoadImage(file))
            using (Mat bgr = OpenCvImageHelper.EnsureBgr8(image))
            using (Mat gray = new Mat())
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                ModuleRegionResult regions = ModuleRegionLocator.Locate(image, regionParameters);
                Rect left = FindRegion(regions, ModuleRegionType.LeftSilver);
                Rect right = FindRegion(regions, ModuleRegionType.RightSilver);
                return ExtractSample(gray, left, right, parameters);
            }
        }

        private static SilverSample ExtractSample(
            Mat gray,
            Rect leftSilver,
            Rect rightSilver,
            InspectionParameters parameters)
        {
            SilverSideSample left = ExtractSide(gray, leftSilver, parameters);
            SilverSideSample right = ExtractSide(gray, rightSilver, parameters);
            float[] feature = CombineAndNormalize(left.Hog, right.Hog);
            return new SilverSample { Left = left, Right = right, Feature = feature };
        }

        private static SilverSideSample ExtractSide(
            Mat gray,
            Rect rect,
            InspectionParameters parameters)
        {
            int minimumDimension = Math.Min(rect.Width, rect.Height);
            int closeSize = RatioKernel(minimumDimension, parameters.AdaptiveMaskCloseRatio);
            int openSize = RatioKernel(minimumDimension, parameters.AdaptiveMaskOpenRatio);
            int backgroundSize = RatioKernel(minimumDimension, parameters.AdaptiveBackgroundKernelRatio);
            int erodeSize = RatioKernel(minimumDimension, parameters.AdaptiveInteriorErodeRatio);

            using (Mat roi = new Mat(gray, rect))
            using (Mat blur = new Mat())
            using (Mat mask = new Mat())
            using (Mat normalized = new Mat())
            using (Mat enhanced = new Mat())
            using (Mat blackHat = new Mat())
            using (Mat core = new Mat())
            using (Mat closeKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(closeSize, closeSize)))
            using (Mat openKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(openSize, openSize)))
            using (Mat backgroundKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(backgroundSize, backgroundSize)))
            using (Mat erodeKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(erodeSize, erodeSize)))
            using (CLAHE clahe = Cv2.CreateCLAHE(2.0, new Size(8, 8)))
            using (var hog = new HOGDescriptor(
                new Size(NormalizedWidth, NormalizedHeight),
                new Size(16, 16),
                new Size(8, 8),
                new Size(8, 8),
                9))
            {
                Cv2.GaussianBlur(roi, blur, new Size(3, 3), 0);
                double otsu = Cv2.Threshold(blur, mask, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                double threshold = Math.Max(parameters.AdaptiveSilverThresholdMinimum,
                    Math.Min(parameters.AdaptiveSilverThresholdMaximum, otsu));
                Cv2.Threshold(blur, mask, threshold, 255, ThresholdTypes.Binary);
                Cv2.MorphologyEx(mask, mask, MorphTypes.Close, closeKernel);
                Cv2.MorphologyEx(mask, mask, MorphTypes.Open, openKernel);

                Cv2.Resize(blur, normalized, new Size(NormalizedWidth, NormalizedHeight), 0, 0, InterpolationFlags.Area);
                clahe.Apply(normalized, enhanced);
                float[] hogFeature = hog.Compute(enhanced);

                Cv2.MorphologyEx(blur, blackHat, MorphTypes.BlackHat, backgroundKernel);
                Cv2.Erode(mask, core, erodeKernel);
                double scale = Math.Max(20.0,
                    MaskPercentile(blur, mask, 0.75) - MaskPercentile(blur, mask, 0.25));

                using (Mat normalizedMask = new Mat())
                using (Mat normalizedBlackHat = new Mat())
                using (Mat blackHatFloat = new Mat())
                {
                    Cv2.Resize(mask, normalizedMask, new Size(NormalizedWidth, NormalizedHeight), 0, 0, InterpolationFlags.Nearest);
                    blackHat.ConvertTo(blackHatFloat, MatType.CV_32FC1, 1.0 / scale);
                    Cv2.Resize(blackHatFloat, normalizedBlackHat,
                        new Size(NormalizedWidth, NormalizedHeight), 0, 0, InterpolationFlags.Area);
                    using (Mat normalizedCore = new Mat())
                    {
                        Cv2.Resize(core, normalizedCore, new Size(NormalizedWidth, NormalizedHeight), 0, 0, InterpolationFlags.Nearest);
                        return new SilverSideSample
                        {
                            Mask = ToByteMask(normalizedMask),
                            Core = ToByteMask(normalizedCore),
                            BlackHat = ToFloatArray(normalizedBlackHat),
                            Hog = hogFeature
                        };
                    }
                }
            }
        }

        private static void AddReliableRegions(
            AdaptiveSilverEvaluation output,
            SilverSample sample,
            ModelData model,
            Rect leftRect,
            Rect rightRect,
            InspectionParameters parameters)
        {
            AddSideRegions(output, sample.Left, model.LeftShapeProbability, leftRect, "L", model, parameters);
            AddSideRegions(output, sample.Right, model.RightShapeProbability, rightRect, "R", model, parameters);
        }

        private static void AddSideRegions(
            AdaptiveSilverEvaluation output,
            SilverSideSample side,
            float[] probability,
            Rect silverRect,
            string name,
            ModelData model,
            InspectionParameters parameters)
        {
            Candidate shape = LargestShapeCandidate(side, probability, parameters);
            if (shape.Ratio >= model.ShapeReviewRatio)
            {
                bool isNg = shape.Ratio >= model.ShapeNgRatio;
                output.Regions.Add(new AdaptiveSilverRegion
                {
                    Box = MapBox(shape.Box, silverRect, parameters.AdaptiveBoxPaddingRatio),
                    Ratio = shape.Ratio,
                    Kind = name + " shape",
                    IsNg = isNg
                });
            }

            Candidate texture = LargestTextureCandidate(side, parameters);
            if (texture.Ratio >= model.TextureReviewRatio)
            {
                bool isNg = texture.Ratio >= model.TextureNgRatio;
                output.Regions.Add(new AdaptiveSilverRegion
                {
                    Box = MapBox(texture.Box, silverRect, parameters.AdaptiveBoxPaddingRatio),
                    Ratio = texture.Ratio,
                    Kind = name + " texture",
                    IsNg = isNg
                });
            }
        }

        private static double LargestShapeRatio(
            SilverSideSample side,
            float[] probability,
            InspectionParameters parameters) => LargestShapeCandidate(side, probability, parameters).Ratio;

        private static Candidate LargestShapeCandidate(
            SilverSideSample side,
            float[] probability,
            InspectionParameters parameters)
        {
            using (Mat missing = Mat.Zeros(NormalizedHeight, NormalizedWidth, MatType.CV_8UC1))
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse,
                new Size(RatioKernel(Math.Min(NormalizedWidth, NormalizedHeight), parameters.AdaptiveMaskOpenRatio),
                         RatioKernel(Math.Min(NormalizedWidth, NormalizedHeight), parameters.AdaptiveMaskOpenRatio))))
            {
                int index = 0;
                for (int y = 0; y < NormalizedHeight; y++)
                for (int x = 0; x < NormalizedWidth; x++, index++)
                {
                    if (probability[index] >= parameters.AdaptiveShapeCoreProbability && side.Mask[index] == 0)
                        missing.Set(y, x, (byte)255);
                }
                Cv2.MorphologyEx(missing, missing, MorphTypes.Open, kernel);
                return LargestCandidate(missing);
            }
        }

        private static double LargestTextureRatio(
            SilverSideSample side,
            InspectionParameters parameters) => LargestTextureCandidate(side, parameters).Ratio;

        private static Candidate LargestTextureCandidate(
            SilverSideSample side,
            InspectionParameters parameters)
        {
            var coreValues = new List<float>();
            for (int i = 0; i < side.BlackHat.Length; i++)
                if (side.Core[i] != 0)
                    coreValues.Add(side.BlackHat[i]);
            double threshold = Math.Max(parameters.AdaptiveTextureMinimumContrast,
                Percentile(coreValues, parameters.AdaptiveTexturePercentile));

            using (Mat candidate = Mat.Zeros(NormalizedHeight, NormalizedWidth, MatType.CV_8UC1))
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse,
                new Size(RatioKernel(Math.Min(NormalizedWidth, NormalizedHeight), parameters.AdaptiveMaskOpenRatio),
                         RatioKernel(Math.Min(NormalizedWidth, NormalizedHeight), parameters.AdaptiveMaskOpenRatio))))
            {
                int index = 0;
                for (int y = 0; y < NormalizedHeight; y++)
                for (int x = 0; x < NormalizedWidth; x++, index++)
                {
                    if (side.Core[index] != 0 && side.BlackHat[index] > threshold)
                        candidate.Set(y, x, (byte)255);
                }
                Cv2.MorphologyEx(candidate, candidate, MorphTypes.Close, kernel);
                return LargestCandidate(candidate);
            }
        }

        private static Candidate LargestCandidate(Mat mask)
        {
            using (Mat labels = new Mat())
            using (Mat stats = new Mat())
            using (Mat centroids = new Mat())
            {
                int count = Cv2.ConnectedComponentsWithStats(mask, labels, stats, centroids);
                int bestArea = 0;
                Rect bestBox = Rect.Empty;
                for (int i = 1; i < count; i++)
                {
                    int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
                    if (area <= bestArea) continue;
                    bestArea = area;
                    bestBox = new Rect(
                        stats.At<int>(i, (int)ConnectedComponentsTypes.Left),
                        stats.At<int>(i, (int)ConnectedComponentsTypes.Top),
                        stats.At<int>(i, (int)ConnectedComponentsTypes.Width),
                        stats.At<int>(i, (int)ConnectedComponentsTypes.Height));
                }
                return new Candidate
                {
                    Box = bestBox,
                    Ratio = bestArea / (double)(NormalizedWidth * NormalizedHeight)
                };
            }
        }

        private static Rect MapBox(Rect box, Rect target, double paddingRatio)
        {
            if (box.Width <= 0 || box.Height <= 0) return Rect.Empty;
            int x1 = target.X + (int)Math.Floor(box.X * target.Width / (double)NormalizedWidth);
            int y1 = target.Y + (int)Math.Floor(box.Y * target.Height / (double)NormalizedHeight);
            int x2 = target.X + (int)Math.Ceiling((box.X + box.Width) * target.Width / (double)NormalizedWidth);
            int y2 = target.Y + (int)Math.Ceiling((box.Y + box.Height) * target.Height / (double)NormalizedHeight);
            int padding = Math.Max(2, (int)Math.Round(Math.Min(target.Width, target.Height) * paddingRatio));
            x1 = Math.Max(target.X, x1 - padding);
            y1 = Math.Max(target.Y, y1 - padding);
            x2 = Math.Min(target.Right, x2 + padding);
            y2 = Math.Min(target.Bottom, y2 + padding);
            return new Rect(x1, y1, Math.Max(1, x2 - x1), Math.Max(1, y2 - y1));
        }

        private static double FindNearestDistance(float[] feature, float[] prototypes, int featureLength)
        {
            if (featureLength <= 0 || prototypes.Length < featureLength)
                return double.PositiveInfinity;
            double bestSimilarity = -1;
            int count = prototypes.Length / featureLength;
            for (int row = 0; row < count; row++)
            {
                double dot = 0;
                int offset = row * featureLength;
                for (int i = 0; i < featureLength; i++)
                    dot += feature[i] * prototypes[offset + i];
                if (dot > bestSimilarity) bestSimilarity = dot;
            }
            return Math.Max(0, 1.0 - bestSimilarity);
        }

        private static float[] CombineAndNormalize(float[] left, float[] right)
        {
            float[] result = new float[left.Length + right.Length];
            Array.Copy(left, 0, result, 0, left.Length);
            Array.Copy(right, 0, result, left.Length, right.Length);
            double sum = 0;
            for (int i = 0; i < result.Length; i++) sum += result[i] * result[i];
            double norm = Math.Sqrt(Math.Max(sum, 1e-12));
            for (int i = 0; i < result.Length; i++) result[i] = (float)(result[i] / norm);
            return result;
        }

        private static ModelData GetCachedModel(string path)
        {
            DateTime writeTime = File.GetLastWriteTimeUtc(path);
            lock (ModelSync)
            {
                if (cachedModel == null ||
                    !string.Equals(cachedPath, path, StringComparison.OrdinalIgnoreCase) ||
                    cachedWriteTimeUtc != writeTime)
                {
                    cachedModel = Load(path);
                    cachedPath = path;
                    cachedWriteTimeUtc = writeTime;
                }
                return cachedModel;
            }
        }

        private static void Save(string path, ModelData model)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write("ASNM");
                writer.Write(ModelVersion);
                writer.Write(model.Width);
                writer.Write(model.Height);
                writer.Write(model.FeatureLength);
                writer.Write(model.OkDistanceThreshold);
                writer.Write(model.ShapeReviewRatio);
                writer.Write(model.ShapeNgRatio);
                writer.Write(model.TextureReviewRatio);
                writer.Write(model.TextureNgRatio);
                WriteArray(writer, model.LeftShapeProbability);
                WriteArray(writer, model.RightShapeProbability);
                WriteArray(writer, model.Prototypes);
            }
        }

        private static ModelData Load(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadString() != "ASNM" || reader.ReadInt32() != ModelVersion)
                    throw new InvalidDataException("Unsupported silver normal model.");
                return new ModelData
                {
                    Width = reader.ReadInt32(),
                    Height = reader.ReadInt32(),
                    FeatureLength = reader.ReadInt32(),
                    OkDistanceThreshold = reader.ReadDouble(),
                    ShapeReviewRatio = reader.ReadDouble(),
                    ShapeNgRatio = reader.ReadDouble(),
                    TextureReviewRatio = reader.ReadDouble(),
                    TextureNgRatio = reader.ReadDouble(),
                    LeftShapeProbability = ReadArray(reader),
                    RightShapeProbability = ReadArray(reader),
                    Prototypes = ReadArray(reader)
                };
            }
        }

        private static IEnumerable<string> UniqueExistingFiles(IEnumerable<string> files)
        {
            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SHA256 sha = SHA256.Create())
            {
                foreach (string file in files.Where(File.Exists))
                {
                    string hash;
                    using (FileStream stream = File.OpenRead(file))
                        hash = Convert.ToBase64String(sha.ComputeHash(stream));
                    if (hashes.Add(hash)) yield return file;
                }
            }
        }

        private static byte[] ToByteMask(Mat mask)
        {
            var values = new byte[mask.Rows * mask.Cols];
            int index = 0;
            for (int y = 0; y < mask.Rows; y++)
            for (int x = 0; x < mask.Cols; x++, index++)
                values[index] = mask.At<byte>(y, x) == 0 ? (byte)0 : (byte)1;
            return values;
        }

        private static float[] ToFloatArray(Mat mat)
        {
            var values = new float[mat.Rows * mat.Cols];
            int index = 0;
            for (int y = 0; y < mat.Rows; y++)
            for (int x = 0; x < mat.Cols; x++, index++)
                values[index] = mat.At<float>(y, x);
            return values;
        }

        private static double MaskPercentile(Mat gray, Mat mask, double percentile)
        {
            var values = new List<byte>();
            for (int y = 0; y < gray.Rows; y++)
            for (int x = 0; x < gray.Cols; x++)
                if (mask.At<byte>(y, x) != 0)
                    values.Add(gray.At<byte>(y, x));
            if (values.Count == 0) return 0;
            values.Sort();
            int index = (int)Math.Round((values.Count - 1) * Math.Max(0, Math.Min(1, percentile)));
            return values[index];
        }

        private static double Percentile(List<float> values, double percentile)
        {
            if (values == null || values.Count == 0) return double.PositiveInfinity;
            values.Sort();
            int index = (int)Math.Round((values.Count - 1) * Math.Max(0, Math.Min(1, percentile)));
            return values[index];
        }

        private static int RatioKernel(int dimension, double ratio)
        {
            int value = Math.Max(3, Math.Min(dimension, (int)Math.Round(dimension * ratio)));
            if ((value & 1) == 0) value--;
            return Math.Max(3, value);
        }

        private static Rect FindRegion(ModuleRegionResult regions, ModuleRegionType type)
        {
            foreach (ModuleRegion region in regions.Regions)
                if (region.Type == type) return region.Box;
            throw new InvalidOperationException("region not found: " + type);
        }

        private static void AccumulateMask(float[] target, byte[] mask)
        {
            for (int i = 0; i < target.Length; i++) target[i] += mask[i];
        }

        private static void Divide(float[] values, int divisor)
        {
            for (int i = 0; i < values.Length; i++) values[i] /= divisor;
        }

        private static void WriteArray(BinaryWriter writer, float[] values)
        {
            writer.Write(values.Length);
            foreach (float value in values) writer.Write(value);
        }

        private static float[] ReadArray(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            var values = new float[length];
            for (int i = 0; i < length; i++) values[i] = reader.ReadSingle();
            return values;
        }

        private sealed class ModelData
        {
            public int Width;
            public int Height;
            public int FeatureLength;
            public double OkDistanceThreshold;
            public double ShapeReviewRatio;
            public double ShapeNgRatio;
            public double TextureReviewRatio;
            public double TextureNgRatio;
            public float[] LeftShapeProbability;
            public float[] RightShapeProbability;
            public float[] Prototypes;
        }

        private sealed class SilverSample
        {
            public SilverSideSample Left;
            public SilverSideSample Right;
            public float[] Feature;
        }

        private sealed class SilverSideSample
        {
            public byte[] Mask;
            public byte[] Core;
            public float[] BlackHat;
            public float[] Hog;
        }

        private sealed class Candidate
        {
            public Rect Box;
            public double Ratio;
        }
    }
}
