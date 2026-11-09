# Artwork packaging and distribution

The application remains .NET Framework 4.8 / Windows Forms. This feature does
not migrate the UI or claim macOS/Linux support.

## Installed layout

`TarotDeck.exe` contains the publisher's public verification key. Each front
deck is one `assets/<deck-id>.deck` file. `assets/cardbacks.deck` contains all
21 independent backs. The base installer includes The Illustrated Arcana
and the card-back package. Emerald Labyrinth remains the default back.
The 14 optional front installers install beside these two core packages.
Restart the app after adding/removing a package. Settings discovers installed
signed front packages; it does not list uninstalled decks as available.

No image files are installed in an `images` directory. Existing image paths
are logical keys, keeping saved readings compatible. Images and display-bound
metadata are read from packages in memory. Source PNG/JPG files remain intact
and visible in the source project; they are never copied by the app build.

## Integrity and limits

Each package is a ZIP container with encrypted entries, a SHA-256 manifest,
and an RSA-3072/SHA-256 signature. Startup checks each installed manifest and
package identity against the public key embedded in the EXE. Each entry's
ciphertext hash is checked before decrypting it. Unknown publishers, modified
manifests, substituted package identities, missing/unlisted entries and
modified payloads are rejected. Missing core packs prevent startup.

AES encryption raises the effort required for casual extraction. Its key must
exist in an offline client, so this is not DRM or a guarantee against copying.
A determined user can extract rendered images or patch the executable.
Third-party artwork retains its original licence permissions. Installed
THIRD-PARTY-NOTICES.txt preserves attribution and licence text outside the
encrypted assets. Packages do not implement subscription access.

## Build

Use Visual Studio MSBuild with .NET Framework 4.8 reference assemblies. The
application's post-build target runs tools/AssetPackager. It builds changed
packages and removes only the generated legacy output `images` directory.
`assets/catalog.txt` and its signature are build inventories; optional-package
discovery verifies each package independently without updating the inventory.

The private signing key is DPAPI-protected for the build user's Windows account:
`%LOCALAPPDATA%/TarotDeck/BuildKeys/asset-signing.dpapi`.
It is outside the repository and is never copied to installers or Drive.
Do not delete it or replace the pinned public key when producing updates.
Other developers can compile the code but cannot issue trusted artwork updates
without this signing identity. Initialize is only for establishing a NEW identity.

Use `tools/asset-signing-backup.ps1 -Action Export -Path <private-backup>`
to make a password-encrypted portable backup. Restore with `-Action Import` on
another Windows build account. Keep the backup/password separate and outside
the source repository, public Google Drive folder and release downloads.

Download official portable NSIS 3.13 from SourceForge and extract it under
`.tools/nsis-3.13`. The downloaded `nsis-3.13.zip` SHA-256 was
`ba63dffc4410ee89193e1cb5a41989991bd77c61068da17e3156d136b7b0b3d8`.
Run `tools/build-distribution.ps1`. Output is under
`artifacts/distribution/1.0.0`, with a README, download-size CSV and SHA-256 list.
Each optional installer requires the base install and installs into its
registered location. Close the app before installing/updating (open packages
are locked). Uninstall removes installed packages and leaves user settings
and reading journals in the user profile.

The installers currently have NO Authenticode certificate. Package signatures
are distinct from Windows publisher signing. Obtain a code-signing certificate
and sign the finished EXEs before a public commercial release if trusted
publisher identity / fewer SmartScreen warnings are required.

## Validation

`tests/run-offline-reading-tests.ps1` runs the existing reading, artwork,
layout and image-decoding regression suite plus AssetPackageTests. The latter
checks tampering, wrong publishers/identities, unlisted entries, and isolated
core-only installation; adds/removes an optional deck; and checks missing-core
failure. `tests/test-installers.ps1` compiles the same installer file operations
with a test-only switch that suppresses elevation, registry and shortcut writes;
it verifies base install, optional install, exact payload hashes, in-memory
loading and uninstall in a unique temporary directory. Production elevation,
registry and Start-menu behavior still require an administrator smoke test
before release to paying users. The signing-key backup helper's syntax was
checked, but its live-key round-trip test was blocked by automatic review.
