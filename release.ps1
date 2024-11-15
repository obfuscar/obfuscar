[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $Configuration = "Release"
)
try
{
    & dotnet --version
    & dotnet restore
    & dotnet clean -c $Configuration
    & dotnet build -c $Configuration
}
catch
{
    Write-Host ".NET SDK doesn't exist. Exit."
    exit 1
}

if ($LASTEXITCODE -ne 0)
{
    Write-Host "Compilation failed. Exit."
    exit $LASTEXITCODE
}

Write-Host "Compilation finished."
