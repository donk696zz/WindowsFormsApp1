param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$binaryDirectory = Join-Path $projectRoot 'WindowsFormsApp1\bin\Debug'
$env:PATH = $binaryDirectory + ';' + $env:PATH

Add-Type -Path (Join-Path $binaryDirectory 'OpenCvSharp.dll')
[System.Reflection.Assembly]::LoadFrom(
    (Join-Path $binaryDirectory 'WindowsFormsApp1.exe')) | Out-Null

function Save-CombinedMask {
    param(
        [OpenCvSharp.Mat]$Left,
        [OpenCvSharp.Mat]$Right,
        [OpenCvSharp.Rect]$LeftBox,
        [OpenCvSharp.Rect]$RightBox,
        [int]$Rows,
        [int]$Columns,
        [string]$Path
    )

    $combined = New-Object OpenCvSharp.Mat(
        $Rows, $Columns, [OpenCvSharp.MatType]::CV_8UC1,
        [OpenCvSharp.Scalar]::Black)
    $leftDestination = New-Object OpenCvSharp.Mat($combined, $LeftBox)
    $rightDestination = New-Object OpenCvSharp.Mat($combined, $RightBox)
    try {
        $Left.CopyTo($leftDestination)
        $Right.CopyTo($rightDestination)
        [OpenCvSharp.Cv2]::ImWrite($Path, $combined) | Out-Null
    }
    finally {
        $rightDestination.Dispose()
        $leftDestination.Dispose()
        $combined.Dispose()
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$source = [OpenCvSharp.Cv2]::ImRead($SourcePath)
$regionParameters = New-Object WindowsFormsApp1.RegionParameters
$applicationParameterFile = Get-ChildItem -Path $binaryDirectory -Filter '*.xml' -Recurse |
    Where-Object {
        (Get-Content -LiteralPath $_.FullName -Raw) -match '<ApplicationParameters'
    } |
    Select-Object -First 1 -ExpandProperty FullName
if ($applicationParameterFile) {
    $serializer = New-Object System.Xml.Serialization.XmlSerializer(
        [WindowsFormsApp1.ApplicationParameters])
    $stream = [System.IO.File]::OpenRead($applicationParameterFile)
    try {
        $savedParameters = $serializer.Deserialize($stream)
        $inspectionParameters = $savedParameters.Inspection
    }
    finally {
        $stream.Dispose()
    }
}
else {
    $inspectionParameters = New-Object WindowsFormsApp1.InspectionParameters
}
$loadedReviewArea = $inspectionParameters.InnerDefectReviewArea
$loadedNgArea = $inspectionParameters.MinimumDefectArea
$angle = 0.0
$aligned = [WindowsFormsApp1.ModuleRegionLocator]::AlignToModule(
    $source, $regionParameters, [ref]$angle)
$gray = New-Object OpenCvSharp.Mat

try {
    [OpenCvSharp.Cv2]::CvtColor(
        $aligned, $gray,
        [OpenCvSharp.ColorConversionCodes]::BGR2GRAY, 0)
    $regions = [WindowsFormsApp1.ModuleRegionLocator]::Locate(
        $aligned, $regionParameters)
    $leftBox = ($regions.Regions | Where-Object {
        $_.Type -eq [WindowsFormsApp1.ModuleRegionType]::LeftSilver
    }).Box
    $rightBox = ($regions.Regions | Where-Object {
        $_.Type -eq [WindowsFormsApp1.ModuleRegionType]::RightSilver
    }).Box

    $leftStages = [WindowsFormsApp1.AdaptiveSilverNormalModel]::CreateDarkDefectDebugStages(
        $gray, $leftBox, $inspectionParameters)
    $rightStages = [WindowsFormsApp1.AdaptiveSilverNormalModel]::CreateDarkDefectDebugStages(
        $gray, $rightBox, $inspectionParameters)
    try {
        [OpenCvSharp.Cv2]::ImWrite(
            (Join-Path $OutputDirectory '00_original.png'), $source) | Out-Null
        [OpenCvSharp.Cv2]::ImWrite(
            (Join-Path $OutputDirectory '01_aligned_source.png'), $aligned) | Out-Null
        [OpenCvSharp.Cv2]::ImWrite(
            (Join-Path $OutputDirectory '02_gray.png'), $gray) | Out-Null

        $stageDefinitions = @(
            @('03_gradient_mask.png', 'GradientMask'),
            @('04_silver_body_mask.png', 'SilverBodyMask'),
            @('05_silver_body_envelope.png', 'SilverBodyEnvelope'),
            @('05a_silver_body_symmetric_reference.png', 'SilverBodySymmetricReference'),
            @('05b_edge_concavity_mask.png', 'EdgeConcavityMask'),
            @('06_dark_inside_envelope.png', 'DarkInsideEnvelope'),
            @('07_eroded_dark_mask.png', 'ErodedDarkMask'),
            @('08_opened_dark_mask.png', 'OpenedDarkMask'),
            @('09_envelope_boundary_band.png', 'EnvelopeBoundaryBand'),
            @('10_final_area_filtered_defects.png', 'FinalDefectMask')
        )
        foreach ($definition in $stageDefinitions) {
            Save-CombinedMask `
                -Left $leftStages.($definition[1]) `
                -Right $rightStages.($definition[1]) `
                -LeftBox $leftBox `
                -RightBox $rightBox `
                -Rows $gray.Rows `
                -Columns $gray.Cols `
                -Path (Join-Path $OutputDirectory $definition[0])
        }

        $metricLines = New-Object System.Collections.Generic.List[string]
        foreach ($entry in @(
            @('L', $leftBox),
            @('R', $rightBox))) {
            $measurements = [WindowsFormsApp1.AdaptiveSilverNormalModel]::MeasureEdgeConcavities(
                $gray, $entry[1], $inspectionParameters)
            foreach ($measurement in $measurements) {
                $metricLines.Add(('{0} area={1} depth={2:F2} opening={3:F2} thickness={4:F2} score={5:F0} class={6} box={7}' -f
                    $entry[0],
                    $measurement.PixelArea,
                    $measurement.MaximumDepth,
                    $measurement.OpeningWidth,
                    $measurement.AverageThickness,
                    $measurement.EffectiveArea,
                    [WindowsFormsApp1.AdaptiveSilverNormalModel]::ClassifyEdgeConcavity(
                        $measurement, $inspectionParameters),
                    $measurement.Box))
            }
        }
        [System.IO.File]::WriteAllLines(
            (Join-Path $OutputDirectory '05c_edge_concavity_metrics.txt'),
            $metricLines)
        $classifiedConcavities = [WindowsFormsApp1.AdaptiveSilverNormalModel]::CreateEdgeConcavityClassificationImage(
            $gray, $leftBox, $rightBox, $inspectionParameters)
        try {
            [OpenCvSharp.Cv2]::ImWrite(
                (Join-Path $OutputDirectory '05d_edge_concavity_classified.png'),
                $classifiedConcavities) | Out-Null
        }
        finally {
            $classifiedConcavities.Dispose()
        }

        # Fill only the external contours; no morphology is applied here.
        $bodyPath = Join-Path $OutputDirectory '04_silver_body_mask.png'
        $bodyColor = [OpenCvSharp.Cv2]::ImRead(
            (Resolve-Path $bodyPath).Path)
        $bodyMask = New-Object OpenCvSharp.Mat
        [OpenCvSharp.Cv2]::CvtColor(
            $bodyColor, $bodyMask,
            [OpenCvSharp.ColorConversionCodes]::BGR2GRAY, 0)
        $filledBody = New-Object OpenCvSharp.Mat(
            $bodyMask.Rows, $bodyMask.Cols,
            [OpenCvSharp.MatType]::CV_8UC1,
            [OpenCvSharp.Scalar]::Black)
        try {
            $contours = [OpenCvSharp.Cv2]::FindContoursAsArray(
                $bodyMask,
                [OpenCvSharp.RetrievalModes]::External,
                [OpenCvSharp.ContourApproximationModes]::ApproxSimple)
            [OpenCvSharp.Cv2]::DrawContours(
                $filledBody, $contours, -1,
                [OpenCvSharp.Scalar]::White, -1) | Out-Null
            [OpenCvSharp.Cv2]::ImWrite(
                (Join-Path $OutputDirectory '04a_silver_body_filled_only.png'),
                $filledBody) | Out-Null
        }
        finally {
            $filledBody.Dispose()
            $bodyMask.Dispose()
            $bodyColor.Dispose()
        }

        [pscustomobject]@{
            OutputDirectory = $OutputDirectory
            AlignmentAngle = $angle
            LeftBox = $leftBox
            RightBox = $rightBox
            MorphKernel = $inspectionParameters.DefectMorphKernelSize
            ReviewArea = $inspectionParameters.InnerDefectReviewArea
            NgArea = $inspectionParameters.MinimumDefectArea
            LoadedReviewArea = $loadedReviewArea
            LoadedNgArea = $loadedNgArea
        }
    }
    finally {
        $rightStages.Dispose()
        $leftStages.Dispose()
    }
}
finally {
    $gray.Dispose()
    $aligned.Dispose()
    $source.Dispose()
}
