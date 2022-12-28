$foundCert = Test-Certificate -Cert Cert:\CurrentUser\my\8ef9a86dfd4bd0b4db313d55c4be8b837efa7b77 -User
if(!$foundCert)
{
    Write-Host "Certificate doesn't exist. Exit."
    exit
}

Write-Host "Certificate found. Sign the assemblies."
$signtool = ${Env:ProgramFiles(x86)} + "\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"
& $signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a .\bin\release\obfuscar.console.exe | Write-Debug

Write-Host "Verify digital signature."
& $signtool verify /pa /q .\bin\release\obfuscar.console.exe 2>&1 | Write-Debug
if ($LASTEXITCODE -ne 0)
{
    Write-Host "$_.FullName is not signed. Exit."
    exit $LASTEXITCODE
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

Write-Host "Sign NuGet packages."
& $nuget sign *.nupkg -CertificateSubjectName "Yang Li" -Timestamper http://timestamp.digicert.com | Write-Debug
& $nuget verify -All *.nupkg | Write-Debug
if ($LASTEXITCODE -ne 0)
{
    Write-Host "NuGet package is not signed. Exit."
    exit $LASTEXITCODE
}

Write-Host "Verification finished."
