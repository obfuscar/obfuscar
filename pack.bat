set EnableNuGetPackageRestore=true
set msBuildDir=%WINDIR%\Microsoft.NET\Framework\v4.0.30319
call %MSBuildDir%\msbuild obfuscar.sln /p:Configuration=Release
CALL .\.nuget\nuget.exe pack 