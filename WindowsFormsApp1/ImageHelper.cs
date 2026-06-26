using System;
using System.Drawing;
using OpenCvSharp;

namespace WindowsFormsApp1
{
    /// <summary>
    /// 图像处理工具类
    /// 用于图像转换、预处理、保存等操作
    /// 替代 HALCON 的图像处理功能
    /// </summary>
    public class ImageHelper
    {
        /// <summary>
        /// 将 IntPtr 相机数据转换为 OpenCV Mat
        /// </summary>
        /// <param name="imageData">原始图像指针</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="channels">通道数（1=灰度，3=BGR）</param>
        /// <returns>OpenCV Mat 对象</returns>
        public static Mat ConvertPtrToMat(IntPtr imageData, int width, int height, int channels = 1)
        {
            try
            {
                if (imageData == IntPtr.Zero || width <= 0 || height <= 0)
                {
                    throw new ArgumentException("无效的图像参数");
                }

                // 根据通道数选择图像类型
                MatType matType = channels == 1 ? MatType.CV_8UC1 : MatType.CV_8UC3;

                // 创建 Mat 对象（不复制数据，直接指向内存）
                Mat mat = new Mat(height, width, matType, imageData);

                // 重要：创建一个独立副本，避免指针失效
                Mat result = mat.Clone();
                mat.Dispose();

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"图像转换失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将 Mat 转换为 Bitmap（用于显示）
        /// </summary>
        /// <param name="mat">OpenCV Mat 对象</param>
        /// <returns>Bitmap 对象</returns>
        public static Bitmap ConvertMatToBitmap(Mat mat)
        {
            try
            {
                if (mat == null || mat.Empty())
                {
                    return null;
                }

                // 确保是 8UC3 格式
                Mat displayMat = mat;
                if (mat.Channels() == 1)
                {
                    displayMat = new Mat();
                    Cv2.CvtColor(mat, displayMat, ColorConversionCodes.GRAY2BGR);
                }
                else if (mat.Type() != MatType.CV_8UC3)
                {
                    displayMat = new Mat();
                    mat.ConvertTo(displayMat, MatType.CV_8UC3);
                }

                // 创建 Bitmap
                Bitmap bitmap = new Bitmap(displayMat.Cols, displayMat.Rows, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                // 复制数据
                byte[] data = new byte[displayMat.Total() * displayMat.ElemSize()];
                System.Runtime.InteropServices.Marshal.Copy(displayMat.Data, data, 0, data.Length);

                System.Drawing.Imaging.BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, displayMat.Cols, displayMat.Rows),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                System.Runtime.InteropServices.Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
                bitmap.UnlockBits(bmpData);

                if (displayMat != mat)
                {
                    displayMat.Dispose();
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bitmap 转换失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 调整图像大小
        /// </summary>
        /// <param name="mat">源图像</param>
        /// <param name="width">目标宽度</param>
        /// <param name="height">目标高度</param>
        /// <returns>调整后的图像</returns>
        public static Mat ResizeImage(Mat mat, int width, int height)
        {
            try
            {
                if (mat == null || mat.Empty())
                {
                    return null;
                }

                Mat result = new Mat();
                Cv2.Resize(mat, result, new OpenCvSharp.Size(width, height));
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"图像缩放失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 图像预处理（归一化 + 通道转换）
        /// </summary>
        /// <param name="mat">源图像</param>
        /// <param name="normalize">是否归一化（除以 255）</param>
        /// <returns>预处理后的浮点数组</returns>
        public static float[] PreprocessImage(Mat mat, bool normalize = true)
        {
            try
            {
                if (mat == null || mat.Empty())
                {
                    return null;
                }

                // 确保是 8 位 3 通道图像
                Mat processedMat = mat;
                if (mat.Channels() == 1)
                {
                    // 灰度图转 BGR
                    processedMat = new Mat();
                    Cv2.CvtColor(mat, processedMat, ColorConversionCodes.GRAY2BGR);
                }
                else if (mat.Type() != MatType.CV_8UC3)
                {
                    // 转换为 8 位 3 通道
                    processedMat = new Mat();
                    mat.ConvertTo(processedMat, MatType.CV_8UC3);
                }

                // 转换为浮点数组
                float[] result = MatToFloatArray(processedMat, normalize);

                if (processedMat != mat)
                {
                    processedMat.Dispose();
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"图像预处理失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将 Mat 转换为浮点数组
        /// </summary>
        /// <param name="mat">源 Mat</param>
        /// <param name="normalize">是否归一化</param>
        /// <returns>浮点数组</returns>
        private static float[] MatToFloatArray(Mat mat, bool normalize = true)
        {
            try
            {
                if (mat == null || mat.Empty())
                {
                    return null;
                }

                int size = mat.Rows * mat.Cols * mat.Channels();
                float[] result = new float[size];

                // 直接从 Mat 的数据中读取
                byte[] data = new byte[mat.Total() * mat.ElemSize()];
                System.Runtime.InteropServices.Marshal.Copy(mat.Data, data, 0, data.Length);

                for (int i = 0; i < data.Length; i++)
                {
                    result[i] = normalize ? data[i] / 255.0f : data[i];
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mat 转数组失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存图像到文件
        /// </summary>
        /// <param name="mat">源图像</param>
        /// <param name="filePath">保存路径</param>
        /// <returns>保存成功返回 true</returns>
        public static bool SaveImage(Mat mat, string filePath)
        {
            try
            {
                if (mat == null || mat.Empty())
                {
                    return false;
                }

                // 创建目录（如果不存在）
                string directory = System.IO.Path.GetDirectoryName(filePath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // 保存图像
                return Cv2.ImWrite(filePath, mat);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存图像失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从文件读取图像
        /// </summary>
        /// <param name="filePath">图像文件路径</param>
        /// <param name="flags">读取标志（IMREAD_COLOR 等）</param>
        /// <returns>读取的 Mat 对象</returns>
        public static Mat LoadImage(string filePath, ImreadModes flags = ImreadModes.Color)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    throw new System.IO.FileNotFoundException($"图像文件不存在: {filePath}");
                }

                return Cv2.ImRead(filePath, flags);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取图像失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建批量输入张量（用于 ONNX 推理）
        /// </summary>
        /// <param name="mat">源图像</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <returns>符合模型输入的浮点数组</returns>
        public static float[] CreateModelInput(Mat mat, int targetWidth, int targetHeight)
        {
            try
            {
                // 调整大小
                Mat resized = ResizeImage(mat, targetWidth, targetHeight);
                if (resized == null)
                {
                    return null;
                }

                // 预处理（归一化）
                float[] data = PreprocessImage(resized, true);
                resized.Dispose();

                if (data == null)
                {
                    return null;
                }

                // 转换为 CHW 格式（如果需要）
                // 假设模型输入是 [1, 3, H, W] 格式
                int channels = 3;
                float[] result = new float[1 * channels * targetHeight * targetWidth];

                // HWC -> CHW 转换
                int index = 0;
                for (int c = 0; c < channels; c++)
                {
                    for (int h = 0; h < targetHeight; h++)
                    {
                        for (int w = 0; w < targetWidth; w++)
                        {
                            result[index++] = data[(h * targetWidth + w) * channels + c];
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建模型输入失败: {ex.Message}");
                return null;
            }
        }
    }
}
