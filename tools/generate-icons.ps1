# Génère favicon.png, icon-192.png, icon-512.png pour l'app Coffee Tracker.
# Design : grain de café crème sur fond rounded-square coffee-800, palette de l'appli.
# Usage : pwsh -File tools/generate-icons.ps1   (depuis la racine du repo)

Add-Type -AssemblyName System.Drawing

function New-CoffeeIcon {
    param(
        [Parameter(Mandatory)] [int]    $Size,
        [Parameter(Mandatory)] [string] $OutPath
    )

    $bmp = New-Object System.Drawing.Bitmap $Size, $Size
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    # Logical canvas 1024×1024 → scale to requested size.
    $scale = $Size / 1024.0
    $g.ScaleTransform($scale, $scale)

    # Couleurs palette appli.
    $coffee800 = [System.Drawing.Color]::FromArgb(0x3b, 0x24, 0x18)
    $coffee600 = [System.Drawing.Color]::FromArgb(0x7d, 0x51, 0x28)
    $cream     = [System.Drawing.Color]::FromArgb(0xf3, 0xe9, 0xd2)

    # Fond rounded-square (iOS auto-mask compatible : marge intérieure suffisante).
    $r = 230
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0,             0,             $r * 2, $r * 2, 180, 90)
    $path.AddArc(1024 - $r * 2, 0,             $r * 2, $r * 2, 270, 90)
    $path.AddArc(1024 - $r * 2, 1024 - $r * 2, $r * 2, $r * 2, 0,   90)
    $path.AddArc(0,             1024 - $r * 2, $r * 2, $r * 2, 90,  90)
    $path.CloseFigure()
    $bgBrush = New-Object System.Drawing.SolidBrush $coffee800
    $g.FillPath($bgBrush, $path)

    # Repère au centre, rotation -22° pour donner du dynamisme au grain.
    $g.TranslateTransform(512, 512)
    $g.RotateTransform(-22)

    # Grain : ellipse cream (avec un léger contour coffee-600 pour le relief).
    $bw = 470; $bh = 640
    $beanBrush = New-Object System.Drawing.SolidBrush $cream
    $g.FillEllipse($beanBrush, -$bw / 2, -$bh / 2, $bw, $bh)

    $rim = New-Object System.Drawing.Pen $coffee600, 14
    $g.DrawEllipse($rim, -$bw / 2, -$bh / 2, $bw, $bh)

    # Sillon central : courbe en S (Bézier cubique) entre haut et bas du grain.
    $groove = New-Object System.Drawing.Pen $coffee800, 56
    $groove.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $groove.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
    $points = @(
        [System.Drawing.PointF]::new(-12, -280),
        [System.Drawing.PointF]::new( 95, -130),
        [System.Drawing.PointF]::new(-95,  130),
        [System.Drawing.PointF]::new( 12,  280)
    )
    $g.DrawBeziers($groove, $points)

    $g.Dispose()

    # Crée le dossier parent au besoin.
    $dir = Split-Path -Parent $OutPath
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    $bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Generated $OutPath ($Size px)"
}

$root = Split-Path -Parent $PSScriptRoot
$wwwroot = Join-Path $root 'wwwroot'

New-CoffeeIcon -Size 64  -OutPath (Join-Path $wwwroot 'favicon.png')
New-CoffeeIcon -Size 192 -OutPath (Join-Path $wwwroot 'icon-192.png')
New-CoffeeIcon -Size 512 -OutPath (Join-Path $wwwroot 'icon-512.png')
