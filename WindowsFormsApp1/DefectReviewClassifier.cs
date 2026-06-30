using OpenCvSharp;
using OpenCvSharp.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WindowsFormsApp1
{
    /// <summary>
    /// 轻量级整图复检门控。HOG/SVM只决定是否需要复检，具体缺陷框仍由
    /// 暗孔、边缘缺银和线状缺陷三条可解释规则生成。
    /// </summary>
    public static class DefectReviewClassifier
    {
        private static readonly OpenCvSharp.Size WindowSize = new OpenCvSharp.Size(128, 64);
        private static readonly object ModelSync = new object();
        private static SVM cachedModel;
        private static string cachedModelPath;
        private static DateTime cachedModelWriteTimeUtc;

        public static string RuntimeModelPath => Path.Combine(
            Path.GetDirectoryName(typeof(DefectReviewClassifier).Assembly.Location),
            "检测模型",
            "defect_review_svm.xml");

        public static float[] ExtractFeatures(Mat source)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("source image is empty.", nameof(source));

            ModuleRegionResult located = ModuleRegionLocator.Locate(source, new RegionParameters());
            Rect module = located.ModuleBox;
            using (Mat bgr = OpenCvImageHelper.EnsureBgr8(source))
            using (Mat crop = new Mat(bgr, module))
            using (Mat gray = new Mat())
            using (Mat resized = new Mat())
            using (var hog = new HOGDescriptor(
                WindowSize,
                new OpenCvSharp.Size(16, 16),
                new OpenCvSharp.Size(8, 8),
                new OpenCvSharp.Size(8, 8),
                9))
            {
                Cv2.CvtColor(crop, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.Resize(gray, resized, WindowSize, 0, 0, InterpolationFlags.Area);
                return hog.Compute(resized);
            }
        }

        public static void Train(
            IEnumerable<string> okFiles,
            IEnumerable<string> reviewFiles,
            string modelPath,
            SVM.KernelTypes kernel,
            double c,
            double gamma,
            int positiveReplication = 8)
        {
            List<string> ok = okFiles.Where(File.Exists).ToList();
            List<string> review = reviewFiles.Where(File.Exists).ToList();
            if (ok.Count == 0 || review.Count == 0)
                throw new InvalidOperationException("SVM training requires both OK and review images.");

            var rows = new List<float[]>();
            var labels = new List<int>();
            foreach (string file in ok)
            {
                rows.Add(ExtractFile(file));
                labels.Add(-1);
            }
            int replication = Math.Max(1, positiveReplication);
            foreach (string file in review)
            {
                float[] feature = ExtractFile(file);
                for (int i = 0; i < replication; i++)
                {
                    rows.Add(feature);
                    labels.Add(1);
                }
            }

            using (Mat samples = ToSampleMat(rows))
            using (Mat responses = new Mat(labels.Count, 1, MatType.CV_32SC1))
            using (SVM svm = SVM.Create())
            {
                for (int row = 0; row < labels.Count; row++)
                    responses.Set(row, 0, labels[row]);

                svm.Type = SVM.Types.CSvc;
                svm.KernelType = kernel;
                svm.C = c;
                if (kernel == SVM.KernelTypes.Rbf ||
                    kernel == SVM.KernelTypes.Poly ||
                    kernel == SVM.KernelTypes.Sigmoid)
                    svm.Gamma = gamma;
                svm.TermCriteria = new TermCriteria(CriteriaTypes.MaxIter | CriteriaTypes.Eps, 2000, 1e-6);
                if (!svm.Train(samples, SampleTypes.RowSample, responses))
                    throw new InvalidOperationException("SVM training failed.");

                string directory = Path.GetDirectoryName(modelPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);
                svm.Save(modelPath);
            }
        }

        public static int Predict(string modelPath, string imagePath)
        {
            using (Mat image = OpenCvImageHelper.LoadImage(imagePath))
                return Predict(modelPath, image);
        }

        public static int Predict(string modelPath, Mat image)
        {
            float[] feature = ExtractFeatures(image);
            using (Mat row = ToSampleMat(new[] { feature }))
            using (SVM svm = SVM.Load(modelPath))
                return svm.Predict(row) > 0 ? 1 : -1;
        }

        public static int[] PredictFiles(string modelPath, IEnumerable<string> imagePaths)
        {
            List<string> files = imagePaths.Where(File.Exists).ToList();
            var predictions = new int[files.Count];
            using (SVM svm = SVM.Load(modelPath))
            {
                for (int i = 0; i < files.Count; i++)
                {
                    float[] feature = ExtractFile(files[i]);
                    using (Mat row = ToSampleMat(new[] { feature }))
                        predictions[i] = svm.Predict(row) > 0 ? 1 : -1;
                }
            }
            return predictions;
        }

        public static bool TryPredictRuntime(Mat image, out bool requiresReview)
        {
            requiresReview = false;
            string path = RuntimeModelPath;
            if (!File.Exists(path))
                return false;

            try
            {
                float[] feature = ExtractFeatures(image);
                using (Mat row = ToSampleMat(new[] { feature }))
                {
                    lock (ModelSync)
                    {
                        SVM model = GetCachedModel(path);
                        requiresReview = model.Predict(row) > 0;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static SVM GetCachedModel(string path)
        {
            DateTime writeTime = File.GetLastWriteTimeUtc(path);
            lock (ModelSync)
            {
                if (cachedModel == null ||
                    !string.Equals(cachedModelPath, path, StringComparison.OrdinalIgnoreCase) ||
                    cachedModelWriteTimeUtc != writeTime)
                {
                    cachedModel?.Dispose();
                    cachedModel = SVM.Load(path);
                    cachedModelPath = path;
                    cachedModelWriteTimeUtc = writeTime;
                }
                return cachedModel;
            }
        }

        private static float[] ExtractFile(string file)
        {
            using (Mat image = OpenCvImageHelper.LoadImage(file))
                return ExtractFeatures(image);
        }

        private static Mat ToSampleMat(IList<float[]> rows)
        {
            if (rows == null || rows.Count == 0)
                throw new ArgumentException("feature rows are empty.", nameof(rows));
            int columns = rows[0].Length;
            var samples = new Mat(rows.Count, columns, MatType.CV_32FC1);
            for (int row = 0; row < rows.Count; row++)
            {
                if (rows[row].Length != columns)
                    throw new InvalidOperationException("feature length mismatch.");
                for (int column = 0; column < columns; column++)
                    samples.Set(row, column, rows[row][column]);
            }
            return samples;
        }
    }
}
