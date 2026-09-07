$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Se necesita .NET Framework 4.x para compilar.' }
$output = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$artifacts = Join-Path $PSScriptRoot 'artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$iconSources = @((Get-Item "$PSScriptRoot\src\AppIcon.cs").FullName, (Get-Item "$PSScriptRoot\tools\IconBuilder.cs").FullName)
& $compiler /nologo /target:exe "/out:$artifacts/IconBuilder.exe" /reference:System.Drawing.dll $iconSources
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el generador de iconos.' }
& "$artifacts/IconBuilder.exe" "$PSScriptRoot/assets/neon-stack.ico" "$artifacts/icon.png"
if ($LASTEXITCODE -ne 0) { throw 'No se pudo generar el icono.' }
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName }
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /warnaserror+ /codepage:65001 "/out:$output/NeonStack.exe" "/win32manifest:$PSScriptRoot/app.manifest" "/win32icon:$PSScriptRoot/assets/neon-stack.ico" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll /reference:System.Xml.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'La compilacion ha fallado.' }
Write-Output "Compilado: $output/NeonStack.exe"
