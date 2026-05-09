# Builds a single-file PDF Merger.exe in the publish folder.
# Usage: right-click → "Run with PowerShell", or run from a terminal:
#   .\publish.ps1

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $here "publish"

Write-Host "Cleaning previous publish folder..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

Write-Host "Publishing single-file exe..." -ForegroundColor Cyan
dotnet publish (Join-Path $here "PDFMerger\PDFMerger.csproj") -c Release -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

$exe = Join-Path $publishDir "PDF Merger.exe"
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)

Write-Host ""
Write-Host "Done! $exe ($sizeMb MB)" -ForegroundColor Green
Write-Host "Upload that single file to your GitHub release." -ForegroundColor Green
