pwsh -ExecutionPolicy Bypass -file sign.ps1
if %errorlevel% neq 0 exit /b %errorlevel%
