call build.all.bat
if %errorlevel% neq 0 exit /b %errorlevel%
call pack.all.bat
if %errorlevel% neq 0 exit /b %errorlevel%
