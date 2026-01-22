dotnet tool install --global GitVersion.Tool
# Compose MajorMinorPatch with PreReleaseTagWithDash so prerelease labels like '-beta.1' are preserved
$majorMinorPatch = (dotnet-gitversion /showvariable MajorMinorPatch | Out-String).Trim()
$pre = (dotnet-gitversion /showvariable PreReleaseTagWithDash | Out-String).Trim()
$version = "$majorMinorPatch$pre"
(Get-Content -path .\Obfuscar.nuspec.txt -Raw) -replace ([regex]::Escape('$(GitVersion_NuGetVersion)')),$version | Set-Content -path '.\Obfuscar.nuspec'
