$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$managedDir = "D:\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed"
# Defaults to the current user's Klei mod folder. Override if the game keeps mods elsewhere
# (for example a OneDrive redirected Documents folder).
$devModDir = Join-Path $env:USERPROFILE "Documents\Klei\OxygenNotIncluded\mods\Dev\LargeStorageLocker"

$releaseDir = Join-Path $projectRoot "bin\Release"
New-Item -ItemType Directory -Force $releaseDir | Out-Null

$outputDll = Join-Path $releaseDir "LargeStorageLocker.dll"
$sourceDir = Join-Path $projectRoot "src"
$sources = @(Get-ChildItem -Path $sourceDir -Filter *.cs -Recurse -File | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) {
    throw "No C# sources found under $sourceDir"
}

& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
    /nologo `
    /target:library `
    "/out:$outputDll" `
    "/reference:$(Join-Path $managedDir "netstandard.dll")" `
    "/reference:$(Join-Path $managedDir "0Harmony.dll")" `
    "/reference:$(Join-Path $managedDir "Assembly-CSharp.dll")" `
    "/reference:$(Join-Path $managedDir "Assembly-CSharp-firstpass.dll")" `
    "/reference:$(Join-Path $managedDir "UnityEngine.CoreModule.dll")" `
    $sources
if ($LASTEXITCODE -ne 0) {
    throw "csc build failed with exit code $LASTEXITCODE"
}

New-Item -ItemType Directory -Force $devModDir | Out-Null
Copy-Item -Force (Join-Path $releaseDir "LargeStorageLocker.dll") $devModDir
Copy-Item -Force (Join-Path $projectRoot "mod.yaml") $devModDir
Copy-Item -Force (Join-Path $projectRoot "mod_info.yaml") $devModDir

Write-Host "Installed dev mod to $devModDir"
