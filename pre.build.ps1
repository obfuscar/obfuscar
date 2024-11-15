dotnet tool install --global GitVersion.Tool
$version = (dotnet-gitversion /output json /showvariable MajorMinorPatch | Out-String).TrimEnd()
(Get-Content -path .\Obfuscar.nuspec.txt -Raw) -replace ([regex]::Escape('$(GitVersion_NuGetVersion)')),$version | Set-Content -path '.\Obfuscar.nuspec'
