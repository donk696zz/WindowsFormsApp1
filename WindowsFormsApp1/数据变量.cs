using System.Collections.Generic;

namespace WindowsFormsApp1
{
    public static class 账号密码
    {
        public static string 账号 { get; set; }
        public static string 密码 { get; set; }
    }

    public static class 数据变量
    {
        public static string 检测相机曝光时间 { get; set; } = "10000";
        public static string 检测相机增益 { get; set; } = "0";
        public static string 分类相机曝光时间 { get; set; } = "10000";
        public static string 分类相机增益 { get; set; } = "0";

        public static string 检测相机原图保存路径 { get; set; } = string.Empty;
        public static string 检测相机OK图保存路径 { get; set; } = string.Empty;
        public static string 检测相机NG图保存路径 { get; set; } = string.Empty;
        public static string 分类相机原图保存路径 { get; set; } = string.Empty;
        public static string 分类相机OK图保存路径 { get; set; } = string.Empty;
        public static string 分类相机NG图保存路径 { get; set; } = string.Empty;

        public static string 料号名称 { get; set; } = string.Empty;
        public static string 权限时间 { get; set; } = "10";
        public static string 图片删除日期 { get; set; } = "30";

        public static void 从参数模型同步()
        {
            ApplicationParameters parameters = VisionParameterStore.ApplicationParameters;
            检测相机曝光时间 = parameters.DetectionCamera.Exposure.ToString();
            检测相机增益 = parameters.DetectionCamera.Gain.ToString();
            分类相机曝光时间 = parameters.ClassificationCamera.Exposure.ToString();
            分类相机增益 = parameters.ClassificationCamera.Gain.ToString();
            检测相机原图保存路径 = parameters.DetectionCamera.RawImagePath;
            检测相机OK图保存路径 = parameters.DetectionCamera.OkImagePath;
            检测相机NG图保存路径 = parameters.DetectionCamera.NgImagePath;
            分类相机原图保存路径 = parameters.ClassificationCamera.RawImagePath;
            分类相机OK图保存路径 = parameters.ClassificationCamera.OkImagePath;
            分类相机NG图保存路径 = parameters.ClassificationCamera.NgImagePath;
            权限时间 = parameters.PermissionTimeoutMinutes.ToString();
            图片删除日期 = parameters.ImageRetentionDays.ToString();
        }
    }

    public static class 状态
    {
        public static bool 登录权限;
        public static bool 选择区域时halcon不可动 = true;
        public static bool 控制放大缩小后图片的显示;
        public static bool 自动状态;
        public static bool 实时状态;
    }

    public static class 相机变量
    {
        // 固定顺序：0=检测相机，1=分类相机。业务代码只使用角色名称。
        public static List<MVS变量> CameraList = new List<MVS变量>();
        public static List<string> 料号集合 = new List<string>();
    }
}
