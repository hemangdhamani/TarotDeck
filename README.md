# TarotDeck

A Windows Forms tarot card picker built with .NET Framework 4.8.

![TarotDeck showing the centered card spread and deck choices](docs/tarotdeck-screenshot.jpg)

## Deck options

Choose front artwork and a card back independently in Settings:

- **Artwork deck:** Classic Tarot, Soimoi Tarot, Woodcut Marseille, Tarot de Viéville, and Lequart Besançon (included). Artwork and back choices are remembered for the next launch.
- **Card back:** Ten independent designs; Emerald Labyrinth is the default. Changing the back preserves drawn cards and fan slots.
- **Add deck…:** Import a complete folder of all 78 front images. A back is not required.
- **Auto spread:** Spread automatically after shuffling, or turn it off to drag the top card manually.
- Card selection is currently hidden; the existing saved card-set preference is retained.

### Image names for custom decks

Put the images in one folder using these card filenames (the app also accepts `.jpeg`, `.png`, or `.bmp` extensions):

- `maj01` through `maj22`
- `cups01` through `cups14`
- `pents01` through `pents14`
- `swords01` through `swords14`
- `wands01` through `wands14`

The names are case-insensitive. In the app, click **Add deck…** and choose the folder. Imported sets are stored under `%LOCALAPPDATA%\TarotDeck\Decks` and appear in the artwork selector.

## Artwork credits

The Soimoi Tarot images are by Mike Koz and licensed under [Creative Commons Attribution 4.0](https://creativecommons.org/licenses/by/4.0/). Suggested credit: “Soimoi Tarot” by [Mike Koz](https://koz.tv/), licensed under CC BY 4.0. The source license is included at `TarotDeck/images/soimoi/LICENSE`; the filenames were adapted for this application. The custom Soimoi card back was created for TarotDeck.

Woodcut Marseille is SONDLecT's [complete CC0 woodcut deck](https://github.com/SONDLecT/woodcut-tarot), traced from historical Jean Noblet and Jacques Vieville cards. Its full-color SVGs are bundled as 1000×1720 PNGs. Source URLs, canonical filename mappings, hashes, and the CC0 license are in `TarotDeck/images/woodcut-marseille/`. No attribution display is required for CC0; provenance remains bundled. Marseille's printed Justice VIII and Strength XI are mapped by card identity, not printed number. Reading meanings remain shared Rider-Waite-Smith-based interpretations rather than a historical Marseille-specific system.

Tarot de Viéville (Paris, circa 1650) includes all 78 unmodified full-resolution BnF/Gallica scans, obtained through [Wikimedia Commons and the TarotCards archive](https://github.com/mixvlad/TarotCards/blob/main/SOURCES.md). Its unusual printed trump order is mapped by identity: VII Justice, VIII Chariot, IX Strength, XI Hermit. Per-card public-domain evidence, URLs, and image hashes are bundled in `TarotDeck/images/vieville/`.

Lequart Besançon (Paris, circa 1890) includes 78 full-resolution public-domain PNGs from the same archive and Commons, with individual license records and verified source hashes in `TarotDeck/images/lequart-besancon/`. This historical design uses Juno II and Jupiter V in the traditional Priestess and Hierophant roles. Those conventional role equivalents are explicit in the provenance; printed imagery and numbering remain unchanged. Page and Knight images are mapped by their actual identities. The reading engine uses shared RWS meanings for both historical decks, without claiming deck-specific visual interpretations.

## Offline readings

1. Open **Reading setup** to choose a spread, topic, and focus. There is no free-form question field and no AI or online service.
2. Pick three reading cards from the fan, or use **Draw Cards**. The outer first/last cards are separate from the three reading positions.
3. Choose **Read Cards** to interpret the currently displayed page from left to right. Position labels show the selected spread.
4. Read the overview, open each card's detailed tab, and optionally record reflection notes. Notes never affect the interpretation.
5. **Save reading** stores a snapshot on this desktop; **Export text…** writes a plain-text copy. **Journal** reopens or deletes saved readings.

All 78 cards have original upright meanings. The engine uses deterministic position, topic, focus, and card-pattern rules. No credentials, model downloads, payments, or network access are needed. Reversals and physical-card entry are outside this release.

Readings are symbolic guidance, with conditional possible directions rather than fixed predictions. Opening analysis does not move, redraw, rotate, or shuffle cards. Saved interpretations retain their original content version even after a later library update.

Journal files live under `%LOCALAPPDATA%\TarotDeck\Readings`. Notes stay local. The history window reports unreadable files without deleting them. Thumbnails use the original artwork path and gracefully fall back to card names if the image is unavailable.

The implementation plan is in [docs/offline-reading-plan.md](docs/offline-reading-plan.md).

## Regression checks

Run `powershell -NoProfile -ExecutionPolicy Bypass -File tests/run-offline-reading-tests.ps1`.
The runner builds an isolated x86 test output, then checks all 234 newly bundled faces, full content coverage, deterministic interpretations, invalid inputs, local journal round trips, and automatic/manual draw integration. HTTP requests are rejected by a test handler throughout the run. It does not modify the user's journal.

## Build and run

Open `TarotDeck.sln` in Visual Studio and build the `TarotDeck` project. It targets .NET Framework 4.8 and x86. Card images are copied beside the executable during build.
