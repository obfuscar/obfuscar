Install-Module VSSetup -Scope CurrentUser -Force
$instance = Get-VSSetupInstance -All | Select-VSSetupInstance -Require 'Microsoft.Component.MSBuild' -Latest
$installDir = $instance.installationPath
$msBuild = $installDir + '\MSBuild\15.0\Bin\MSBuild.exe'
if (![System.IO.File]::Exists($msBuild))
{
    Write-Host "MSBuild doesn't exist. Exit."
    exit 1
}

Write-Host "MSBuild found. Compile the projects."

& $msBuild obfuscar.sln /p:Configuration=Release /t:restore
& $msBuild obfuscar.sln /p:Configuration=Release /t:clean
& $msBuild obfuscar.sln /p:Configuration=Release

Write-Host "Compilation finished."