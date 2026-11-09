$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repository 'TarotDeck\bin\OfflineReadingTests'
$project = Join-Path $repository 'TarotDeck\TarotDeck.csproj'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'

if (!(Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework x86 C# compiler was not found.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio MSBuild is required for .NET Framework 4.8 builds.' }
& $msbuild $project '-p:Configuration=Debug' '-p:Platform=x86' ("-p:OutDir=" + $output.Replace('\', '/') + '/') '-verbosity:minimal'
if ($LASTEXITCODE -ne 0) { throw 'Offline reading test build failed.' }

$testExe = Join-Path $output 'OfflineReadingTests.exe'
& $compiler '/nologo' '/platform:x86' '/target:exe' ("/out:" + $testExe) '/reference:System.Drawing.dll' '/reference:System.Windows.Forms.dll' '/reference:System.Xml.dll' ("/reference:" + (Join-Path $output 'TarotDeck.exe')) (Join-Path $PSScriptRoot 'OfflineReadingTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Offline reading test compilation failed.' }

& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Offline reading regression checks failed.' }

$assetTestExe = Join-Path $output 'AssetPackageTests.exe'
& $compiler '/nologo' '/platform:x86' '/target:exe' ("/out:" + $assetTestExe) '/reference:System.Drawing.dll' '/reference:System.IO.Compression.dll' '/reference:System.IO.Compression.FileSystem.dll' ("/reference:" + (Join-Path $output 'TarotDeck.exe')) (Join-Path $PSScriptRoot 'AssetPackageTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Asset package test compilation failed.' }
& $assetTestExe
if ($LASTEXITCODE -ne 0) { throw 'Asset package security and installation checks failed.' }
