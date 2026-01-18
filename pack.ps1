param(
    [string]$dll = "",
    [string]$version = ""
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectRoot = $scriptDir
$pkgDir = Join-Path $projectRoot "package"

# Ensure package and plugins folders exist
if (-not (Test-Path $pkgDir)) { New-Item -ItemType Directory -Path $pkgDir | Out-Null }
$plugins = Join-Path $pkgDir "plugins"
if (-not (Test-Path $plugins)) { New-Item -ItemType Directory -Path $plugins | Out-Null }

# Copy metadata files from project root into package (if present)
$files = @("README.md","CHANGELOG.md","manifest.json","icon.png","icon.b64")
foreach ($f in $files) {
    $src = Join-Path $projectRoot $f
    $dst = Join-Path $pkgDir $f
    if (Test-Path $src) { Copy-Item $src $dst -Force }
}

# Determine DLL to include
if (-not [string]::IsNullOrWhiteSpace($dll)) {
    $sourceDll = $dll
} else {
    $sourceDll = Join-Path $projectRoot "bin\Release\netstandard2.1\smert-v-nishite.dll"
}

if (Test-Path $sourceDll) {
    Copy-Item $sourceDll $plugins -Force
} else {
    Write-Warning "DLL not found at $sourceDll — build first or pass -dll parameter."
}

# Create zip with versioned name
$ver = if (-not [string]::IsNullOrWhiteSpace($version)) { $version } else { '1.0.0' }
$zipName = "kossnikita-smert-v-nishite-$ver.zip"
$zipPath = Join-Path $projectRoot $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$pkgDir\*" -DestinationPath $zipPath -Force
Write-Output "Package created: $zipPath"
