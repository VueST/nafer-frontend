param(
    [string]$Version = "1.2.3"
)

$ErrorActionPreference = "Stop"

$root = Get-Location
$publishDir = "$root\src\Nafer.WinUI\bin\publish"
$installerProj = "$root\src\Nafer.Installer\Nafer.Installer.wixproj"

Write-Host "--- Starting Master Build Process for Nafer v$Version ---" -ForegroundColor Cyan

# 1. Clean and Publish
Write-Host "[1/3] Preparing clean build of Nafer.WinUI (Self-Contained)..." -ForegroundColor Yellow

if (Test-Path $publishDir) { Remove-Item -Path $publishDir -Recurse -Force }

dotnet clean src\Nafer.WinUI\Nafer.WinUI.csproj -c Release -r win-x64

dotnet publish src\Nafer.WinUI\Nafer.WinUI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:WindowsAppSDKSelfContained=true `
    -p:WindowsAppSdkBootstrapInitialize=false `
    -p:PublishSingleFile=false `
    -p:BuildVersion=$Version `
    -o $publishDir

# 1.5 Verify Publish Assets
Write-Host "     Verifying critical assets..." -ForegroundColor Yellow
if (-not (Test-Path "$publishDir\resources.pri")) {
    Write-Error "CRITICAL FAILURE: resources.pri missing from publish directory! Build aborted to prevent broken MSI."
    exit 1
}
Write-Host "     Found resources.pri - Publish OK." -ForegroundColor Green

# 2. Apply "The Totoro Fix": Remove problematic localization folders
Write-Host "[2/3] Applying Totoro's cleanup trick..." -ForegroundColor Yellow
$langFolders = @("gd-gb", "mi-Nz", "ug-CN")
foreach ($lang in $langFolders) {
    if (Test-Path "$publishDir\$lang") {
        Write-Host "     Removing localized resources for: $lang"
        Remove-Item -Path "$publishDir\$lang" -Recurse -Force
    }
}

# 3. Build the MSI
Write-Host "[3/3] Compiling MSI package with Wix Toolset v4..." -ForegroundColor Yellow
dotnet build $installerProj -c Release -p:BuildVersion=$Version

Write-Host "`n--- Build Complete! ---" -ForegroundColor Green
Write-Host "Installer location: src\Nafer.Installer\bin\x64\Release\NaferSetup.msi`n" -ForegroundColor Cyan
