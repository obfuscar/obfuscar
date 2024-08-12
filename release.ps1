$msBuild = "msbuild"
try
{
    & $msBuild /version
    Write-Host "Likely on Linux/macOS."
}
catch
{
    Write-Host "MSBuild doesn't exist. Use VSSetup instead."

    Install-Module VSSetup -Scope CurrentUser -Force
    $instance = Get-VSSetupInstance -All | Select-VSSetupInstance -Product * -Latest
    $installDir = $instance.installationPath
    $msBuild = $installDir + '\MSBuild\Current\Bin\MSBuild.exe'
    if (!(Test-Path $msBuild))
    {
        $msBuild = $installDir + '\MSBuild\15.0\Bin\MSBuild.exe'
        if (!(Test-Path $msBuild))
        {
            $instance = Get-VSSetupInstance -All -Prerelease | Select-VSSetupInstance -Latest
            $installDir = $instance.installationPath
            $msBuild = $installDir + '\MSBuild\Current\Bin\MSBuild.exe'
            if (!(Test-Path $msBuild))
            {
                $msBuild = $installDir + '\MSBuild\15.0\Bin\MSBuild.exe'
                if (!(Test-Path $msBuild))
                {
                    Write-Host "MSBuild doesn't exist. Exit."
                    exit 1
                }
                else
                {
                    Write-Host "Likely on Windows with VS2017 Preview."
                }
            }
            else
            {
                Write-Host "Likely on Windows with VS2019 Preview."
            }
        }
        else
        {
            Write-Host "Likely on Windows with VS2017."
        }
    }
    else
    {
        Write-Host "Likely on Windows with VS2019."
    }
}

Write-Host "MSBuild found. Compile the projects."

& $msBuild obfuscar.sln /p:Configuration=Release /t:restore
& $msBuild obfuscar.sln /p:Configuration=Release /t:clean
& $msBuild obfuscar.sln /p:Configuration=Release

Write-Host "Compilation finished."