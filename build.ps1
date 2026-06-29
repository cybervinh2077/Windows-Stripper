# Biên dịch WindowsStriper bằng csc.exe có sẵn trong .NET Framework
# Tạo 2 bản:
#   WindowsStriper.exe       - bản thật (thực thi xoá bản quyền)
#   WindowsStriper-TEST.exe  - bản TEST (mọi thao tác đều chỉ giả lập)
$csc  = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$root = $PSScriptRoot

$common = @(
    "/target:winexe",
    "/win32manifest:$root\app.manifest",
    "/win32icon:$root\fpt.ico",
    "/reference:System.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Management.dll",
    "/optimize+",
    "/nologo"
)

Write-Host "Đang biên dịch bản THẬT..." -ForegroundColor Cyan
& $csc $common "/out:$root\WindowsStriper.exe" "$root\Program.cs"

Write-Host "Đang biên dịch bản TEST..." -ForegroundColor Cyan
& $csc $common "/define:TEST_BUILD" "/out:$root\WindowsStriper-TEST.exe" "$root\Program.cs"

foreach ($f in @("WindowsStriper.exe", "WindowsStriper-TEST.exe")) {
    if (Test-Path "$root\$f") {
        Write-Host "  OK -> $root\$f" -ForegroundColor Green
    } else {
        Write-Host "  THẤT BẠI -> $f" -ForegroundColor Red
    }
}
