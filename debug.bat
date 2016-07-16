set EnableNuGetPackageRestore=true
set msBuildDir=%WINDIR%\Microsoft.NET\Framework\v4.0.30319
CALL .\.nuget\nuget.exe restore obfuscar.sln
call %MSBuildDir%\msbuild obfuscar.sln