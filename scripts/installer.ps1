param([string]$MakeNsis = 'makensis')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
[xml]$project = Get-Content (Join-Path $root 'src/AdaptaPE/AdaptaPE.csproj')
$version = $project.Project.PropertyGroup.Version
$fileVersion = ([version]$project.Project.PropertyGroup.FileVersion).ToString()
$payload = Join-Path $root 'artifacts/net10.0-windows-win-x64-contained'
$prerequisite = Join-Path $root 'assets/prerequisites/VC_redist.x64.exe'
if (!(Test-Path $prerequisite)) { throw 'Falta VC_redist.x64.exe oficial. Consulta README y assets/prerequisites/manifest.json.' }
$expected = (Get-Content (Join-Path $root 'assets/prerequisites/manifest.json') -Raw | ConvertFrom-Json).sha256
if ((Get-FileHash $prerequisite -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'Visual C++ no coincide con su hash registrado.' }
& dotnet publish (Join-Path $root 'src/AdaptaPE/AdaptaPE.csproj') -f net10.0-windows -c Release -r win-x64 --self-contained true -o $payload
if ($LASTEXITCODE -ne 0) { throw 'Publicación fallida.' }
$packageCache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages' }
$frameworks = (Get-Content (Join-Path $payload 'AdaptaPE.runtimeconfig.json') -Raw | ConvertFrom-Json).runtimeOptions.includedFrameworks
foreach ($framework in $frameworks) {
    $package = Join-Path $packageCache ($framework.name.ToLowerInvariant() + '.runtime.win-x64/' + $framework.version)
    $licenseFolder = Join-Path $payload ('licenses/' + $framework.name)
    New-Item $licenseFolder -ItemType Directory -Force | Out-Null
    Get-ChildItem $package -File | Where-Object { $_.Name -match '^LICENSE|THIRD-PARTY-NOTICES' } | Copy-Item -Destination $licenseFolder
}
$deleteList = Join-Path $root 'artifacts/uninstall-files.nsh'
$lines = @(Get-ChildItem $payload -Recurse -File | ForEach-Object {
    $relative = $_.FullName.Substring($payload.Length + 1).Replace('/', '\').Replace('$', '$$')
    'Delete "$INSTDIR\' + $relative + '"'
})
$lines += @(Get-ChildItem $payload -Recurse -Directory | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
    'RMDir "$INSTDIR\' + $_.FullName.Substring($payload.Length + 1).Replace('/', '\').Replace('$', '$$') + '"'
})
Set-Content $deleteList $lines -Encoding utf8
$output = Join-Path $root "artifacts/AdaptaPE-$version-Setup-x64.exe"
& $MakeNsis "-DVERSION=$version" "-DFILE_VERSION=$fileVersion" "-DPAYLOAD=$payload" "-DOUTPUT=$output" "-DICON=$root/assets/brand/AdaptaPE.ico" "-DLICENSE=$root/LICENSE.txt" "-DVCREDIST=$prerequisite" "-DDELETE_LIST=$deleteList" (Join-Path $PSScriptRoot 'installer.nsi')
if ($LASTEXITCODE -ne 0) { throw 'Falló NSIS.' }
Write-Host "Instalador: $output"
