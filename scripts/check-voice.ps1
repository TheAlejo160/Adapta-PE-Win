param([string]$Executable = "$PSScriptRoot/../artifacts/net10.0-windows-win-x64-contained/AdaptaPE.exe")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
& "$PSScriptRoot/check-startup.ps1" -Executable $Executable
$process = Get-Process -Name AdaptaPE | Select-Object -First 1
$window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
$condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, 'Activar voz')
$toggle = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition).GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
try {
    if ($toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::Off) { $toggle.Toggle() }
    $deadline = (Get-Date).AddSeconds(60)
    $stableSince = $null
    do {
        Start-Sleep -Milliseconds 500
        $texts = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text))
        $names = @($texts | ForEach-Object { $_.Current.Name })
        $errorText = $names | Where-Object { $_ -match '^Voz detenida:|^No se pudo.*micrófono|^Micrófono sin muestras' } | Select-Object -First 1
        if ($errorText) { throw $errorText }
        if ($toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::Off) { throw 'Voz se apagó durante la comprobación.' }
        if (!$stableSince -and ($names | Where-Object { $_ -match '^SAPI ·|^Voz local incluida ·' })) { $stableSince = Get-Date }
        if ($stableSince -and ((Get-Date) - $stableSince).TotalSeconds -ge 30) { Write-Host 'Escucha activa durante 30 segundos. Dictado real y precisión requieren una prueba hablada.'; return }
    } while ((Get-Date) -lt $deadline)
    throw 'Voz no mantuvo escucha activa durante 30 segundos.'
} finally {
    if ($toggle.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On) { $toggle.Toggle() }
}
