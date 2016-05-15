CALL ..\..\pack.bat

set EnableNuGetPackageRestore=true
set msBuildDir=%WINDIR%\Microsoft.NET\Framework\v4.0.30319
call %MSBuildDir%\msbuild BasicExample.sln /p:Configuration=Release /p:OutDir=..\Obfuscator_Input

CALL ..\..\bin\Release\Obfuscar.Console.exe obfuscar.xml
