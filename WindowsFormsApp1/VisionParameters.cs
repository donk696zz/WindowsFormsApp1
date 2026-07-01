using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WindowsFormsApp1
{
    [Serializable]
    public sealed class RegionParameters
    {
        public int ModuleDarkThreshold { get; set; } = 155;
        public double ColumnActivationRatio { get; set; } = 0.025;
        public int ColumnActivationMinimum { get; set; } = 8;
        public double RowActivationRatio { get; set; } = 0.04;
        public int RowActivationMinimum { get; set; } = 20;
        public int ColumnSmoothRadius { get; set; } = 8;
        public int RowSmoothRadius { get; set; } = 5;
        public int ColumnSupportRadius { get; set; } = 8;
        public int ColumnMinimumSupport { get; set; } = 5;
        public int RowSupportRadius { get; set; } = 6;
        public int RowMinimumSupport { get; set; } = 4;

        public double SilverOuterEdgeRatio { get; set; } = 0.045;
        public double SilverInnerEdgeRatio { get; set; } = 0.335;
        public double MiddleWidthRatio { get; set; } = 0.27;
        public double SilverTopRatio { get; set; } = 0.07;
        public double SilverBottomRatio { get; set; } = 0.06;
        public double TopInspectHeightRatio { get; set; } = 0.052;
        public double BottomInspectHeightRatio { get; set; } = 0.042;
        public double SideInspectWidthRatio { get; set; } = 0.035;
        public double MiddleInspectHorizontalInsetRatio { get; set; } = 0.08;
        public double SilverVerticalSplitRatio { get; set; } = 0.50;
        public double SilverInnerHorizontalInsetRatio { get; set; } = 0.08;
        public double SilverInnerVerticalInsetRatio { get; set; } = 0.06;
        public double EdgeInspectTrimRatio { get; set; } = 0.18;

        public RegionParameters Clone()
        {
            return (RegionParameters)MemberwiseClone();
        }

        public void Validate()
        {
            ModuleDarkThreshold = Clamp(ModuleDarkThreshold, 0, 255);
            ColumnActivationRatio = Clamp(ColumnActivationRatio, 0.001, 0.50);
            RowActivationRatio = Clamp(RowActivationRatio, 0.001, 0.50);
            ColumnActivationMinimum = Math.Max(1, ColumnActivationMinimum);
            RowActivationMinimum = Math.Max(1, RowActivationMinimum);
            ColumnSmoothRadius = Math.Max(0, ColumnSmoothRadius);
            RowSmoothRadius = Math.Max(0, RowSmoothRadius);
            ColumnSupportRadius = Math.Max(0, ColumnSupportRadius);
            RowSupportRadius = Math.Max(0, RowSupportRadius);
            ColumnMinimumSupport = Math.Max(1, ColumnMinimumSupport);
            RowMinimumSupport = Math.Max(1, RowMinimumSupport);

            SilverOuterEdgeRatio = Clamp(SilverOuterEdgeRatio, 0, 0.45);
            SilverInnerEdgeRatio = Clamp(SilverInnerEdgeRatio, SilverOuterEdgeRatio + 0.001, 0.49);
            MiddleWidthRatio = Clamp(MiddleWidthRatio, 0.01, 0.90);
            SilverTopRatio = Clamp(SilverTopRatio, 0, 0.45);
            SilverBottomRatio = Clamp(SilverBottomRatio, 0, 0.45);
            TopInspectHeightRatio = Clamp(TopInspectHeightRatio, 0.001, 0.45);
            BottomInspectHeightRatio = Clamp(BottomInspectHeightRatio, 0.001, 0.45);
            SideInspectWidthRatio = Clamp(SideInspectWidthRatio, 0.001, 0.45);
            MiddleInspectHorizontalInsetRatio = Clamp(MiddleInspectHorizontalInsetRatio, 0, 0.45);
            SilverVerticalSplitRatio = Clamp(SilverVerticalSplitRatio, 0.10, 0.90);
            SilverInnerHorizontalInsetRatio = Clamp(SilverInnerHorizontalInsetRatio, 0, 0.45);
            SilverInnerVerticalInsetRatio = Clamp(SilverInnerVerticalInsetRatio, 0, 0.45);
            EdgeInspectTrimRatio = Clamp(EdgeInspectTrimRatio, 0, 0.45);
        }

        private static int Clamp(int value, int minimum, int maximum) => Math.Max(minimum, Math.Min(maximum, value));
        private static double Clamp(double value, double minimum, double maximum) => Math.Max(minimum, Math.Min(maximum, value));
    }

    [Serializable]
    public sealed class InspectionParameters
    {
        // 银面上下半区覆盖率
        public double SilverCoverageNgRatio { get; set; } = 0.70;
        public double SilverCoverageOkRatio { get; set; } = 0.78;

        // 中间框检测
        public double MiddleSilverReviewRatio { get; set; } = 0.0020;
        public double MiddleSilverNgRatio { get; set; } = 0.0050;
        public int MiddleSilverThresholdOffset { get; set; } = 55;
        public int MiddleSilverThresholdMinimum { get; set; } = 145;
        public int MiddleSilverThresholdMaximum { get; set; } = 190;

        // 边缘框与画面完整性
        public int ModuleVisibleMargin { get; set; } = 2;
        public double ModuleBorderDarkRatio { get; set; } = 0.50;

        // 银面掩码：先闭运算，再开运算
        public double AdaptiveMaskCloseRatio { get; set; } = 0.012;
        public double AdaptiveMaskOpenRatio { get; set; } = 0.012;
        public double AdaptiveRoundedCornerRatio { get; set; } = 0.15;

        // 银面内部缺陷
        public int DefectMorphKernelSize { get; set; } = 3;
        public int InnerDefectReviewArea { get; set; } = 60;
        public int MinimumDefectArea { get; set; } = 80;

        // 银面边缘凹陷
        public int EdgeConcavityReviewArea { get; set; } = 1000;
        public int EdgeConcavityNgArea { get; set; } = 2000;
        public double EdgeConcavityReviewDepth { get; set; } = 15.0;
        public double EdgeConcavityNgDepth { get; set; } = 20.0;
        public double EdgeConcavityShapeScale { get; set; } = 12.0;
        public double EdgeConcavityMinimumThickness { get; set; } = 3.0;

        public InspectionParameters Clone() =>
            (InspectionParameters)MemberwiseClone();

        public void Validate()
        {
            SilverCoverageNgRatio = Ratio(SilverCoverageNgRatio);
            SilverCoverageOkRatio = Math.Max(
                SilverCoverageNgRatio, Ratio(SilverCoverageOkRatio));

            MiddleSilverReviewRatio = Ratio(MiddleSilverReviewRatio);
            MiddleSilverNgRatio = Math.Max(
                MiddleSilverReviewRatio, Ratio(MiddleSilverNgRatio));
            MiddleSilverThresholdOffset = Gray(MiddleSilverThresholdOffset);
            MiddleSilverThresholdMinimum = Gray(MiddleSilverThresholdMinimum);
            MiddleSilverThresholdMaximum = Math.Max(
                MiddleSilverThresholdMinimum,
                Gray(MiddleSilverThresholdMaximum));

            ModuleVisibleMargin = Math.Max(0, ModuleVisibleMargin);
            ModuleBorderDarkRatio = Ratio(ModuleBorderDarkRatio);

            AdaptiveMaskCloseRatio = Clamp(AdaptiveMaskCloseRatio, 0.005, 0.25);
            AdaptiveMaskOpenRatio = Clamp(AdaptiveMaskOpenRatio, 0.005, 0.25);
            AdaptiveRoundedCornerRatio = Clamp(
                AdaptiveRoundedCornerRatio, 0.02, 0.50);

            DefectMorphKernelSize = OddAtLeastThree(DefectMorphKernelSize);
            InnerDefectReviewArea = Math.Max(1, InnerDefectReviewArea);
            MinimumDefectArea = Math.Max(
                InnerDefectReviewArea, MinimumDefectArea);

            EdgeConcavityReviewArea = Math.Max(1, EdgeConcavityReviewArea);
            EdgeConcavityNgArea = Math.Max(
                EdgeConcavityReviewArea, EdgeConcavityNgArea);
            EdgeConcavityReviewDepth = Math.Max(0.1, EdgeConcavityReviewDepth);
            EdgeConcavityNgDepth = Math.Max(
                EdgeConcavityReviewDepth, EdgeConcavityNgDepth);
            EdgeConcavityShapeScale = Clamp(
                EdgeConcavityShapeScale, 0.1, 100.0);
            EdgeConcavityMinimumThickness = Clamp(
                EdgeConcavityMinimumThickness, 0.1, 100.0);
        }

        private static int Gray(int value) =>
            Math.Max(0, Math.Min(255, value));

        private static double Ratio(double value) =>
            Math.Max(0, Math.Min(1, value));

        private static double Clamp(double value, double minimum, double maximum) =>
            Math.Max(minimum, Math.Min(maximum, value));

        private static int OddAtLeastThree(int value)
        {
            value = Math.Max(3, value);
            return value % 2 == 0 ? value + 1 : value;
        }
    }

    [Serializable]
    public sealed class CameraRoleParameters
    {
        public double Exposure { get; set; } = 10000;
        public double Gain { get; set; }
        public string RawImagePath { get; set; } = string.Empty;
        public string OkImagePath { get; set; } = string.Empty;
        public string NgImagePath { get; set; } = string.Empty;

        public CameraRoleParameters Clone() => (CameraRoleParameters)MemberwiseClone();
    }

    [Serializable]
    public sealed class ApplicationParameters
    {
        public InspectionParameters Inspection { get; set; } = new InspectionParameters();
        public CameraRoleParameters DetectionCamera { get; set; } = new CameraRoleParameters();
        public CameraRoleParameters ClassificationCamera { get; set; } = new CameraRoleParameters();
        public int PermissionTimeoutMinutes { get; set; } = 10;
        public int ImageRetentionDays { get; set; } = 30;

        public ApplicationParameters Clone()
        {
            return new ApplicationParameters
            {
                Inspection = (Inspection ?? new InspectionParameters()).Clone(),
                DetectionCamera = (DetectionCamera ?? new CameraRoleParameters()).Clone(),
                ClassificationCamera = (ClassificationCamera ?? new CameraRoleParameters()).Clone(),
                PermissionTimeoutMinutes = PermissionTimeoutMinutes,
                ImageRetentionDays = ImageRetentionDays
            };
        }
    }

    [Serializable]
    public sealed class MaterialRegionProfile
    {
        public string MaterialNumber { get; set; } = string.Empty;
        public string ReferenceImagePath { get; set; } = string.Empty;
        public RegionParameters Regions { get; set; } = new RegionParameters();

        public MaterialRegionProfile Clone()
        {
            return new MaterialRegionProfile
            {
                MaterialNumber = MaterialNumber,
                ReferenceImagePath = ReferenceImagePath,
                Regions = (Regions ?? new RegionParameters()).Clone()
            };
        }
    }

    public static class VisionParameterStore
    {
        private static readonly object SyncRoot = new object();
        private static readonly string RootDirectory = Path.Combine(Application.StartupPath, "视觉参数");
        private static readonly string ApplicationFile = Path.Combine(RootDirectory, "系统检测参数.xml");
        private static ApplicationParameters applicationParameters;
        private static MaterialRegionProfile materialProfile;

        public static ApplicationParameters ApplicationParameters
        {
            get
            {
                lock (SyncRoot)
                {
                    if (applicationParameters == null)
                        applicationParameters = LoadXml(ApplicationFile, new ApplicationParameters());
                    Normalize(applicationParameters);
                    return applicationParameters.Clone();
                }
            }
        }

        public static MaterialRegionProfile CurrentMaterialProfile
        {
            get
            {
                lock (SyncRoot)
                {
                    string material = ResolveCurrentMaterialNumber();
                    if (materialProfile == null || !string.Equals(materialProfile.MaterialNumber, material, StringComparison.OrdinalIgnoreCase))
                        materialProfile = LoadMaterial(material);
                    return materialProfile.Clone();
                }
            }
        }

        public static MaterialRegionProfile LoadMaterial(string materialNumber)
        {
            lock (SyncRoot)
            {
                string normalized = NormalizeMaterialNumber(materialNumber);
                string file = GetMaterialFile(normalized);
                MaterialRegionProfile profile = LoadXml(file, new MaterialRegionProfile
                {
                    MaterialNumber = normalized,
                    Regions = new RegionParameters()
                });
                profile.MaterialNumber = normalized;
                if (profile.Regions == null)
                    profile.Regions = new RegionParameters();
                profile.Regions.Validate();
                materialProfile = profile;
                return profile.Clone();
            }
        }

        public static void SaveApplicationParameters(ApplicationParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            lock (SyncRoot)
            {
                ApplicationParameters snapshot = parameters.Clone();
                Normalize(snapshot);
                SaveXml(ApplicationFile, snapshot);
                applicationParameters = snapshot;
            }
        }

        public static void SaveMaterialProfile(MaterialRegionProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            lock (SyncRoot)
            {
                MaterialRegionProfile snapshot = profile.Clone();
                snapshot.MaterialNumber = NormalizeMaterialNumber(snapshot.MaterialNumber);
                if (snapshot.Regions == null)
                    snapshot.Regions = new RegionParameters();
                snapshot.Regions.Validate();
                SaveXml(GetMaterialFile(snapshot.MaterialNumber), snapshot);
                materialProfile = snapshot;
            }
        }

        public static void ActivateMaterial(string materialNumber)
        {
            LoadMaterial(materialNumber);
        }

        private static string ResolveCurrentMaterialNumber()
        {
            string material = 数据变量.料号名称;
            if (string.IsNullOrWhiteSpace(material))
                material = Properties.Settings.Default.当前料号;
            return NormalizeMaterialNumber(material);
        }

        private static string NormalizeMaterialNumber(string materialNumber)
        {
            return string.IsNullOrWhiteSpace(materialNumber) ? "默认料号" : materialNumber.Trim();
        }

        private static string GetMaterialFile(string materialNumber)
        {
            string safe = materialNumber;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                safe = safe.Replace(invalid, '_');
            return Path.Combine(RootDirectory, "料号", safe + ".xml");
        }

        private static void Normalize(ApplicationParameters parameters)
        {
            if (parameters.Inspection == null) parameters.Inspection = new InspectionParameters();
            if (parameters.DetectionCamera == null) parameters.DetectionCamera = new CameraRoleParameters();
            if (parameters.ClassificationCamera == null) parameters.ClassificationCamera = new CameraRoleParameters();
            if (string.IsNullOrWhiteSpace(parameters.DetectionCamera.OkImagePath))
                parameters.DetectionCamera.OkImagePath = Path.Combine(Application.StartupPath, "调试结果", "OK");
            if (string.IsNullOrWhiteSpace(parameters.DetectionCamera.NgImagePath))
                parameters.DetectionCamera.NgImagePath = Path.Combine(Application.StartupPath, "调试结果", "NG");
            parameters.Inspection.Validate();
            parameters.PermissionTimeoutMinutes = Math.Max(1, parameters.PermissionTimeoutMinutes);
            parameters.ImageRetentionDays = Math.Max(1, parameters.ImageRetentionDays);
        }

        private static T LoadXml<T>(string path, T fallback)
        {
            try
            {
                if (!File.Exists(path)) return fallback;
                using (FileStream stream = File.OpenRead(path))
                    return (T)new XmlSerializer(typeof(T)).Deserialize(stream);
            }
            catch
            {
                return fallback;
            }
        }

        private static void SaveXml<T>(string path, T value)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            string temp = path + ".tmp";
            using (FileStream stream = File.Create(temp))
                new XmlSerializer(typeof(T)).Serialize(stream, value);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
    }
}
