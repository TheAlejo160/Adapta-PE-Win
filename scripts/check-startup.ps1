param([string]$Executable = "$PSScriptRoot/../artifacts/net10.0-windows-win-x64/AdaptaPE.exe")
$ErrorActionPreference = 'Stop'
if (Get-Process -Name AdaptaPE -ErrorAction SilentlyContinue) { throw 'Cierra Adapta PE antes de comprobar su arranque.' }
$process = Start-Process -FilePath (Resolve-Path $Executable).Path -PassThru
$deadline = (Get-Date).AddSeconds(15)
do {
    Start-Sleep -Milliseconds 200
    $process.Refresh()
    if ($process.HasExited) { throw "Adapta PE termino durante el arranque: codigo $($process.ExitCode)." }
} while ($process.MainWindowTitle -notlike 'Adapta PE*Windows' -and (Get-Date) -lt $deadline)
if ($process.MainWindowHandle -eq 0 -or $process.MainWindowTitle -notlike 'Adapta PE*Windows') { throw 'No aparecio la ventana de ajustes en 15 segundos.' }
Write-Host 'Ventana de ajustes detectada. Comprueba Panel y bandeja; sal desde la aplicacion al terminar.'
