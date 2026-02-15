#!/usr/bin/env pwsh
# update-version.ps1
# This script updates the version in the .csproj file based on the latest tag or commit.

param(
    [string]$ProjectFile = "src/Odland.Software.ImageSorter/ImageSorter.csproj"
)

# Get latest tag or use 0.1.0 if none
$tag = git describe --tags --abbrev=0 2>$null
if (-not $tag) { $tag = "0.1.0" }

# Remove leading 'v' if present
$version = $tag -replace "^v", ""

# Update <Version> in .csproj
[xml]$csproj = Get-Content $ProjectFile
$propertyGroup = $csproj.Project.PropertyGroup | Where-Object { $_.Version -or $_.AssemblyVersion }
if (-not $propertyGroup) {
    $propertyGroup = $csproj.CreateElement("PropertyGroup")
    $csproj.Project.AppendChild($propertyGroup) | Out-Null
}

if ($propertyGroup.Version) {
    $propertyGroup.Version = $version
} else {
    $versionElem = $csproj.CreateElement("Version")
    $versionElem.InnerText = $version
    $propertyGroup.AppendChild($versionElem) | Out-Null
}

$csproj.Save($ProjectFile)
Write-Host "Updated $ProjectFile to version $version"
