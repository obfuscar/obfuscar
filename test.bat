cd Tests\bin\Release
"%USERPROFILE%\.nuget\packages\xunit.runner.console\2.4.1\tools\net461\xunit.console.exe" ObfuscarTests.dll -appveyor
"%USERPROFILE%\.nuget\packages\xunit.runner.console\2.4.1\tools\net461\xunit.console.x86.exe" ObfuscarTests.dll -appveyor
cd ..\..\..
