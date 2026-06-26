using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WindowsFormsApp1
{
    public sealed class DeimOnnxDetector : IDisposable
    {
        private const int InputSize = 640;
        private readonly InferenceSession session;
        private readonly float confidenceThreshold;
        private readonly HashSet<int> defectClassIds;

        private static readonly string[] CocoClassNames =
        {
            "person","bicycle","car","motorcycle","airplane","bus","train","truck","boat","traffic light",
            "fire hydrant","stop sign","parking meter","bench","bird","cat","dog","horse","sheep","cow",
            "elephant","bear","zebra","giraffe","backpack","umbrella","handbag","tie","suitcase","frisbee",
            "skis","snowboard","sports ball","kite","baseball bat","baseball glove","skateboard","surfboard","tennis racket","bottle",
            "wine glass","cup","fork","knife","spoon","bowl","banana","apple","sandwich","orange",
            "broccoli","carrot","hot dog","pizza","donut","cake","chair","couch","potted plant","bed",
            "dining table","toilet","tv","laptop","mouse","remote","keyboard","cell phone","microwave","oven",
            "toaster","sink","refrigerator","book","clock","vase","scissors","teddy bear","hair drier","toothbrush"
        };

        public DeimOnnxDetector(string modelPath, float confidenceThreshold = 0.5f,
            IEnumerable<int> defectClassIds = null)
        {
            if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
                throw new FileNotFoundException("DEIM ONNX模型不存在。", modelPath);

            this.confidenceThreshold = confidenceThreshold;
            this.defectClassIds = defectClassIds == null
                ? new HashSet<int>()
                : new HashSet<int>(defectClassIds);

            var options = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_SEQUENTIAL
            };
            session = new InferenceSession(modelPath, options);
            ValidateModelSignature();
        }

        public DetectionResult Detect(Mat source)
        {
            if (source == null || source.Empty())
                throw new ArgumentException("检测图片为空。", nameof(source));

            var timer = Stopwatch.StartNew();
            using (var bgr = OpenCvImageHelper.EnsureBgr8(source))
            using (var letterboxed = CreateLetterbox(bgr, out float ratio, out int padX, out int padY))
            using (var rgb = new Mat())
            {
                Cv2.CvtColor(letterboxed, rgb, ColorConversionCodes.BGR2RGB);
                DenseTensor<float> imageTensor = CreateImageTensor(rgb);
                var sizeTensor = new DenseTensor<long>(new[] { 1, 2 });
                sizeTensor[0, 0] = InputSize;
                sizeTensor[0, 1] = InputSize;

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("images", imageTensor),
                    NamedOnnxValue.CreateFromTensor("orig_target_sizes", sizeTensor)
                };

                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = session.Run(inputs))
                {
                    Tensor<long> labels = outputs.First(x => x.Name == "labels").AsTensor<long>();
                    Tensor<float> boxes = outputs.First(x => x.Name == "boxes").AsTensor<float>();
                    Tensor<float> scores = outputs.First(x => x.Name == "scores").AsTensor<float>();
                    var result = new DetectionResult { AnnotatedImage = bgr.Clone() };

                    for (int i = 0; i < labels.Dimensions[1]; i++)
                    {
                        float score = scores[0, i];
                        if (score < confidenceThreshold)
                            continue;

                        int classId = checked((int)labels[0, i]);
                        Rect rectangle = MapBoxToOriginal(
                            boxes[0, i, 0], boxes[0, i, 1], boxes[0, i, 2], boxes[0, i, 3],
                            ratio, padX, padY, bgr.Width, bgr.Height);
                        if (rectangle.Width <= 0 || rectangle.Height <= 0)
                            continue;

                        result.Detections.Add(new DetectionBox
                        {
                            ClassId = classId,
                            ClassName = GetClassName(classId),
                            Confidence = score,
                            BoundingBox = rectangle
                        });
                    }

                    result.IsOk = defectClassIds.Count == 0
                        ? result.Detections.Count == 0
                        : result.Detections.All(x => !defectClassIds.Contains(x.ClassId));
                    DrawResult(result);
                    timer.Stop();
                    result.ElapsedMilliseconds = timer.ElapsedMilliseconds;
                    return result;
                }
            }
        }

        private void ValidateModelSignature()
        {
            string[] requiredInputs = { "images", "orig_target_sizes" };
            string[] requiredOutputs = { "labels", "boxes", "scores" };
            if (requiredInputs.Any(x => !session.InputMetadata.ContainsKey(x)) ||
                requiredOutputs.Any(x => !session.OutputMetadata.ContainsKey(x)))
                throw new InvalidDataException("ONNX模型输入输出与DEIM导出格式不一致。");
        }

        private static Mat CreateLetterbox(Mat source, out float ratio, out int padX, out int padY)
        {
            ratio = Math.Min((float)InputSize / source.Width, (float)InputSize / source.Height);
            int width = Math.Max(1, (int)(source.Width * ratio));
            int height = Math.Max(1, (int)(source.Height * ratio));
            padX = (InputSize - width) / 2;
            padY = (InputSize - height) / 2;

            var canvas = new Mat(InputSize, InputSize, MatType.CV_8UC3, Scalar.Black);
            using (var resized = new Mat())
            {
                Cv2.Resize(source, resized, new OpenCvSharp.Size(width, height));
                using (var roi = new Mat(canvas, new Rect(padX, padY, width, height)))
                    resized.CopyTo(roi);
            }
            return canvas;
        }

        private static DenseTensor<float> CreateImageTensor(Mat rgb)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 3, InputSize, InputSize });
            for (int y = 0; y < InputSize; y++)
            for (int x = 0; x < InputSize; x++)
            {
                Vec3b pixel = rgb.At<Vec3b>(y, x);
                tensor[0, 0, y, x] = pixel.Item0 / 255f;
                tensor[0, 1, y, x] = pixel.Item1 / 255f;
                tensor[0, 2, y, x] = pixel.Item2 / 255f;
            }
            return tensor;
        }

        private static Rect MapBoxToOriginal(float x1, float y1, float x2, float y2,
            float ratio, int padX, int padY, int sourceWidth, int sourceHeight)
        {
            int left = Clamp((int)Math.Round((x1 - padX) / ratio), 0, sourceWidth - 1);
            int top = Clamp((int)Math.Round((y1 - padY) / ratio), 0, sourceHeight - 1);
            int right = Clamp((int)Math.Round((x2 - padX) / ratio), 0, sourceWidth);
            int bottom = Clamp((int)Math.Round((y2 - padY) / ratio), 0, sourceHeight);
            return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }

        private static void DrawResult(DetectionResult result)
        {
            Scalar color = result.IsOk ? Scalar.LimeGreen : Scalar.Red;
            foreach (DetectionBox detection in result.Detections)
            {
                Cv2.Rectangle(result.AnnotatedImage, detection.BoundingBox, color, 2);
                Cv2.PutText(result.AnnotatedImage,
                    $"{detection.ClassName} {detection.Confidence:0.00}",
                    new Point(detection.BoundingBox.X, Math.Max(20, detection.BoundingBox.Y)),
                    HersheyFonts.HersheySimplex, 0.6, color, 2);
            }
            Cv2.PutText(result.AnnotatedImage, result.IsOk ? "OK" : "NG",
                new Point(20, 50), HersheyFonts.HersheySimplex, 1.5, color, 3);
        }

        private static string GetClassName(int classId)
        {
            return classId >= 0 && classId < CocoClassNames.Length
                ? CocoClassNames[classId] : "class_" + classId;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Min(maximum, Math.Max(minimum, value));
        }

        public void Dispose() { session.Dispose(); }
    }
}
