[CmdletBinding()]
param(
    [string]$ChangeNote = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$contentDirs = @('Assemblies', 'Defs', 'Languages', 'Textures', 'Sounds', 'Patches')
$relativeFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

# Explicit metadata and runtime folders form the upload whitelist.
foreach ($relativePath in @('LoadFolders.xml', 'About/About.xml', 'About/preview.png', 'About/PublishedFileId.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $relativePath) -PathType Leaf)) {
        throw "Required package file is missing: $relativePath"
    }
    [void]$relativeFiles.Add($relativePath)
}
$publishedFileId = [IO.File]::ReadAllText((Join-Path $repoRoot 'About/PublishedFileId.txt')).Trim()
if ($publishedFileId -notmatch '^[1-9][0-9]*$') {
    throw 'PublishedFileId.txt must contain the existing Workshop item ID.'
}
[xml]$loadFolders = Get-Content -LiteralPath (Join-Path $repoRoot 'LoadFolders.xml') -Raw -Encoding UTF8
[xml]$about = Get-Content -LiteralPath (Join-Path $repoRoot 'About/About.xml') -Raw -Encoding UTF8
$sourcePrefix = $repoRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar

foreach ($version in $about.ModMetaData.supportedVersions.li) {
    $versionFolders = $loadFolders.SelectNodes("/loadFolders/v$version/li")
    if ($versionFolders.Count -eq 0) { throw "No load folders defined for $version" }
    foreach ($node in $versionFolders) {
        # Include conditional load folders too, so optional integrations are shipped.
        $folder = [IO.Path]::GetFullPath((Join-Path $repoRoot $node.InnerText.Trim()))
        if (-not $folder.StartsWith($sourcePrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Load folder must be inside the repository: $($node.InnerText)"
        }
        if (-not (Test-Path -LiteralPath $folder -PathType Container)) {
            throw "Load folder is missing: $($node.InnerText)"
        }
        foreach ($contentDir in $contentDirs) {
            $sourceDir = Join-Path $folder $contentDir
            if (-not (Test-Path -LiteralPath $sourceDir -PathType Container)) { continue }
            foreach ($file in Get-ChildItem -LiteralPath $sourceDir -File -Recurse) {
                # Assembly folders ship runtime DLLs, without symbols or build metadata.
                if ($contentDir -eq 'Assemblies' -and $file.Extension -ne '.dll') { continue }
                [void]$relativeFiles.Add($file.FullName.Substring($sourcePrefix.Length))
            }
        }
    }
    if (-not $relativeFiles.Contains("$version\Assemblies\MoreBetterDeepDrill.dll")) {
        throw "Build the $version core DLL before packaging."
    }
}

# Each run gets a fresh folder: stale files cannot survive from an older export.
# The script never deletes or moves files in the development checkout.
$exportName = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$exportRoot = Join-Path $repoRoot "artifacts/workshop/$exportName"
$contentRoot = Join-Path $exportRoot 'MoreBetterDeepDrill'
New-Item -ItemType Directory -Path $contentRoot | Out-Null
$manifest = foreach ($relativePath in ($relativeFiles | Sort-Object)) {
    $source = Join-Path $repoRoot $relativePath
    $destination = Join-Path $contentRoot $relativePath
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination
    [pscustomobject]@{
        Path = $relativePath.Replace('\', '/')
        Bytes = (Get-Item -LiteralPath $destination).Length
        SHA256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    }
}
$manifest | Export-Csv -LiteralPath (Join-Path $exportRoot 'manifest.csv') -NoTypeInformation -Encoding UTF8

function ConvertTo-VdfString([string]$Value) {
    return $Value.Replace('\', '\\').Replace('"', '\"').Replace("`r", '').Replace("`n", '\n')
}
$contentVdf = ConvertTo-VdfString $contentRoot.Replace('\', '/')
$previewVdf = ConvertTo-VdfString ((Join-Path $contentRoot 'About/preview.png').Replace('\', '/'))
$noteVdf = ConvertTo-VdfString $ChangeNote
$vdfPath = Join-Path $exportRoot 'workshop.vdf'
$vdf = @"
"workshopitem"
{
    "appid" "294100"
    "publishedfileid" "$publishedFileId"
    "contentfolder" "$contentVdf"
    "previewfile" "$previewVdf"
    "changenote" "$noteVdf"
}
"@
[IO.File]::WriteAllText($vdfPath, $vdf, [Text.UTF8Encoding]::new($false))

[pscustomobject]@{
    ContentDirectory = $contentRoot
    UploadConfig = $vdfPath
    Manifest = Join-Path $exportRoot 'manifest.csv'
    WorkshopItemId = $publishedFileId
    FileCount = @($manifest).Count
    TotalBytes = ($manifest | Measure-Object -Property Bytes -Sum).Sum
}
