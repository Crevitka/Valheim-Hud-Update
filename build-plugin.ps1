param(
    [string]$ValheimDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    [string]$BepInExDir = ''
)
$ErrorActionPreference = 'Stop'
$managed = Join-Path $ValheimDir 'valheim_Data\Managed'
if (-not $BepInExDir) { $BepInExDir = Join-Path $ValheimDir 'BepInEx' }
$core = Join-Path $BepInExDir 'core'
$outDir = Join-Path $PSScriptRoot 'bin\Release'
New-Item -ItemType Directory -Force $outDir | Out-Null

$sdk = Get-ChildItem 'C:\Program Files\dotnet\sdk' -Directory | Sort-Object { [version]($_.Name -replace '-.*$','') } -Descending | Select-Object -First 1
if (-not $sdk) { throw 'Install a .NET SDK (https://dotnet.microsoft.com/download)' }
$compiler = Join-Path $sdk.FullName 'Roslyn\bincore\csc.dll'

$arguments = @('-noconfig', '-nostdlib+', '-target:library', '-langversion:latest', '-optimize+', "-out:$outDir\HUD-Update.dll")
foreach ($assembly in @('mscorlib','System','System.Core','netstandard','assembly_valheim','Assembly-CSharp','UnityEngine','UnityEngine.CoreModule','UnityEngine.AssetBundleModule','UnityEngine.ImageConversionModule','UnityEngine.TextRenderingModule','UnityEngine.UI','UnityEngine.UIModule','Unity.TextMeshPro')) {
    $arguments += "-r:$managed\$assembly.dll"
}
$arguments += "-r:$core\BepInEx.dll", "-r:$core\0Harmony.dll"

# Every PNG in Icons/ is embedded as UIReforge.Icons.<file name>.
foreach ($icon in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Icons') -Filter '*.png') {
    $arguments += "-resource:$($icon.FullName),UIReforge.Icons.$($icon.Name)"
}
# The HUD panel prefab (Unity asset bundle) is embedded too.
$arguments += "-resource:$(Join-Path $PSScriptRoot 'Bundles\hudPrefab'),UIReforge.Bundles.hudPrefab"

$arguments += (Join-Path $PSScriptRoot 'HudUpdatePlugin.cs'), (Join-Path $PSScriptRoot 'TmpFontFixPatch.cs')
& dotnet $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'HUD-Update compilation failed' }
# Loose copies of the icons go next to the DLL; players replace them there.
$iconOut = Join-Path $outDir 'Icons'
New-Item -ItemType Directory -Force -Path $iconOut | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot 'Icons\*.png') -Destination $iconOut -Force
Write-Output "Built $outDir\HUD-Update.dll"
Write-Output "Copied icons to $iconOut"
