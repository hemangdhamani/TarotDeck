param(
  [Parameter(Mandatory=$true)][ValidateSet('Export','Import')][string]$Action,
  [Parameter(Mandatory=$true)][string]$Path
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$keyPath=Join-Path $env:LOCALAPPDATA 'TarotDeck\BuildKeys\asset-signing.dpapi'
$Path=[IO.Path]::GetFullPath($Path)
if ($Path.StartsWith($repo.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase) -or $Path.StartsWith('G:\My Drive\JD YT Software\',[StringComparison]::OrdinalIgnoreCase)) {
  throw 'Keep signing-key backups outside the source tree and public software download folder.'
}
if ($Action -eq 'Export' -and (Test-Path -LiteralPath $Path)) { throw 'Refusing to overwrite an existing backup.' }
$secure=Read-Host 'Backup password (use a strong unique password)' -AsSecureString
$pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try { $password=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer); $secure.Dispose() }
if ($password.Length -lt 16) { throw 'Use a backup password of at least 16 characters.' }
$rng=[Security.Cryptography.RandomNumberGenerator]::Create()
$aes=[Security.Cryptography.Aes]::Create()
$plain=$null; $key=$null; $derived=$null; $mac=$null
try {
  if ($Action -eq 'Export') {
    $plain=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    $salt=New-Object byte[] 16; $rng.GetBytes($salt)
  } else {
    $data=Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    if ($data.version -ne 1) { throw 'Unknown backup format.' }
    $salt=[Convert]::FromBase64String($data.salt)
    if ($salt.Length -ne 16) { throw 'Invalid backup salt.' }
  }
  $derived=[Security.Cryptography.Rfc2898DeriveBytes]::new($password,$salt,200000,[Security.Cryptography.HashAlgorithmName]::SHA256)
  $key=$derived.GetBytes(64)
  $aes.Key=[byte[]]$key[0..31]
  $mac=[Security.Cryptography.HMACSHA256]::new([byte[]]$key[32..63])
  if ($Action -eq 'Export') {
    $aes.GenerateIV()
    $transform=$aes.CreateEncryptor()
    try { $cipher=$transform.TransformFinalBlock($plain,0,$plain.Length) } finally { $transform.Dispose() }
    $tag=$mac.ComputeHash([byte[]]($salt+$aes.IV+$cipher))
    $json=@{version=1;salt=[Convert]::ToBase64String($salt);iv=[Convert]::ToBase64String($aes.IV);ciphertext=[Convert]::ToBase64String($cipher);tag=[Convert]::ToBase64String($tag)} | ConvertTo-Json
    [IO.File]::WriteAllText($Path,$json)
    Write-Output 'Encrypted private backup created. Keep the password separate; never publish this backup.'
  } else {
    $iv=[Convert]::FromBase64String($data.iv); $cipher=[Convert]::FromBase64String($data.ciphertext); $tag=[Convert]::FromBase64String($data.tag)
    $expected=$mac.ComputeHash([byte[]]($salt+$iv+$cipher)); $difference=$tag.Length -bxor $expected.Length
    if ($tag.Length -eq $expected.Length) { for($i=0;$i -lt $tag.Length;$i++) { $difference=$difference -bor ($tag[$i] -bxor $expected[$i]) } }
    if ($difference -ne 0) { throw 'Wrong password or modified backup.' }
    $aes.IV=$iv; $transform=$aes.CreateDecryptor()
    try { $plain=$transform.TransformFinalBlock($cipher,0,$cipher.Length) } finally { $transform.Dispose() }
    $rsa=[Security.Cryptography.RSACryptoServiceProvider]::new(); $rsa.PersistKeyInCsp=$false
    try {
      $rsa.FromXmlString([Text.Encoding]::UTF8.GetString($plain))
      $pinned=[IO.File]::ReadAllText((Join-Path $repo 'TarotDeck\Assets\AssetPublicKey.xml')).Trim()
      if ($rsa.ToXmlString($false) -ne $pinned) { throw 'Backup does not match the pinned publisher identity.' }
    } finally { $rsa.Dispose() }
    if (Test-Path -LiteralPath $keyPath) { throw 'A local signing key already exists. Preserve it; restoration will not overwrite it.' }
    $null=New-Item -ItemType Directory -Force -Path (Split-Path $keyPath -Parent)
    $protected=[Security.Cryptography.ProtectedData]::Protect($plain,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    [IO.File]::WriteAllBytes($keyPath,$protected)
    Write-Output 'Publisher signing identity restored for this Windows account.'
  }
} finally {
  if($plain) { [Array]::Clear($plain,0,$plain.Length) }
  if($key) { [Array]::Clear($key,0,$key.Length) }
  $password=$null
  if($derived) { $derived.Dispose() }; if($mac) { $mac.Dispose() }
  $aes.Dispose(); $rng.Dispose()
}
