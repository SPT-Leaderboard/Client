param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Debug", "Release", "Beta")]
    [string]$Configuration
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$packRoot = $PSScriptRoot
$clientPluginRel = "BepInEx\plugins\SPT-Leaderboard"
$serverModRel = "SPT\user\mods\SPT-Leaderboard"
$clientDllRel = "Build\$clientPluginRel\SPTLeaderboard.Client.dll"
$clientDllFullPath = Join-Path $packRoot $clientDllRel
$serverProject = Join-Path $repoRoot "src\Server\SPTLeaderboardServer.csproj"
$globalDataPath = Join-Path $repoRoot "src\Client\Data\GlobalData.cs"
$sevenZipPath = "C:\Program Files\7-Zip\7z.exe"

function Get-Sha256HashLower {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $hashBytes = $sha256.ComputeHash($stream)
            return ([System.BitConverter]::ToString($hashBytes) -replace "-", "").ToLowerInvariant()
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

Set-Location -Path $repoRoot

Write-Host "Checking client DLL: $clientDllFullPath"
if (-not (Test-Path $clientDllFullPath)) {
    Write-Error "Client DLL not found: $clientDllFullPath"
    exit 1
}

Write-Host "Building server ($Configuration)..."
& dotnet build $serverProject -c $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Server build failed."
    exit $LASTEXITCODE
}

$serverBinRoot = Join-Path $repoRoot "src\Server\bin\$Configuration"
$serverDll = Get-ChildItem -Path $serverBinRoot -Recurse -Filter "SPTLeaderboard.Server.dll" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $serverDll) {
    Write-Error "Server DLL not found under: $serverBinRoot"
    exit 1
}

Write-Host "Using server DLL: $($serverDll.FullName)"

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($clientDllFullPath).ProductVersion
$versionClean = $version -replace "[^\d\.]", ""

$sha256 = Get-Sha256HashLower -Path $clientDllFullPath
Write-Host "SHA256 (client DLL): $sha256"

$subVersionSuffix = ""
if ($Configuration -eq "Debug" -or $Configuration -eq "Beta") {
    $subVersionLine = Get-Content $globalDataPath | Where-Object { $_ -match 'SubVersion\s*=\s*"(\d+)"' }
    if ($subVersionLine -match 'SubVersion\s*=\s*"(\d+)"') {
        $subVersion = $matches[1]
        $subVersionSuffix = "-$subVersion"
    }
    else {
        Write-Error "SubVersion not found in GlobalData.cs"
        exit 1
    }
}

$stagingRoot = Join-Path $packRoot "_FullPackStaging\$Configuration"
if (Test-Path $stagingRoot) {
    Remove-Item -Path $stagingRoot -Recurse -Force
}
$sptDir = Join-Path $stagingRoot $serverModRel
$bepInExDest = Join-Path $stagingRoot "BepInEx"
New-Item -ItemType Directory -Path $sptDir -Force | Out-Null

Copy-Item -Path $serverDll.FullName -Destination (Join-Path $sptDir "SPTLeaderboard.Server.dll") -Force

$clientBepInExSource = Join-Path $packRoot "Build\BepInEx"
if (-not (Test-Path $clientBepInExSource)) {
    Write-Error "Client BepInEx staging not found: $clientBepInExSource"
    exit 1
}
Copy-Item -Path $clientBepInExSource -Destination $bepInExDest -Recurse -Force

if ($Configuration -eq "Release") {
    $readmeSource = Join-Path $packRoot "README LA-TOS.txt"
    $readmeDestination = Join-Path $bepInExDest "plugins\SPT-Leaderboard\README LA-TOS.txt"
    $readmeDestinationDir = Split-Path $readmeDestination -Parent

    if (-not (Test-Path $readmeSource)) {
        Write-Error "README LA-TOS.txt not found: $readmeSource"
        exit 1
    }

    if (-not (Test-Path $readmeDestinationDir)) {
        New-Item -ItemType Directory -Path $readmeDestinationDir -Force | Out-Null
    }

    Copy-Item -Path $readmeSource -Destination $readmeDestination -Force
    Write-Host "Copied README LA-TOS.txt to staging."
}

if (-not (Test-Path $sevenZipPath)) {
    Write-Error "7-Zip not found at: $sevenZipPath"
    exit 1
}

$configLabel = if ($Configuration -eq "Release") { "RELEASE" } elseif ($Configuration -eq "Beta") { "BETA" } else { "DEBUG" }
$destination7z = Join-Path $packRoot "SPT_Leaderboard_${configLabel}_FULL_v${versionClean}${subVersionSuffix}.7z"
$sourceFor7z = Join-Path $stagingRoot "*"
if (Test-Path $destination7z) {
    Remove-Item -Path $destination7z -Force
}

& $sevenZipPath a -t7z -mx=9 $destination7z $sourceFor7z

if ($LASTEXITCODE -ne 0) {
    Write-Error "7-Zip failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

$hashFilePath = Join-Path (Split-Path $destination7z -Parent) "HASH.txt"
Set-Content -Path $hashFilePath -Value $sha256 -Encoding ASCII
Write-Host "Client DLL SHA256 written to: $hashFilePath"

Write-Host "Full mod archive created: $destination7z"
