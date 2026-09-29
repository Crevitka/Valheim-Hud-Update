$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.Drawing
$modDir = Split-Path -Parent $PSScriptRoot
$dll = Join-Path $modDir 'bin/Release/HUD-Update.dll'
$manifestPath = Join-Path $PSScriptRoot 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$version = $manifest.version_number
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid semantic version' }
if ($manifest.name -notmatch '^[a-zA-Z0-9_]{1,128}$') { throw 'Invalid package name' }
if ($manifest.description.Length -gt 250) { throw 'Description exceeds 250 characters' }
if ($null -eq $manifest.website_url) { throw 'Missing website_url' }
if ($manifest.dependencies.Count -ne 1 -or $manifest.dependencies[0] -ne 'denikson-BepInExPack_Valheim-5.4.2202') { throw 'Unexpected dependencies' }
$plugin = Get-Content -LiteralPath (Join-Path $modDir 'HudUpdatePlugin.cs') -Raw
if ($plugin -notmatch ('\[BepInPlugin\("crevitka\.hudupdate", "HUD Update", "' + [regex]::Escape($version) + '"\)\]')) { throw 'Plugin version mismatch' }
if ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll).FileVersion -ne "$version.0") { throw 'DLL version mismatch: rebuild first' }
$png = [System.Drawing.Image]::FromFile((Join-Path $PSScriptRoot 'icon.png'))
try { if ($png.Width -ne 256 -or $png.Height -ne 256 -or $png.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid) { throw 'Icon must be a 256x256 PNG' } } finally { $png.Dispose() }
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$report = [System.Collections.Generic.List[string]]::new()
$report.Add("HUD Update $version - package validation")
$report.Add('Author: Crevitka | License: MIT')
$report.Add('Manifest fields, description length, icon format/size, plugin and DLL versions: PASS')

function New-VerifiedArchive([string]$name, [System.Collections.IDictionary]$entries) {
    $path = Join-Path $dist $name
    $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::Create)
    $zip = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entryName in $entries.Keys) {
            $source = $entries[$entryName]
            if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing $source" }
            $entry = $zip.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
            $dest = $entry.Open()
            $inputFile = [System.IO.File]::OpenRead($source)
            try { $inputFile.CopyTo($dest) } finally { $inputFile.Dispose(); $dest.Dispose() }
        }
    } finally { $zip.Dispose(); $stream.Dispose() }
    $checkStream = [System.IO.File]::OpenRead($path)
    $check = [System.IO.Compression.ZipArchive]::new($checkStream, [System.IO.Compression.ZipArchiveMode]::Read)
    try {
        if ($check.Entries.Count -ne $entries.Count) { throw 'Unexpected ZIP contents' }
        foreach ($entry in $check.Entries) {
            if (!$entries.Contains($entry.FullName) -or $entry.FullName.Contains('\') -or $entry.FullName.Contains('..')) { throw 'Unexpected archive entry' }
            $reader = $entry.Open()
            $hasher = [System.Security.Cryptography.SHA256]::Create()
            try { $hash = [BitConverter]::ToString($hasher.ComputeHash($reader)).Replace('-','') } finally { $reader.Dispose(); $hasher.Dispose() }
            if ($hash -ne (Get-FileHash -LiteralPath $entries[$entry.FullName] -Algorithm SHA256).Hash) { throw "Corrupt ZIP entry: $($entry.FullName)" }
            $report.Add("  $($entry.FullName) [$($entry.Length) bytes] OK")
        }
    } finally { $check.Dispose(); $checkStream.Dispose() }
    $report.Add("$name : PASS")
    $report.Add("SHA256: $((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)")
}
$common = [ordered]@{
    'README.md' = (Join-Path $PSScriptRoot 'README.md')
    'README.ru.md' = (Join-Path $PSScriptRoot 'README.ru.md')
    'CHANGELOG.md' = (Join-Path $PSScriptRoot 'CHANGELOG.md')
    'LICENSE' = (Join-Path $PSScriptRoot 'LICENSE')
}
$thunderstore = [ordered]@{}
foreach ($key in $common.Keys) { $thunderstore[$key] = $common[$key] }
$thunderstore['manifest.json'] = $manifestPath
$thunderstore['icon.png'] = Join-Path $PSScriptRoot 'icon.png'
$thunderstore['plugins/HUD-Update/HUD-Update.dll'] = $dll
New-VerifiedArchive "HUD_Update-$version-Thunderstore.zip" $thunderstore
$nexus = [ordered]@{}
foreach ($key in $common.Keys) { $nexus[$key] = $common[$key] }
$nexus['BepInEx/plugins/HUD-Update/HUD-Update.dll'] = $dll
New-VerifiedArchive "HUD-Update-$version-Nexus.zip" $nexus
$source = [ordered]@{
    'HudUpdatePlugin.cs' = (Join-Path $modDir 'HudUpdatePlugin.cs')
    'TmpFontFixPatch.cs' = (Join-Path $modDir 'TmpFontFixPatch.cs')
    'build-plugin.ps1' = (Join-Path $modDir 'build-plugin.ps1')
    'Bundles/hudPrefab' = (Join-Path $modDir 'Bundles/hudPrefab')
    'IconSource/generate_food_icons.py' = (Join-Path $modDir 'IconSource/generate_food_icons.py')
    'IconSource/chevron.py' = (Join-Path $modDir 'IconSource/chevron.py')
    'IconSource/figma_subtract_path.txt' = (Join-Path $modDir 'IconSource/figma_subtract_path.txt')
    'README.md' = (Join-Path $PSScriptRoot 'SOURCE-README.md')
    'LICENSE' = (Join-Path $PSScriptRoot 'LICENSE')
}
foreach ($icon in Get-ChildItem -LiteralPath (Join-Path $modDir 'Icons') -Filter '*.png' | Sort-Object Name) {
    $source["Icons/$($icon.Name)"] = $icon.FullName
}
New-VerifiedArchive "HUD-Update-$version-Source.zip" $source
$report.Add('Only explicitly listed release files included; no game/BepInEx assemblies, player logs, configs or backups.')
$report.Add('Validation is local, not an online platform upload validation or a full in-game visual test.')
[System.IO.File]::WriteAllLines((Join-Path $dist 'VALIDATION.txt'), $report, [System.Text.UTF8Encoding]::new($false))
$report
