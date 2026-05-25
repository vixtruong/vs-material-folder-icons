param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [double]$Padding = 0.5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName WindowsBase

$iconSize = 16.0
$svgCoordinateSize = 24.0
$svgNamespace = @{ svg = 'http://www.w3.org/2000/svg' }

function ConvertTo-SafeName([string]$name) {
    return $name -replace '[^A-Za-z0-9_]', '_'
}

function Get-ViewBoxTransform([xml]$svg) {
    $viewBox = $svg.DocumentElement.GetAttribute('viewBox')
    $numbers = @()
    if (-not [string]::IsNullOrWhiteSpace($viewBox)) {
        $numbers = $viewBox -split '[,\s]+' | Where-Object { $_ -ne '' }
    }

    if ($numbers.Count -eq 4) {
        $minX = [double]::Parse($numbers[0], [Globalization.CultureInfo]::InvariantCulture)
        $minY = [double]::Parse($numbers[1], [Globalization.CultureInfo]::InvariantCulture)
        $width = [double]::Parse($numbers[2], [Globalization.CultureInfo]::InvariantCulture)
        $height = [double]::Parse($numbers[3], [Globalization.CultureInfo]::InvariantCulture)
    }
    else {
        $minX = 0.0
        $minY = 0.0
        $width = $svgCoordinateSize
        $height = $svgCoordinateSize
    }

    if ($width -le 0 -or $height -le 0) {
        throw "Invalid viewBox '$viewBox'."
    }

    $xScale = ($iconSize - ($Padding * 2.0)) / $width
    $yScale = ($iconSize - ($Padding * 2.0)) / $height
    [Windows.Media.MatrixTransform]::new($xScale, 0.0, 0.0, $yScale, $Padding - ($minX * $xScale), $Padding - ($minY * $yScale))
}

function Multiply-Matrix([Windows.Media.Matrix]$left, [Windows.Media.Matrix]$right) {
    return [Windows.Media.Matrix]::Multiply($left, $right)
}

function Parse-Transform([string]$transform) {
    $matrix = [Windows.Media.Matrix]::Identity
    $matches = [regex]::Matches($transform, '(matrix|translate|scale)\s*\(([^)]*)\)', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    foreach ($match in $matches) {
        $values = @($match.Groups[2].Value -split '[,\s]+' | Where-Object { $_ -ne '' } | ForEach-Object {
            [double]::Parse($_, [Globalization.CultureInfo]::InvariantCulture)
        })

        switch ($match.Groups[1].Value.ToLowerInvariant()) {
            'matrix' {
                if ($values.Count -ge 6) {
                    $next = [Windows.Media.Matrix]::new($values[0], $values[1], $values[2], $values[3], $values[4], $values[5])
                    $matrix = Multiply-Matrix $next $matrix
                }
            }
            'translate' {
                if ($values.Count -ge 1) {
                    $ty = if ($values.Count -gt 1) { $values[1] } else { 0.0 }
                    $next = [Windows.Media.Matrix]::new(1.0, 0.0, 0.0, 1.0, $values[0], $ty)
                    $matrix = Multiply-Matrix $next $matrix
                }
            }
            'scale' {
                if ($values.Count -ge 1) {
                    $sy = if ($values.Count -gt 1) { $values[1] } else { $values[0] }
                    $next = [Windows.Media.Matrix]::new($values[0], 0.0, 0.0, $sy, 0.0, 0.0)
                    $matrix = Multiply-Matrix $next $matrix
                }
            }
        }
    }

    return $matrix
}

function Get-SvgTransform([System.Xml.XmlElement]$path) {
    $matrix = [Windows.Media.Matrix]::Identity
    $node = $path
    while ($node -is [System.Xml.XmlElement]) {
        if ($node.HasAttribute('transform')) {
            $matrix = Multiply-Matrix $matrix (Parse-Transform $node.GetAttribute('transform'))
        }

        $node = $node.ParentNode
    }

    return $matrix
}

function Get-StyleValue([System.Xml.XmlElement]$element, [string]$name) {
    if (-not $element.HasAttribute('style')) {
        return $null
    }

    foreach ($part in $element.GetAttribute('style') -split ';') {
        $pair = $part -split ':', 2
        if ($pair.Count -eq 2 -and $pair[0].Trim() -eq $name) {
            return $pair[1].Trim()
        }
    }

    return $null
}

function Get-Fill([xml]$svg, [System.Xml.XmlElement]$element) {
    $fill = $element.GetAttribute('fill')
    if ([string]::IsNullOrWhiteSpace($fill)) {
        $fill = Get-StyleValue $element 'fill'
    }

    $node = $element.ParentNode
    while ([string]::IsNullOrWhiteSpace($fill) -and $node -is [System.Xml.XmlElement]) {
        $fill = $node.GetAttribute('fill')
        if ([string]::IsNullOrWhiteSpace($fill)) {
            $fill = Get-StyleValue $node 'fill'
        }

        $node = $node.ParentNode
    }

    if ([string]::IsNullOrWhiteSpace($fill) -or $fill -eq 'none' -or -not $fill.StartsWith('#')) {
        return $null
    }

    return $fill
}

function Get-Double([System.Xml.XmlElement]$element, [string]$name, [double]$defaultValue = 0.0) {
    $value = $element.GetAttribute($name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $defaultValue
    }

    $normalized = $value -replace 'px$', ''
    return [double]::Parse($normalized, [Globalization.CultureInfo]::InvariantCulture)
}

function Get-Points([string]$points) {
    $values = @($points -split '[,\s]+' | Where-Object { $_ -ne '' } | ForEach-Object {
        [double]::Parse($_, [Globalization.CultureInfo]::InvariantCulture)
    })

    $result = [Collections.Generic.List[Windows.Point]]::new()
    for ($i = 0; $i + 1 -lt $values.Count; $i += 2) {
        $result.Add([Windows.Point]::new($values[$i], $values[$i + 1]))
    }

    return $result
}

function Convert-ElementToGeometry([System.Xml.XmlElement]$element) {
    switch ($element.LocalName) {
        'path' {
            $data = $element.GetAttribute('d')
            if ([string]::IsNullOrWhiteSpace($data)) {
                return $null
            }

            return [Windows.Media.Geometry]::Parse($data).Clone()
        }
        'rect' {
            $x = Get-Double $element 'x'
            $y = Get-Double $element 'y'
            $width = Get-Double $element 'width'
            $height = Get-Double $element 'height'
            if ($width -le 0 -or $height -le 0) {
                return $null
            }

            $rx = Get-Double $element 'rx'
            $ry = Get-Double $element 'ry' $rx
            return [Windows.Media.RectangleGeometry]::new([Windows.Rect]::new($x, $y, $width, $height), $rx, $ry)
        }
        'circle' {
            $r = Get-Double $element 'r'
            if ($r -le 0) {
                return $null
            }

            return [Windows.Media.EllipseGeometry]::new([Windows.Point]::new((Get-Double $element 'cx'), (Get-Double $element 'cy')), $r, $r)
        }
        'ellipse' {
            $rx = Get-Double $element 'rx'
            $ry = Get-Double $element 'ry'
            if ($rx -le 0 -or $ry -le 0) {
                return $null
            }

            return [Windows.Media.EllipseGeometry]::new([Windows.Point]::new((Get-Double $element 'cx'), (Get-Double $element 'cy')), $rx, $ry)
        }
        'polygon' {
            return Convert-PointsToGeometry (Get-Points ($element.GetAttribute('points'))) $true
        }
        'polyline' {
            return Convert-PointsToGeometry (Get-Points ($element.GetAttribute('points'))) $false
        }
        default {
            return $null
        }
    }
}

function Convert-PointsToGeometry([Collections.Generic.List[Windows.Point]]$points, [bool]$closed) {
    if ($points.Count -lt 2) {
        return $null
    }

    $geometry = [Windows.Media.StreamGeometry]::new()
    $context = $geometry.Open()
    try {
        $context.BeginFigure($points[0], $true, $closed)
        for ($i = 1; $i -lt $points.Count; $i++) {
            $context.LineTo($points[$i], $true, $false)
        }
    }
    finally {
        $context.Close()
    }

    return $geometry
}

function Get-RenderableElements([xml]$svg) {
    $elements = @(Select-Xml -Xml $svg -Namespace $svgNamespace -XPath '//*[local-name()="path" or local-name()="rect" or local-name()="circle" or local-name()="ellipse" or local-name()="polygon" or local-name()="polyline"]')
    $renderable = @()
    foreach ($element in $elements.Node) {
        if ((Get-Fill $svg $element) -and (Convert-ElementToGeometry $element)) {
            $renderable += $element
        }
    }

    return $renderable
}

function Convert-SvgToDrawingImage([string]$svgPath, [string]$xamlPath, [string]$pngPath) {
    [xml]$svg = Get-Content -LiteralPath $svgPath -Raw
    $viewBoxTransform = Get-ViewBoxTransform $svg
    $viewBoxMatrix = $viewBoxTransform.Value
    $elements = @(Get-RenderableElements $svg)
    $xamlDrawings = [Collections.Generic.List[string]]::new()

    if ($elements.Count -eq 0) {
        throw "No renderable SVG elements found in $svgPath"
    }

    $drawingGroup = [Windows.Media.DrawingGroup]::new()
    $context = $drawingGroup.Open()
    try {
        foreach ($element in $elements) {
            $geometry = Convert-ElementToGeometry $element
            $fill = Get-Fill $svg $element
            if ($null -eq $geometry -or [string]::IsNullOrWhiteSpace($fill)) {
                continue
            }

            $escapedGeometry = [Security.SecurityElement]::Escape($geometry.ToString([Globalization.CultureInfo]::InvariantCulture))
            $svgMatrix = Get-SvgTransform $element
            $totalMatrix = Multiply-Matrix $svgMatrix $viewBoxMatrix
            $geometry.Transform = [Windows.Media.MatrixTransform]::new($totalMatrix)
            $brush = [Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($fill))
            $brush.Freeze()
            $context.DrawGeometry($brush, $null, $geometry)

            $escapedFill = [Security.SecurityElement]::Escape($fill)
            $matrix = $totalMatrix
            $transformText = ('{0},{1},{2},{3},{4},{5}' -f
                $matrix.M11.ToString([Globalization.CultureInfo]::InvariantCulture),
                $matrix.M12.ToString([Globalization.CultureInfo]::InvariantCulture),
                $matrix.M21.ToString([Globalization.CultureInfo]::InvariantCulture),
                $matrix.M22.ToString([Globalization.CultureInfo]::InvariantCulture),
                $matrix.OffsetX.ToString([Globalization.CultureInfo]::InvariantCulture),
                $matrix.OffsetY.ToString([Globalization.CultureInfo]::InvariantCulture))
            $xamlDrawings.Add("      <GeometryDrawing Brush=`"$escapedFill`"><GeometryDrawing.Geometry><PathGeometry Figures=`"$escapedGeometry`" Transform=`"$transformText`" /></GeometryDrawing.Geometry></GeometryDrawing>")
        }
    }
    finally {
        $context.Close()
    }

    $drawingImage = [Windows.Media.DrawingImage]::new($drawingGroup)
    $drawingImage.Freeze()

    $xamlDirectory = Split-Path -Parent $xamlPath
    $pngDirectory = Split-Path -Parent $pngPath
    New-Item -ItemType Directory -Force -Path $xamlDirectory, $pngDirectory | Out-Null

    $xamlLines = @(
        '<?xml version="1.0" encoding="utf-8"?>'
        '<DrawingImage xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">'
        '  <DrawingImage.Drawing>'
        '    <DrawingGroup>'
    ) + $xamlDrawings + @(
        '    </DrawingGroup>'
        '  </DrawingImage.Drawing>'
        '</DrawingImage>'
    )
    $xaml = $xamlLines -join [Environment]::NewLine
    [IO.File]::WriteAllText($xamlPath, $xaml, [Text.UTF8Encoding]::new($false))

    $visual = [Windows.Media.DrawingVisual]::new()
    $visualContext = $visual.RenderOpen()
    try {
        $visualContext.DrawDrawing($drawingGroup)
    }
    finally {
        $visualContext.Close()
    }

    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(16, 16, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)

    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.File]::Create($pngPath)
    try {
        $encoder.Save($stream)
    }
    finally {
        $stream.Dispose()
    }
}

$generatedRoot = Join-Path $Root 'GeneratedImages'
$pngRoot = Join-Path $Root 'GeneratedImagesPng'
$catalogRoot = Join-Path $Root 'ImageCatalogResources'

foreach ($relativeFolder in @('folders', 'foldersOpen')) {
    $sourceFolder = Join-Path (Join-Path $Root 'assets\icons') $relativeFolder
    $xamlFolderName = if ($relativeFolder -eq 'foldersOpen') { 'foldersOpen' } else { 'folders' }
    $pngFolderName = if ($relativeFolder -eq 'foldersOpen') { 'foldersopen' } else { 'folders' }

    foreach ($svg in Get-ChildItem -LiteralPath $sourceFolder -Filter '*.svg' | Sort-Object Name) {
        $xamlPath = Join-Path (Join-Path $generatedRoot $xamlFolderName) ($svg.BaseName + '.xaml')
        $pngPath = Join-Path (Join-Path $pngRoot $pngFolderName) ($svg.BaseName + '.png')
        $catalogPath = Join-Path (Join-Path $catalogRoot $pngFolderName) ($svg.BaseName + '.16.16.png')

        Convert-SvgToDrawingImage $svg.FullName $xamlPath $pngPath
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $catalogPath) | Out-Null
        Copy-Item -LiteralPath $pngPath -Destination $catalogPath -Force
    }
}

$closedCount = (Get-ChildItem -LiteralPath (Join-Path $pngRoot 'folders') -Filter '*.png').Count
$openCount = (Get-ChildItem -LiteralPath (Join-Path $pngRoot 'foldersopen') -Filter '*.png').Count
"closed=$closedCount"
"open=$openCount"
