# Menu item photographs

The dish photographs live in **`MenuExplorer/wwwroot/images/menu/`**. `wwwroot` is served by
`MapStaticAssets`, so a file in that folder is reachable at `/images/menu/<file-name>`, and the
Content Security Policy already allows `img-src 'self'`.

This guide lives in `docs/` rather than inside the images folder on purpose: everything under
`wwwroot` is publicly served, and an internal naming/tooling document should not be answerable at a
URL.

## Naming convention

`{id:00}-{slug}.{ext}`, where `id` is the item's `id` in `Data/menu.json` and `slug` is the
lowercase-hyphenated name. The leading id means the folder sorts in menu order, and the file for
item 5 stays unambiguous even if a dish is renamed.

| File | Item | Section | Size |
|---|---|---|---|
| `01-butternut-sage-soup.jpg` | Butternut & Sage Soup | Starters | 51 KB |
| `02-beef-carpaccio.jpg` | Beef Carpaccio | Starters | 68 KB |
| `03-crispy-calamari.jpg` | Crispy Calamari | Starters | 72 KB |
| `04-halloumi-honey.jpg` | Halloumi & Honey | Starters | 69 KB |
| `05-dry-aged-sirloin.jpg` | Dry-Aged Sirloin | Mains | 66 KB |
| `06-karoo-lamb-sosaties.jpg` | Karoo Lamb Sosaties | Mains | 73 KB |
| `07-wild-mushroom-risotto.jpg` | Wild Mushroom Risotto | Mains | 76 KB |
| `08-grilled-line-fish.jpg` | Grilled Line Fish | Mains | 68 KB |
| `09-butter-chicken-curry.jpg` | Butter Chicken Curry | Mains | 71 KB |
| `10-malva-pudding.jpg` | Malva Pudding | Desserts | 63 KB |
| `11-dark-chocolate-fondant.jpg` | Dark Chocolate Fondant | Desserts | 74 KB |
| `12-lemon-posset.jpg` | Lemon Posset | Desserts | 59 KB |
| `13-koeksister-cheesecake.jpg` | Koeksister Cheesecake | Desserts | 67 KB |
| `14-rooibos-iced-tea.jpg` | Rooibos Iced Tea | Drinks | 76 KB |
| `15-cape-sundowner.jpg` | Cape Sundowner | Drinks | 78 KB |
| `16-single-origin-espresso.jpg` | Single-Origin Espresso | Drinks | 75 KB |

About 1.1 MB in total. The extension is not enforced — `.webp` or `.png` work too — but `.jpg` keeps
the set uniform and is what the current files use.

## Format guidance

- **Aspect ratio:** the card tile is a 4:3 box and the photograph fills it with `object-fit: cover`,
  so the master is cropped rather than stretched. The current masters are 1136 × 943 (≈ 6:5), which
  crops about 10% of the height on a card and none of the detail hero.
- **Size:** the card renders at roughly 300 px wide, even on a 2× screen, so about 1200 × 900 px is
  plenty. Keep each file under ~150 KB; the current set is 51–78 KB at JPEG quality 82.
- **Content:** photograph the dish on a plain surface with the plate centred. The card frame and
  category gradient are already decorative, so a busy background competes with them.

## How the images are wired up

1. **Data.** Each item in `Data/menu.json` carries an optional `imageFile`, e.g.
   `"imageFile": "10-malva-pudding.jpg"`. `MenuJsonReader` rejects anything that is not a bare file
   name — no directory separators, no `..` — so the composed URL cannot escape `/images/menu/`.
2. **Mapping.** `MenuItem.ImageUrl` composes `/images/menu/{ImageFile}`, so the folder is named once
   and the views never build a path. An item without `imageFile` has a null `ImageUrl`.
3. **Rendering.** `_MenuItemCard.cshtml` and `Menu/Details.cshtml` emit an `<img>` when `ImageUrl` is
   set and fall back to the category glyph when it is not. Cards are lazy and decorative; the detail
   hero is eager with `fetchpriority="high"`.
4. **Validation.** `JsonMenuService` verifies that every referenced file exists at startup. A typo
   stops the host with a `MenuDataException` naming the file, the item id and the item, rather than
   shipping a blank tile that only a browser would discover.
5. **Serving.** `MenuImageTests` fetches every mapped URL through the real pipeline and asserts a
   `200`, `image/jpeg` and a non-trivial body, and asserts that every file on disk belongs to exactly
   one item.

To add a photograph: drop `{id:00}-{slug}.jpg` in the folder, add the matching `"imageFile"` to the
item in `Data/menu.json`, and restart. A mismatch between the two fails the build of the app at
startup and fails `MenuImageTests` in the suite.

## Why the `alt` text is empty

The photograph is **decorative**, so both views render `alt=""`:

```cshtml
<img class="card__image" src="@Model.ImageUrl" alt="" loading="lazy" decoding="async" />
```

The dish name is already the link text on a card and the `h1` on the details page. A description such
as `alt="A plate of malva pudding"` would make a screen reader announce the dish twice — once as the
image, once as the heading — and slow down navigation without adding information. `alt=""` tells
assistive technology to skip the image and go straight to the text, which is the WCAG-correct
treatment for a picture that carries no meaning beyond its caption.

This only holds while the name is adjacent and always present. If the design ever showed a photo
without the name — a gallery, a social card — that image would need real alternative text, and the
mapping would need a per-item alt field in `Data/menu.json`.