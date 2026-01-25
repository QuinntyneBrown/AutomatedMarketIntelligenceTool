# PowerShell script to generate PNG images from PlantUML files using the PlantUML web service

function ConvertTo-PlantUmlEncoding {
    param([string]$Content)

    # Compress using Deflate
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Content)
    $ms = New-Object System.IO.MemoryStream
    $ds = New-Object System.IO.Compression.DeflateStream($ms, [System.IO.Compression.CompressionMode]::Compress)
    $ds.Write($bytes, 0, $bytes.Length)
    $ds.Close()
    $compressed = $ms.ToArray()

    # Encode using PlantUML's custom base64 encoding
    $encode64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_"
    $result = New-Object System.Text.StringBuilder

    for ($i = 0; $i -lt $compressed.Length; $i += 3) {
        $b1 = $compressed[$i]
        $b2 = if ($i + 1 -lt $compressed.Length) { $compressed[$i + 1] } else { 0 }
        $b3 = if ($i + 2 -lt $compressed.Length) { $compressed[$i + 2] } else { 0 }

        $c1 = $b1 -shr 2
        $c2 = (($b1 -band 0x3) -shl 4) -bor ($b2 -shr 4)
        $c3 = (($b2 -band 0xF) -shl 2) -bor ($b3 -shr 6)
        $c4 = $b3 -band 0x3F

        [void]$result.Append($encode64[$c1])
        [void]$result.Append($encode64[$c2])
        if ($i + 1 -lt $compressed.Length) { [void]$result.Append($encode64[$c3]) }
        if ($i + 2 -lt $compressed.Length) { [void]$result.Append($encode64[$c4]) }
    }

    return $result.ToString()
}

# Find all .puml files in src directory
$pumlFiles = Get-ChildItem -Path "src" -Recurse -Filter "*.puml"

Write-Host "Found $($pumlFiles.Count) PlantUML files to process"

foreach ($file in $pumlFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $encoded = ConvertTo-PlantUmlEncoding -Content $content

    $pngPath = $file.FullName -replace '\.puml$', '.png'
    $url = "http://www.plantuml.com/plantuml/png/$encoded"

    Write-Host "Generating: $($file.Name) -> $(Split-Path $pngPath -Leaf)"

    try {
        Invoke-WebRequest -Uri $url -OutFile $pngPath -UseBasicParsing
        Write-Host "  Success: $pngPath" -ForegroundColor Green
    }
    catch {
        Write-Host "  Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nDone! Generated PNG files for all PlantUML diagrams."
