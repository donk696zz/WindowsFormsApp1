param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$bin = Join-Path $root 'WindowsFormsApp1\bin\Debug'
$env:PATH = $bin + ';' + $env:PATH

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$applicationAssembly = [System.Reflection.Assembly]::LoadFrom(
    (Join-Path $bin 'WindowsFormsApp1.exe'))

[System.Windows.Forms.Application]::EnableVisualStyles()
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

function Decode-Text([string]$value) {
    return [System.Text.Encoding]::UTF8.GetString(
        [System.Convert]::FromBase64String($value))
}

$pages = @(
    @('V2luZG93c0Zvcm1zQXBwMS7mlpnlj7fmt7vliqA=', '01_material_management.png'),
    @('V2luZG93c0Zvcm1zQXBwMS7mlpnlj7forr7nva7pobXpnaI=', '02_material_settings.png'),
    @('V2luZG93c0Zvcm1zQXBwMS7lj4LmlbDnlYzpnaI=', '03_parameters.png'),
    @('V2luZG93c0Zvcm1zQXBwMS7osIPor5XpobXpnaI=', '04_debug.png'),
    @('V2luZG93c0Zvcm1zQXBwMS7nm7jmnLrorr7nva4=', '10_camera_settings_base.png'),
    @('V2luZG93c0Zvcm1zQXBwMS7oh6rliqjpobXpnaI=', '11_auto_page_base.png')
)

foreach ($page in $pages) {
    $type = $applicationAssembly.GetType((Decode-Text $page[0]), $true)
    $form = [Activator]::CreateInstance($type)
    try {
        if ($page[1] -eq '11_auto_page_base.png') {
            $form.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::None
            $form.ClientSize = New-Object System.Drawing.Size(1406, 791)
        }
        $form.ShowInTaskbar = $false
        $form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
        $form.Location = New-Object System.Drawing.Point(-32000, -32000)
        $form.Show()
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 150
        if ($page[1] -eq '11_auto_page_base.png') {
            $flags = [System.Reflection.BindingFlags]::Instance -bor
                [System.Reflection.BindingFlags]::NonPublic
            foreach ($fieldName in @('splitContainerStats', 'splitContainerImages')) {
                $split = $type.GetField($fieldName, $flags).GetValue($form)
                $split.SplitterDistance = [int](
                    ($split.Width - $split.SplitterWidth) / 2)
            }
            [System.Windows.Forms.Application]::DoEvents()
        }
        $form.PerformLayout()
        foreach ($control in $form.Controls) {
            $control.CreateControl()
            $control.PerformLayout()
        }
        $width = [Math]::Max(1000, $form.ClientSize.Width)
        $height = [Math]::Max(650, $form.ClientSize.Height)
        $bitmap = New-Object System.Drawing.Bitmap($width, $height)
        try {
            $form.DrawToBitmap(
                $bitmap,
                (New-Object System.Drawing.Rectangle(0, 0, $width, $height)))
            $bitmap.Save(
                (Join-Path $OutputDirectory $page[1]),
                [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $bitmap.Dispose()
        }
    }
    finally {
        $form.Close()
        $form.Dispose()
    }
}

Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.png' |
    Select-Object Name, Length
