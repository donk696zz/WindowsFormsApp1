using OpenCvSharp;
using System.Collections.Generic;

namespace WindowsFormsApp1
{
    public sealed class DetectionBox
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public float Confidence { get; set; }
        public Rect BoundingBox { get; set; }
    }

    public sealed class DetectionResult
    {
        public DetectionResult() { Detections = new List<DetectionBox>(); }
        public List<DetectionBox> Detections { get; }
        public bool IsOk { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public Mat AnnotatedImage { get; set; }
    }
}
