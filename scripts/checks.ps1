$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
& dotnet run --project (Join-Path $root 'tests/AdaptaPE.Checks/AdaptaPE.Checks.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Comprobaciones fallidas.' }
