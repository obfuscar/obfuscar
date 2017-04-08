@echo off

for /f "usebackq tokens=*" %%i in (`vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
  set InstallDir=%%i
)

if exist "%InstallDir%\MSBuild\15.0\Bin\MSBuild.exe" (
  set msBuildExe="%InstallDir%\MSBuild\15.0\Bin\MSBuild.exe"
)
@echo on

set EnableNuGetPackageRestore=true
CALL .\.nuget\nuget.exe restore obfuscar.sln
call %msBuildExe% obfuscar.sln /p:Configuration=Release
CALL .\.nuget\nuget.exe pack 