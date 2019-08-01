$foundCert = Test-Certificate -Cert Cert:\CurrentUser\my\43eb601ecc35ed5263141d4dc4aff9c77858451c -User
if(!$foundCert)
{
    Write-Host "Certificate doesn't exist. Exit."
    exit
}

Write-Host "Certificate found. Sign the assemblies."
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.17134.0\x64\signtool.exe"
& $signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a .\bin\release\obfuscar.console.exe | Write-Debug

Write-Host "Verify digital signature."
& $signtool verify /pa /q .\bin\release\obfuscar.console.exe 2>&1 | Write-Debug
if ($LASTEXITCODE -ne 0)
{
    Write-Host "$_.FullName is not signed. Exit."
    exit $LASTEXITCODE
}

Remove-Item -Path .\*.nupkg
$nuget = ".\.nuget\nuget.exe"
& $nuget update /self | Write-Debug
& $nuget pack

Set-Location .\GlobalTools
& dotnet pack
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
