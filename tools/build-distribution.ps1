param([string]$OutputDirectory, [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $repo 'artifacts\distribution\1.0.0' }
$null = New-Item -ItemType Directory -Force -Path $OutputDirectory
$OutputDirectory = (Resolve-Path $OutputDirectory).Path
$compiler = Join-Path $repo '.tools\nsis-3.13\makensis.exe'
if (!(Test-Path $compiler)) { throw 'Install the official portable NSIS 3.13 under .tools/nsis-3.13 first.' }
if (!$SkipBuild) {
  $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
  $msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
  if (!$msbuild) { throw 'Visual Studio MSBuild is required.' }
  & $msbuild (Join-Path $repo 'TarotDeck\TarotDeck.csproj') /p:Configuration=Release /p:CopyRetryCount=0 /verbosity:minimal
  if ($LASTEXITCODE) { throw 'Application build failed.' }
}
$source = Join-Path $repo 'TarotDeck\bin\Release'
if (Test-Path (Join-Path $source 'images')) { throw 'Loose output artwork is not permitted.' }
$guide = Join-Path $OutputDirectory 'README.txt'
@'
TarotDeck 1.0.0 - Windows desktop

Install TarotDeck-Setup-1.0.0.exe first. Requires Windows and .NET Framework 4.8.
The base installer includes The Illustrated Arcana (78 fronts) and all 21
card backs. Emerald Labyrinth is the initial default back.

Optional TarotDeck-Artwork-*.exe installers add one complete 78-card deck.
Close TarotDeck before installing/updating a package. Restart the app and
open Settings > Cards and artwork > Choose artwork deck to select it.
Artwork files install under the application's assets folder in Program Files.
Installers require administrator permission; normal app use does not.
Saved readings and preferences remain in your user profile when uninstalling.

Package artwork is encrypted and publisher-signed, decoded in memory without
loose image files. This discourages casual copying and detects changed assets;
it cannot prevent a determined user from extracting displayed artwork.
Third-party artwork retains its original licence permissions and attribution.
See THIRD-PARTY-NOTICES.txt installed with the base app.

These installers are not Authenticode signed. Windows may show an unknown
publisher warning. Compare SHA256SUMS.txt before installation.
Download sizes are listed in DOWNLOAD-SIZES.csv (MB uses 1,000,000 bytes).
'@ | Set-Content -LiteralPath $guide -Encoding utf8
$script = Join-Path $repo 'installer\setup.nsi'
function Build-Installer([string]$name, [string]$title, [string]$deck) {
  $arguments = @('/V2', "/DPRODUCT_NAME=$title", "/DOUTPUT_FILE=$(Join-Path $OutputDirectory $name)", "/DSOURCE_DIR=$source", "/DGUIDE_FILE=$guide")
  if ($deck) { $arguments += @('/DADDON', "/DDECK_ID=$deck") }
  & $compiler @arguments $script
  if ($LASTEXITCODE) { throw "Installer compilation failed: $name" }
}
Build-Installer 'TarotDeck-Setup-1.0.0.exe' 'TarotDeck 1.0.0' ''
Get-ChildItem -LiteralPath (Join-Path $source 'assets') -Filter '*.deck' | Where-Object { $_.BaseName -notin @('cardbacks','the-illustrated-arcana') } | Sort-Object Name | ForEach-Object {
  Build-Installer ("TarotDeck-Artwork-" + $_.BaseName + '-1.0.0.exe') ("TarotDeck artwork - " + $_.BaseName) $_.BaseName
}
$files = @(Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.exe' | Sort-Object Name)
$files | Select-Object Name,Length,@{N='MB';E={[math]::Round($_.Length / 1e6,2)}},@{N='MiB';E={[math]::Round($_.Length / 1MB,2)}} | Export-Csv -LiteralPath (Join-Path $OutputDirectory 'DOWNLOAD-SIZES.csv') -NoTypeInformation
$files | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $_.Name } | Set-Content -LiteralPath (Join-Path $OutputDirectory 'SHA256SUMS.txt') -Encoding ascii
$files | Select-Object Name,Length
