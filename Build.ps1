param (
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

# Auto-detect version from WinUI csproj if not provided
if ([string]::IsNullOrWhiteSpace($Version)) {
    $csprojPath = "src\Nafer.WinUI\Nafer.WinUI.csproj"
    if (Test-Path $csprojPath) {
        $xml = [xml](Get-Content $csprojPath)
        $Version = ([string]$xml.Project.PropertyGroup.Version).Trim()
        Write-Host "Auto-detected Version: $Version" -ForegroundColor Cyan
    } else {
        $Version = "1.0.0"
    }
}

$root = Get-Location
$publishDir = "$root\src\Nafer.WinUI\bin\publish"
$installerProj = "$root\src\Nafer.Installer\Nafer.Installer.wixproj"

Write-Host "`n--- Nafer Professional Build Process (v$Version) ---" -ForegroundColor Cyan

# 1. Build and Publish WinUI App
Write-Host "[1/2] Publishing Nafer.WinUI (Self-Contained)..." -ForegroundColor Yellow

# Ensure a clean start
if (Test-Path $publishDir) { Remove-Item -Path $publishDir -Recurse -Force }

dotnet publish src\Nafer.WinUI\Nafer.WinUI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:WindowsAppSDKSelfContained=true `
    -p:WindowsAppSdkBootstrapInitialize=false `
    -p:PublishSingleFile=false `
    -p:BuildVersion=$Version `
    -o $publishDir

# Verify critical assets
if (-not (Test-Path "$publishDir\resources.pri")) {
    Write-Error "CRITICAL FAILURE: Build sanity check failed (resources.pri missing)."
    exit 1
}

# 2. Apply Localization Cleanup (The Totoro Fix)
Write-Host "     Cleaning localized resources..." -ForegroundColor Yellow
$langFolders = @("gd-gb", "mi-Nz", "ug-CN")
foreach ($lang in $langFolders) {
    if (Test-Path "$publishDir\$lang") {
        Remove-Item -Path "$publishDir\$lang" -Recurse -Force
    }
}

# 3. Compile Installer
Write-Host "[2/2] Generating MSI Installer Package..." -ForegroundColor Yellow
dotnet build $installerProj -c Release -p:BuildVersion=$Version

Write-Host "`n--- Build Succeeded! ---" -ForegroundColor Green
Write-Host "Direct Launcher : src\Nafer.WinUI\bin\publish\Nafer.WinUI.exe" -ForegroundColor Cyan
Write-Host "Final Installer : src\Nafer.Installer\bin\x64\Release\Nafer_v$($Version)_x64.msi`n" -ForegroundColor Cyan
