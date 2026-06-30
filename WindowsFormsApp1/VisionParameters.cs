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
        public double MiddleSilverReviewRatio { get; set; } = 0.0030;
        public double MiddleSilverNgRatio { get; set; } = 0.0050;
        public double MissingSilverReviewRatio { get; set; } = 0.02;
        public double MissingSilverNgRatio { get; set; } = 0.10;
        public double InnerDefectReviewRatio { get; set; } = 0.0020;
        public double InnerDefectNgRatio { get; set; } = 0.0025;
        public double EdgeSilverReviewRatio { get; set; } = 0.40;
        public double EdgeSilverNgRatio { get; set; } = 0.50;
        public double SilverTopCoverageNgRatio { get; set; } = 0.65;
        public double SilverBottomCoverageNgRatio { get; set; } = 0.65;
        public double SilverCoverageOkRatio { get; set; } = 0.70;

        public int SilverGrayThreshold { get; set; } = 145;
        public int EdgeSilverGrayThreshold { get; set; } = 225;
        public int MiddleSilverThresholdOffset { get; set; } = 55;
        public int MiddleSilverThresholdMinimum { get; set; } = 145;
        public int MiddleSilverThresholdMaximum { get; set; } = 190;
        public int MissingDarkThreshold { get; set; } = 95;
        public int MissingBoundaryMargin { get; set; } = 1;
        public int StrongDarkGrayThreshold { get; set; } = 115;
        public int LocalContrastThreshold { get; set; } = 45;
        public int DefectBackgroundKernelSize { get; set; } = 41;
        public int DefectMorphKernelSize { get; set; } = 3;
        public int DefectBoxPadding { get; set; } = 8;
        public int DefectBoxMinimumWidth { get; set; } = 54;
        public int DefectBoxMinimumHeight { get; set; } = 56;
        public int MaximumDefectsPerSilverRegion { get; set; } = 4;

        public double EdgeMissingReviewRatio { get; set; } = 0.0200;
        public double EdgeMissingNgRatio { get; set; } = 0.0350;
        public int EdgeMaskGrayThreshold { get; set; } = 135;
        public int EdgeMaskCloseKernelSize { get; set; } = 9;
        public int PairedEdgeCloseKernelSize { get; set; } = 21;
        public int EdgeBandDepth { get; set; } = 55;
        public int EdgeBoundaryContactDepth { get; set; } = 5;
        public int EdgeMinimumDefectArea { get; set; } = 80;
        public int EdgeMinimumDefectWidth { get; set; } = 6;
        public int EdgeMinimumDefectHeight { get; set; } = 6;
        public double EdgeMinimumDefectFillRatio { get; set; } = 0.20;
        public int EdgeBoxPadding { get; set; } = 6;
        public int MaximumEdgeDefectsPerSilverRegion { get; set; } = 4;

        public double LineDefectReviewRatio { get; set; } = 0.0020;
        public double LineDefectNgRatio { get; set; } = 0.0040;
        public int LineContrastThreshold { get; set; } = 28;
        public int LineKernelLength { get; set; } = 25;
        public int LineMinimumLength { get; set; } = 24;
        public int LineMaximumWidth { get; set; } = 12;
        public int LineMinimumArea { get; set; } = 35;
        public int LineBoxPadding { get; set; } = 6;

        public int ModuleVisibleMargin { get; set; } = 2;
        public double ModuleBorderDarkRatio { get; set; } = 0.50;

        // 正常样本模型与银区尺寸自适应参数。核尺寸均按银区最短边比例计算。
        public double AdaptiveOkSafetyFactor { get; set; } = 0.95;
        public double AdaptiveDefaultOkDistance { get; set; } = 0.06;
        public int AdaptiveSilverThresholdMinimum { get; set; } = 115;
        public int AdaptiveSilverThresholdMaximum { get; set; } = 190;
        public double AdaptiveMaskCloseRatio { get; set; } = 0.035;
        public double AdaptiveMaskOpenRatio { get; set; } = 0.012;
        public double AdaptiveInteriorErodeRatio { get; set; } = 0.080;
        public double AdaptiveBackgroundKernelRatio { get; set; } = 0.130;
        public double AdaptiveShapeCoreProbability { get; set; } = 0.80;
        public double AdaptiveTextureMinimumContrast { get; set; } = 1.80;
        public double AdaptiveTexturePercentile { get; set; } = 0.99;
        public double AdaptiveCandidateSafetyFactor { get; set; } = 1.05;
        public double AdaptiveNgMultiplier { get; set; } = 1.50;
        public double AdaptiveShapeNgRatioMinimum { get; set; } = 0.075;
        public double AdaptiveTextureNgRatioMinimum { get; set; } = 0.008;
        public double AdaptiveBoxPaddingRatio { get; set; } = 0.020;
        public int RoughSilverThresholdMinimum { get; set; } = 130;
        public int RoughSilverThresholdMaximum { get; set; } = 175;
        public int ObservedP25Offset { get; set; } = 22;
        public int ObservedMedianOffset { get; set; } = 55;
        public int ObservedThresholdMinimum { get; set; } = 90;
        public int ObservedThresholdMaximum { get; set; } = 140;
        public int StrongDarkP25Offset { get; set; } = 35;
        public int StrongDarkMedianOffset { get; set; } = 75;
        public int StrongDarkThresholdMinimum { get; set; } = 55;
        public int StrongDarkThresholdMaximum { get; set; } = 115;
        public int SemiDarkP25Offset { get; set; } = 12;
        public int SemiDarkMedianOffset { get; set; } = 45;
        public int SemiDarkThresholdMinimum { get; set; } = 85;
        public int SemiDarkThresholdMaximum { get; set; } = 135;
        public int BrightThresholdBase { get; set; } = 190;
        public int BrightMedianOffset { get; set; } = 20;
        public int BrightThresholdMinimum { get; set; } = 180;
        public int BrightThresholdMaximum { get; set; } = 225;
        public int ExpectedCloseKernelMinimum { get; set; } = 11;
        public int ExpectedCloseKernelMaximum { get; set; } = 31;
        public int ExpectedCloseKernelDivisor { get; set; } = 10;
        public int ExpectedDilateKernelMinimum { get; set; } = 3;
        public int ExpectedDilateKernelMaximum { get; set; } = 9;
        public int ExpectedDilateKernelDivisor { get; set; } = 28;
        public int BackgroundKernelMinimum { get; set; } = 21;
        public int BackgroundKernelMaximum { get; set; } = 61;
        public int BackgroundKernelDivisor { get; set; } = 5;
        public int MinimumDefectArea { get; set; } = 45;
        public double MinimumDefectAreaRatio { get; set; } = 0.00030;
        public int MinimumDefectWidth { get; set; } = 5;
        public int MinimumDefectHeight { get; set; } = 5;
        public double MinimumDefectFillRatio { get; set; } = 0.28;
        public int MinimumSilverSupportSides { get; set; } = 3;
        public double MinimumSideSilverRatio { get; set; } = 0.25;
        public double MinimumVeryDarkRatio { get; set; } = 0.10;
        public double MinimumDullRatio { get; set; } = 0.28;
        public double MaximumBrightRatio { get; set; } = 0.45;
        public double BrightRejectDarkRatio { get; set; } = 0.18;
        public double MinimumDefectAspectRatio { get; set; } = 0.25;
        public double MaximumDefectAspectRatio { get; set; } = 4.5;

        public InspectionParameters Clone() => (InspectionParameters)MemberwiseClone();

        public void Validate()
        {
            MiddleSilverReviewRatio = Ratio(MiddleSilverReviewRatio);
            MiddleSilverNgRatio = Math.Max(MiddleSilverReviewRatio, Ratio(MiddleSilverNgRatio));
            MissingSilverReviewRatio = Ratio(MissingSilverReviewRatio);
            MissingSilverNgRatio = Math.Max(MissingSilverReviewRatio, Ratio(MissingSilverNgRatio));
            InnerDefectReviewRatio = Ratio(InnerDefectReviewRatio);
            InnerDefectNgRatio = Math.Max(InnerDefectReviewRatio, Ratio(InnerDefectNgRatio));
            EdgeSilverReviewRatio = Ratio(EdgeSilverReviewRatio);
            EdgeSilverNgRatio = Math.Max(EdgeSilverReviewRatio, Ratio(EdgeSilverNgRatio));
            SilverTopCoverageNgRatio = Ratio(SilverTopCoverageNgRatio);
            SilverBottomCoverageNgRatio = Ratio(SilverBottomCoverageNgRatio);
            SilverCoverageOkRatio = Math.Max(Math.Max(SilverTopCoverageNgRatio, SilverBottomCoverageNgRatio), Ratio(SilverCoverageOkRatio));

            SilverGrayThreshold = Gray(SilverGrayThreshold);
            EdgeSilverGrayThreshold = Gray(EdgeSilverGrayThreshold);
            MiddleSilverThresholdOffset = Gray(MiddleSilverThresholdOffset);
            MiddleSilverThresholdMinimum = Gray(MiddleSilverThresholdMinimum);
            MiddleSilverThresholdMaximum = Math.Max(MiddleSilverThresholdMinimum, Gray(MiddleSilverThresholdMaximum));
            MissingDarkThreshold = Gray(MissingDarkThreshold);
            MissingBoundaryMargin = Math.Max(0, MissingBoundaryMargin);
            StrongDarkGrayThreshold = Gray(StrongDarkGrayThreshold);
            LocalContrastThreshold = Gray(LocalContrastThreshold);
            DefectBackgroundKernelSize = OddAtLeastThree(DefectBackgroundKernelSize);
            DefectMorphKernelSize = OddAtLeastThree(DefectMorphKernelSize);
            DefectBoxPadding = Math.Max(0, DefectBoxPadding);
            DefectBoxMinimumWidth = Math.Max(1, DefectBoxMinimumWidth);
            DefectBoxMinimumHeight = Math.Max(1, DefectBoxMinimumHeight);
            MaximumDefectsPerSilverRegion = Math.Max(1, Math.Min(20, MaximumDefectsPerSilverRegion));
            EdgeMissingReviewRatio = Ratio(EdgeMissingReviewRatio);
            EdgeMissingNgRatio = Math.Max(EdgeMissingReviewRatio, Ratio(EdgeMissingNgRatio));
            EdgeMaskGrayThreshold = Gray(EdgeMaskGrayThreshold);
            EdgeMaskCloseKernelSize = OddAtLeastThree(EdgeMaskCloseKernelSize);
            PairedEdgeCloseKernelSize = OddAtLeastThree(PairedEdgeCloseKernelSize);
            EdgeBandDepth = Math.Max(1, EdgeBandDepth);
            EdgeBoundaryContactDepth = Math.Max(1, EdgeBoundaryContactDepth);
            EdgeMinimumDefectArea = Math.Max(1, EdgeMinimumDefectArea);
            EdgeMinimumDefectWidth = Math.Max(1, EdgeMinimumDefectWidth);
            EdgeMinimumDefectHeight = Math.Max(1, EdgeMinimumDefectHeight);
            EdgeMinimumDefectFillRatio = Ratio(EdgeMinimumDefectFillRatio);
            EdgeBoxPadding = Math.Max(0, EdgeBoxPadding);
            MaximumEdgeDefectsPerSilverRegion = Math.Max(1, Math.Min(20, MaximumEdgeDefectsPerSilverRegion));
            LineDefectReviewRatio = Ratio(LineDefectReviewRatio);
            LineDefectNgRatio = Math.Max(LineDefectReviewRatio, Ratio(LineDefectNgRatio));
            LineContrastThreshold = Gray(LineContrastThreshold);
            LineKernelLength = OddAtLeastThree(LineKernelLength);
            LineMinimumLength = Math.Max(1, LineMinimumLength);
            LineMaximumWidth = Math.Max(1, LineMaximumWidth);
            LineMinimumArea = Math.Max(1, LineMinimumArea);
            LineBoxPadding = Math.Max(0, LineBoxPadding);
            ModuleVisibleMargin = Math.Max(0, ModuleVisibleMargin);
            ModuleBorderDarkRatio = Ratio(ModuleBorderDarkRatio);
            AdaptiveOkSafetyFactor = Clamp(AdaptiveOkSafetyFactor, 0.10, 0.999);
            AdaptiveDefaultOkDistance = Clamp(AdaptiveDefaultOkDistance, 0.0001, 2.0);
            AdaptiveSilverThresholdMinimum = Gray(AdaptiveSilverThresholdMinimum);
            AdaptiveSilverThresholdMaximum = Math.Max(AdaptiveSilverThresholdMinimum, Gray(AdaptiveSilverThresholdMaximum));
            AdaptiveMaskCloseRatio = Clamp(AdaptiveMaskCloseRatio, 0.005, 0.50);
            AdaptiveMaskOpenRatio = Clamp(AdaptiveMaskOpenRatio, 0.005, 0.25);
            AdaptiveInteriorErodeRatio = Clamp(AdaptiveInteriorErodeRatio, 0.005, 0.50);
            AdaptiveBackgroundKernelRatio = Clamp(AdaptiveBackgroundKernelRatio, 0.01, 0.80);
            AdaptiveShapeCoreProbability = Clamp(AdaptiveShapeCoreProbability, 0.50, 1.0);
            AdaptiveTextureMinimumContrast = Clamp(AdaptiveTextureMinimumContrast, 0.10, 20.0);
            AdaptiveTexturePercentile = Clamp(AdaptiveTexturePercentile, 0.80, 0.9999);
            AdaptiveCandidateSafetyFactor = Clamp(AdaptiveCandidateSafetyFactor, 1.0, 5.0);
            AdaptiveNgMultiplier = Clamp(AdaptiveNgMultiplier, 1.0, 10.0);
            AdaptiveShapeNgRatioMinimum = Ratio(AdaptiveShapeNgRatioMinimum);
            AdaptiveTextureNgRatioMinimum = Ratio(AdaptiveTextureNgRatioMinimum);
            AdaptiveBoxPaddingRatio = Clamp(AdaptiveBoxPaddingRatio, 0, 0.25);
            RoughSilverThresholdMinimum = Gray(RoughSilverThresholdMinimum);
            RoughSilverThresholdMaximum = Math.Max(RoughSilverThresholdMinimum, Gray(RoughSilverThresholdMaximum));
            ObservedP25Offset = Gray(ObservedP25Offset);
            ObservedMedianOffset = Gray(ObservedMedianOffset);
            ObservedThresholdMinimum = Gray(ObservedThresholdMinimum);
            ObservedThresholdMaximum = Math.Max(ObservedThresholdMinimum, Gray(ObservedThresholdMaximum));
            StrongDarkP25Offset = Gray(StrongDarkP25Offset);
            StrongDarkMedianOffset = Gray(StrongDarkMedianOffset);
            StrongDarkThresholdMinimum = Gray(StrongDarkThresholdMinimum);
            StrongDarkThresholdMaximum = Math.Max(StrongDarkThresholdMinimum, Gray(StrongDarkThresholdMaximum));
            SemiDarkP25Offset = Gray(SemiDarkP25Offset);
            SemiDarkMedianOffset = Gray(SemiDarkMedianOffset);
            SemiDarkThresholdMinimum = Gray(SemiDarkThresholdMinimum);
            SemiDarkThresholdMaximum = Math.Max(SemiDarkThresholdMinimum, Gray(SemiDarkThresholdMaximum));
            BrightThresholdBase = Gray(BrightThresholdBase);
            BrightMedianOffset = Gray(BrightMedianOffset);
            BrightThresholdMinimum = Gray(BrightThresholdMinimum);
            BrightThresholdMaximum = Math.Max(BrightThresholdMinimum, Gray(BrightThresholdMaximum));
            ExpectedCloseKernelMinimum = OddAtLeastThree(ExpectedCloseKernelMinimum);
            ExpectedCloseKernelMaximum = Math.Max(ExpectedCloseKernelMinimum, OddAtLeastThree(ExpectedCloseKernelMaximum));
            ExpectedCloseKernelDivisor = Math.Max(1, ExpectedCloseKernelDivisor);
            ExpectedDilateKernelMinimum = OddAtLeastThree(ExpectedDilateKernelMinimum);
            ExpectedDilateKernelMaximum = Math.Max(ExpectedDilateKernelMinimum, OddAtLeastThree(ExpectedDilateKernelMaximum));
            ExpectedDilateKernelDivisor = Math.Max(1, ExpectedDilateKernelDivisor);
            BackgroundKernelMinimum = OddAtLeastThree(BackgroundKernelMinimum);
            BackgroundKernelMaximum = Math.Max(BackgroundKernelMinimum, OddAtLeastThree(BackgroundKernelMaximum));
            BackgroundKernelDivisor = Math.Max(1, BackgroundKernelDivisor);
            MinimumDefectArea = Math.Max(1, MinimumDefectArea);
            MinimumDefectAreaRatio = Ratio(MinimumDefectAreaRatio);
            MinimumDefectWidth = Math.Max(1, MinimumDefectWidth);
            MinimumDefectHeight = Math.Max(1, MinimumDefectHeight);
            MinimumDefectFillRatio = Ratio(MinimumDefectFillRatio);
            MinimumSilverSupportSides = Math.Max(1, Math.Min(4, MinimumSilverSupportSides));
            MinimumSideSilverRatio = Ratio(MinimumSideSilverRatio);
            MinimumVeryDarkRatio = Ratio(MinimumVeryDarkRatio);
            MinimumDullRatio = Ratio(MinimumDullRatio);
            MaximumBrightRatio = Ratio(MaximumBrightRatio);
            BrightRejectDarkRatio = Ratio(BrightRejectDarkRatio);
            MinimumDefectAspectRatio = Math.Max(0.01, MinimumDefectAspectRatio);
            MaximumDefectAspectRatio = Math.Max(MinimumDefectAspectRatio, MaximumDefectAspectRatio);
        }

        private static int Gray(int value) => Math.Max(0, Math.Min(255, value));
        private static double Ratio(double value) => Math.Max(0, Math.Min(1, value));
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
