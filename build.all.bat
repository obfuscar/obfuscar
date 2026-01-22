pwsh -ExecutionPolicy Bypass -file pre.build.ps1
if %errorlevel% neq 0 exit /b %errorlevel%
pwsh -ExecutionPolicy Bypass -file release.ps1
if %errorlevel% neq 0 exit /b %errorlevel%
