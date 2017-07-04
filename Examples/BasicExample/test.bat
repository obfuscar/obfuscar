echo Step 1, build obfuscar command line tool from source code.
call ..\..\dist.pack.bat

echo Step 2, build sample project.

@echo off

for /f "usebackq tokens=*" %%i in (`..\..\vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
  set InstallDir=%%i
)

if exist "%InstallDir%\MSBuild\15.0\Bin\MSBuild.exe" (
  set msBuildExe="%InstallDir%\MSBuild\15.0\Bin\MSBuild.exe"
)
@echo on

set EnableNuGetPackageRestore=true
call %MSBuildExe% BasicExample.sln /p:Configuration=Release /p:OutDir=..\Obfuscator_Input

echo Step 3, execute obfuscation.
call ..\..\bin\Release\Obfuscar.Console.exe obfuscar.xml
