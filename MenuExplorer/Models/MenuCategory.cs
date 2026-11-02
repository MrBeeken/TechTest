/* ==========================================================================
 *  Menu Explorer — MenuCategory
 * ==========================================================================
 *  Role    The fixed set of menu sections.
 *  Layer   Models. Shared by the entity, the filter, the navigation and the JSON
 *          converter, so a new section is a one-line change plus data.
 *  Debug   Declaration order is the order the pills appear in, and
 *          Data/menu.json is sorted by this value. Starters is 0 on purpose;
 *          that is why the parser refuses numeric input (see MenuCategoryParser).
 * ========================================================================== */

namespace MenuExplorer.Models;

/// <summary>
/// The fixed set of sections a menu item can belong to.
/// </summary>
/// <remarks>
/// <para>
/// A closed enum is used instead of a free-text string so that a typo in
/// <c>Data/menu.json</c> fails fast at startup instead of silently creating a
/// phantom category that the navigation bar cannot reach. Declaration order is
/// the order the categories appear in the site's category navigation.
/// </para>
/// <para>
/// Because the values are positional, <c>Starters</c> is <c>0</c> and
/// <c>Mains</c> is <c>1</c>. Nothing in the app depends on the numeric value;
/// both the query-string parser and the JSON converter match names only.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// MenuCategory section = MenuCategory.Desserts;      // "Desserts"
///
/// if (MenuCategoryParser.TryParse("desserts", out MenuCategory parsed))
/// {
///     bool same = parsed == section;                 // true: case-insensitive
/// }
/// </code>
/// Adding a section — one line here, and the navigation, converter, parser and
/// counts all follow automatically:
/// <code>
/// public enum MenuCategory
/// {
///     Starters,
///     Mains,
///     Desserts,
///     Drinks,
///     Sides
/// }
/// </code>
/// </example>
public enum MenuCategory
{
    /// <summary>Small plates to start. No items in this section is valid.</summary>
    Starters,

    /// <summary>Main courses, usually the largest section.</summary>
    Mains,

    /// <summary>Sweet dishes.</summary>
    Desserts,

    /// <summary>Soft drinks, cocktails and coffee.</summary>
    Drinks
}