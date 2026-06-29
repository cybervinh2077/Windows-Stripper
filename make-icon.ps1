# Chuyển fpt.png -> fpt.ico (đa kích thước, nền trong suốt, giữ tỉ lệ)
Add-Type -AssemblyName System.Drawing
$root = $PSScriptRoot
$src  = Join-Path $root "fpt.png"
$dst  = Join-Path $root "fpt.ico"

$source = [System.Drawing.Image]::FromFile($src)
$sizes = @(16, 32, 48, 64, 128, 256)

# Tạo PNG vuông cho từng size (logo căn giữa trên nền trong suốt)
$pngBlobs = @()
foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    # Giữ tỉ lệ: scale theo cạnh dài nhất
    $ratio = [Math]::Min($s / $source.Width, $s / $source.Height)
    $w = [int]($source.Width * $ratio)
    $h = [int]($source.Height * $ratio)
    $x = [int](($s - $w) / 2)
    $y = [int](($s - $h) / 2)
    $g.DrawImage($source, $x, $y, $w, $h)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBlobs += ,($ms.ToArray())
    $bmp.Dispose()
    $ms.Dispose()
}
$source.Dispose()

# Ghi cấu trúc file ICO
$fs = New-Object System.IO.FileStream($dst, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter($fs)

# ICONDIR
$bw.Write([UInt16]0)               # reserved
$bw.Write([UInt16]1)               # type = icon
$bw.Write([UInt16]$sizes.Count)    # số ảnh

# Offset bắt đầu sau header (6) + các directory entry (16 mỗi cái)
$offset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $len = $pngBlobs[$i].Length
    $dim = if ($s -ge 256) { 0 } else { $s }   # 256 -> ghi 0
    $bw.Write([Byte]$dim)          # width
    $bw.Write([Byte]$dim)          # height
    $bw.Write([Byte]0)             # palette
    $bw.Write([Byte]0)             # reserved
    $bw.Write([UInt16]1)           # color planes
    $bw.Write([UInt16]32)          # bits per pixel
    $bw.Write([UInt32]$len)        # kích thước dữ liệu
    $bw.Write([UInt32]$offset)     # offset
    $offset += $len
}
# Dữ liệu PNG
foreach ($blob in $pngBlobs) { $bw.Write($blob) }

$bw.Flush(); $bw.Close(); $fs.Close()
Write-Host "Tao xong: $dst"
