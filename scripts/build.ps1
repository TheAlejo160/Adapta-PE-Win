param(
    [ValidateSet('net10.0-windows','net48','net461')][string]$Framework = 'net10.0-windows',
    [ValidateSet('win-x64','win-x86')][string]$Runtime = 'win-x64',
    [switch]$SelfContained
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src/AdaptaPE/AdaptaPE.csproj'
if ($Framework -ne 'net10.0-windows' -and $SelfContained) { throw '.NET Framework requiere runtime instalado; no admite publicación autocontenida.' }
$platform = if ($Runtime -eq 'win-x86') { 'x86' } else { 'x64' }
$out = Join-Path $root "artifacts/$Framework-$Runtime"
if ($Framework -ne 'net10.0-windows') {
    & dotnet build $project -f $Framework -c Release "-p:PlatformTarget=$platform" -o $out
} else {
    $contained = if ($SelfContained) { 'true' } else { 'false' }
    & dotnet publish $project -f $Framework -c Release -r $Runtime --self-contained $contained "-p:PlatformTarget=$platform" -o $out
}
if ($LASTEXITCODE -ne 0) { throw 'Compilación fallida.' }
Write-Host "Aplicación y modelos: $out"
