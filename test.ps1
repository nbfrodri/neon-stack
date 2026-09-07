$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe' }
$output = Join-Path $PSScriptRoot 'artifacts'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | Where-Object { $_.Name -ne 'Program.cs' } | ForEach-Object { $_.FullName }
$sources += (Get-Item -LiteralPath (Join-Path $PSScriptRoot 'tests\Tests.cs')).FullName
& $compiler /nologo /target:exe /optimize+ /warnaserror+ /codepage:65001 "/out:$output/Tests.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll /reference:System.Xml.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'No se han podido compilar las pruebas.' }
& "$output/Tests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Hay pruebas fallidas.' }
