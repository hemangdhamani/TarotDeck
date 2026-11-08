# Woodcut tarot

A complete 78-card Tarot de Marseille in SVG, traced from the oldest surviving
deck of the pattern. CC0.

![The full deck](preview.png)

### Cards

<p>
<img width="115" src="cards/00-fool.svg"> <img width="115" src="cards/01-magician.svg"> <img width="115" src="cards/13-death.svg"> <img width="115" src="cards/15-devil.svg"> <img width="115" src="cards/17-star.svg"> <img width="115" src="cards/21-world.svg">
</p>
<p>
<img width="115" src="cards/cups-queen.svg"> <img width="115" src="cards/swords-ace.svg"> <img width="115" src="cards/wands-07.svg"> <img width="115" src="cards/coins-02.svg"> <img width="115" src="cards/cups-02.svg"> <img width="115" src="cards/back.svg">
</p>

### Colori

<p>
<img width="115" src="colori/00-fool.svg"> <img width="115" src="colori/10-wheel-of-fortune.svg"> <img width="115" src="colori/19-sun.svg"> <img width="115" src="colori/cups-queen.svg"> <img width="115" src="colori/swords-ace.svg"> <img width="115" src="colori/cups-02.svg">
</p>

`colori/` holds the same 78 cards in full stencil colour. The deck's own
palette was extracted from the scans — vermilion, ochre, and the Noblet's
steel-grey wash — and every pixel is classified to its nearest ink, each
plate traced separately and stacked beneath the keyline in the order the
press printed them. The small red ovals are the BnF's ownership stamps,
left in place as part of the deck's history.

73 cards are traced from the **Jean Noblet tarot** (Paris, c. 1650), the
oldest Tarot de Marseille that survives. The BnF's copy is missing five cards
— the Six through Ten of Wands — which are traced instead from the **Jacques
Viéville tarot**, cut in Paris in the same years. The back is original vector
work after the star lattice on the Noblet's own backs. Everything is flat
single-colour path data; retint with a find-and-replace.

```
cards/       00-fool.svg … 21-world.svg, wands-ace.svg … coins-king.svg, back.svg
sources/     traced artwork as SVG fragments + the tracing recipe (PIPELINE.md)
tools/       generate.mjs — rebuilds cards/ from the fragments
```

Cards are `viewBox="0 0 250 430"`, self-contained. Captions use the deck's own
numbering (Justice VIII, Strength XI; IIII-style numerals as printed on the
cards). Rebuild: `node tools/generate.mjs`.

## Sources

| Cards | Source | Date | Scan |
|---|---|---|---|
| 73 of 78 | Tarot de Jean Noblet, Paris (stencilled woodcut; BnF RESERVE KH-34) | c. 1650 | [Gallica](https://gallica.bnf.fr/ark:/12148/btv1b105109641) |
| Six–Ten of Wands | Tarot de Jacques Viéville, Paris (stencilled woodcut) | c. 1650 | [Gallica](https://gallica.bnf.fr/ark:/12148/btv1b10510963k) |

Both scans are digitised and published by the Bibliothèque nationale de
France. The Noblet album also binds in four period replacement cards from an
unrelated deck (duplicate low Cups); they are not part of this set. Method:
[`sources/PIPELINE.md`](sources/PIPELINE.md).

## License

[CC0 1.0](LICENSE). The sources' copyrights expired centuries ago; this
repository's own contributions are dedicated to the public domain.

Sibling deck: [woodcut-cards](https://github.com/SONDLecT/woodcut-cards).
First made for [Dicebox](https://dicebox.cc).
