if ($env:CI -eq "true") {
    exit 0
}

Remove-Item -Path .\*.nupkg
$nuget = ".\nuget.exe"

if (!(Test-Path $nuget))
{
    Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile nuget.exe
}

& $nuget update /self | Write-Debug
& $nuget pack

Set-Location .\GlobalTools
& dotnet pack -c Release
Set-Location ..

$cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | Select-Object -First 1
if ($null -eq $cert) {
    Write-Host "No code signing certificate found in MY store. Exit."
    exit 1
}

Write-Host "Sign NuGet packages."
& $nuget sign *.nupkg -CertificateSubjectName "Yang Li" -Timestamper http://timestamp.digicert.com | Write-Debug
& $nuget verify -All *.nupkg | Write-Debug
if ($LASTEXITCODE -ne 0)
{
    Write-Host "NuGet package is not signed. Exit."
    exit $LASTEXITCODE
}

Write-Host "Certificate found. Sign the assemblies."
$signtool = Get-ChildItem -Path "${env:ProgramFiles(x86)}\Windows Kits" -Recurse -Filter "signtool.exe" | Select-Object -First 1 -ExpandProperty FullName

& $signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a .\bin\release\obfuscar.console.exe | Write-Debug

Write-Host "Verify digital signature."
& $signtool verify /pa /q .\bin\release\obfuscar.console.exe 2>&1 | Write-Debug
if ($LASTEXITCODE -ne 0)
{
    Write-Host "$_.FullName is not signed. Exit."
    exit $LASTEXITCODE
}

Write-Host "Verification finished."
