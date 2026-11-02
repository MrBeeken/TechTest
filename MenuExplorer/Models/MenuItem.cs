/* ==========================================================================
 *  Menu Explorer — MenuItem
 * ==========================================================================
 *  Role    The domain entity: one dish or drink on the menu.
 *  Layer   Models. Populated exclusively from Data/menu.json at runtime.
 *  Debug   Immutable after construction (init-only + required), so a rendering
 *          bug can never be caused by an item changing mid-request. Validation
 *          lives in MenuJsonReader, not here: this type describes shape only.
 * ========================================================================== */

using System.Globalization;

namespace MenuExplorer.Models;

/// <summary>
/// A single dish or drink on the menu. Populated exclusively from
/// <c>Data/menu.json</c> at runtime — there is no hardcoded item list anywhere
/// in the codebase.
/// </summary>
/// <example>
/// The JSON this type maps from (every property except <c>isVegetarian</c> is
/// required; see <see cref="Services.MenuJsonReader"/> for the rules):
/// <code>
/// {
///   "id": 10,
///   "name": "Malva Pudding",
///   "description": "The classic warm apricot sponge drenched in caramel sauce.",
///   "price": 75.00,
///   "category": "Desserts",
///   "isVegetarian": true,
///   "rating": 5
/// }
/// </code>
/// The equivalent instance, as used by the unit tests:
/// <code>
/// MenuItem item = new MenuItem
/// {
///     Id = 10,
///     Name = "Malva Pudding",
///     Description = "Warm apricot sponge.",
///     Price = 75.00m,
///     Category = MenuCategory.Desserts,
///     IsVegetarian = true,
///     Rating = 5
/// };
///
/// string display = item.PriceDisplay;   // "R75.00"
/// </code>
/// </example>
public sealed class MenuItem
{
    /// <summary>Stable identifier used in URLs (<c>/Menu/Details/5</c>).</summary>
    /// <remarks>Must be positive and unique; both rules are enforced at load.</remarks>
    public int Id { get; init; }

    /// <summary>Display name, e.g. <c>Dry-Aged Sirloin</c>. Must not be blank.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// One- to two-sentence description shown on the card and details page. Must
    /// not be blank; it is also indexed by the client-side search.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>Price in South African rand, excluding any currency symbol.</summary>
    /// <remarks>Must be zero or greater; rendered through <see cref="PriceDisplay"/>.</remarks>
    public decimal Price { get; init; }

    /// <summary>Section the item belongs to, from the closed <see cref="MenuCategory"/> set.</summary>
    public required MenuCategory Category { get; init; }

    /// <summary>
    /// File name of the dish photograph inside <c>wwwroot/images/menu/</c>, e.g.
    /// <c>10-malva-pudding.jpg</c>. Optional: without it the card falls back to the
    /// decorative category glyph.
    /// </summary>
    /// <remarks>
    /// A bare file name, never a path: <c>MenuJsonReader</c> rejects anything with a
    /// directory separator or <c>..</c>, and <see cref="Services.JsonMenuService"/>
    /// verifies the file exists at startup, so a typo fails the host rather than
    /// rendering a broken image.
    /// </remarks>
    public string? ImageFile { get; init; }

    /// <summary>Whether the dish is suitable for vegetarians. Defaults to <c>false</c>.</summary>
    public bool IsVegetarian { get; init; }

    /// <summary>Editorial rating out of five. Validated to the range 1–5 on load.</summary>
    /// <seealso cref="MenuItemRatings"/>
    public int Rating { get; init; }

    /// <summary>
    /// The dish photograph's URL path, e.g. <c>/images/menu/10-malva-pudding.jpg</c>,
    /// or <c>null</c> when the item has no image.
    /// </summary>
    /// <remarks>
    /// Composed here rather than in the view so the folder is named once. The file
    /// name itself comes from <see cref="ImageFile"/> and is validated at load.
    /// </remarks>
    public string? ImageUrl => ImageFile is null ? null : $"/images/menu/{ImageFile}";

    /// <summary>
    /// Zulu rand price ready for display, e.g. <c>R95.00</c>.
    /// </summary>
    /// <remarks>
    /// The app is single-currency by design, so the symbol is fixed and the
    /// numeric part is formatted with the invariant culture to keep the output
    /// identical regardless of the host machine's regional settings. If the app
    /// ever becomes multi-currency this moves to a formatter/resource lookup.
    /// </remarks>
    public string PriceDisplay => string.Create(CultureInfo.InvariantCulture, $"R{Price:0.00}");
}