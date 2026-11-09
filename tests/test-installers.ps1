# Runs the actual NSIS file installation/uninstallation code in an isolated
# temp directory. TEST_INSTALL suppresses elevation, HKLM and Start-menu writes.
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$compiler=Join-Path $repo '.tools\nsis-3.13\makensis.exe'
$source=Join-Path $repo 'TarotDeck\bin\Release'
$guide=Join-Path $repo 'artifacts\distribution\1.0.0\README.txt'
$temp=Join-Path ([IO.Path]::GetTempPath()) ('TarotDeck-InstallerTests-'+[Guid]::NewGuid().ToString('N'))
$null=New-Item -ItemType Directory -Path $temp
$install=Join-Path $temp 'installed'
try {
  foreach($kind in @('base','addon')) {
    $output=Join-Path $temp ($kind+'.exe')
    $arguments=@('/V2','/DTEST_INSTALL',"/DPRODUCT_NAME=TarotDeck installer test", "/DOUTPUT_FILE=$output", "/DSOURCE_DIR=$source", "/DGUIDE_FILE=$guide")
    if($kind -eq 'addon') { $arguments+=@('/DADDON','/DDECK_ID=soimoi') }
    & $compiler @arguments (Join-Path $repo 'installer\setup.nsi')
    if($LASTEXITCODE) { throw 'Test installer compilation failed.' }
    # NSIS /D must be the final argument; do not surround its value with quotes.
    $process=Start-Process -FilePath $output -ArgumentList @('/S',"/D=$install") -PassThru -Wait -WindowStyle Hidden
    if($process.ExitCode) { throw "Installation failed: $kind" }
    $expected=@('the-illustrated-arcana','cardbacks')
    if($kind -eq 'addon') { $expected+='soimoi' }
    foreach($id in $expected) {
      $installed=Join-Path $install "assets\$id.deck"
      if((Get-FileHash -LiteralPath $installed).Hash -ne (Get-FileHash -LiteralPath (Join-Path $source "assets\$id.deck")).Hash) { throw "Installed package differs: $id" }
    }
    if(Test-Path -LiteralPath (Join-Path $install 'images')) { throw 'Loose artwork installed.' }
    Copy-Item -LiteralPath (Join-Path $source 'AssetPackageTests.exe') -Destination (Join-Path $install 'AssetPackageTests.exe') -Force
    & (Join-Path $install 'AssetPackageTests.exe') probe $expected.Count
    if($LASTEXITCODE) { throw "Installed application could not load assets: $kind" }
    Write-Output "PASS ${kind}: installer copies exact packages and app loads them without loose images."
  }
  if((Get-FileHash -LiteralPath (Join-Path $install 'TarotDeck.exe')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $source 'TarotDeck.exe')).Hash) { throw 'Installed application differs.' }
  $process=Start-Process -FilePath (Join-Path $install 'Uninstall.exe') -ArgumentList '/S' -PassThru -Wait -WindowStyle Hidden
  if($process.ExitCode) { throw 'Uninstaller failed.' }
  for($attempt=0;$attempt -lt 40;$attempt++) {
    if(!(Test-Path -LiteralPath (Join-Path $install 'TarotDeck.exe'))) { break }
    Start-Sleep -Milliseconds 100
  }
  if(Test-Path -LiteralPath (Join-Path $install 'TarotDeck.exe')) { throw 'Uninstall left the application.' }
  if(Get-ChildItem -LiteralPath $install -Filter '*.deck' -Recurse) { throw 'Uninstall left artwork packages.' }
  Write-Output 'PASS isolated uninstall; production elevation/registry/shortcut behavior still needs an administrator smoke test.'
} finally {
  $resolved=[IO.Path]::GetFullPath($temp)
  $expectedRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\TarotDeck-InstallerTests-'
  if(!$resolved.StartsWith($expectedRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe test cleanup path.' }
  Remove-Item -LiteralPath $resolved -Recurse -Force
}
