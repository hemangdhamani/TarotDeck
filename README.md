# TarotDeck

A Windows Forms tarot card picker built with .NET Framework 4.8.

## Choose a deck

Use the selector in the upper left to choose from:

- **Complete Tarot**: 78 cards (Major and Minor Arcana)
- **Major Arcana**: 22 cards
- **Minor Arcana**: 56 cards

The selection applies immediately and starts a fresh, shuffled deck. All choices use the included card artwork.

## Add another card artwork set

The current build uses one image set in `TarotDeck/images`. To replace that artwork, keep the existing filenames and provide `cardback.jpg` plus one image for each card: `cups01.jpg` through `cups14.jpg`, `pents01.jpg` through `pents14.jpg`, `swords01.jpg` through `swords14.jpg`, `wands01.jpg` through `wands14.jpg`, and `maj01.jpg` through `maj22.jpg`. Images are loaded from the app's `images` folder at runtime.

To add a separately selectable artwork set, extend the deck selector in `TarotDeck/frmTarot.cs` with a named set, store its files in a separate image folder, and route `LoadCardImage` and the card-back loader to that set. Keep card filenames aligned with the names generated in `TarotDeck/TarotDeck/Deck.cs`.

## Build

Open `TarotDeck.sln` in Visual Studio and build the `TarotDeck` project. The project targets .NET Framework 4.8 and x86.
