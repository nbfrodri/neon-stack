param([switch]$Listen)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe' }
$output = Join-Path $PSScriptRoot 'artifacts'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources = @('src\Audio.cs', 'src\AudioMixer.cs', 'src\WaveOutput.cs', 'tools\AudioCheck.cs') | ForEach-Object { (Get-Item -LiteralPath (Join-Path $PSScriptRoot $_)).FullName }
& $compiler /nologo /target:exe /warnaserror+ /codepage:65001 "/out:$output/AudioCheck.exe" /reference:System.dll /reference:System.Core.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar la prueba de audio.' }
if ($Listen) { & "$output/AudioCheck.exe" --listen } else { & "$output/AudioCheck.exe" }
if ($LASTEXITCODE -ne 0) { throw 'Fallo en la prueba de audio.' }
