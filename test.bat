cd src\Tests
dotnet test
if %errorlevel% neq 0 exit /b %errorlevel%
