/* ==========================================================================
 *  Menu Explorer — MenuCategoryParser
 * ==========================================================================
 *  Role    The single rule that turns an untrusted string into a MenuCategory.
 *  Layer   Models. Pure, allocation-free (apart from a trimmed copy), no I/O.
 *  Users   MenuController (query string), _CategoryNav (active pill) and
 *          MenuCategoryJsonConverter (data file). One rule, three call sites.
 *  Debug   Returns false for anything that is not a declared category name;
 *          callers decide whether that is a fallback or a startup error.
 * ========================================================================== */

namespace MenuExplorer.Models;

/// <summary>
/// Single source of truth for turning a query-string value into a
/// <see cref="MenuCategory"/>.
/// </summary>
/// <remarks>
/// <para>
/// Both <c>MenuController</c> and the shared category navigation use this, so
/// they can never disagree about which category a URL refers to.
/// </para>
/// <para>
/// It deliberately does not use <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>
/// alone: that method also accepts numeric strings and comma-separated
/// combinations, so <c>?category=1</c> would silently mean <c>Mains</c> and
/// <c>?category=Starters,Mains</c> would also mean <c>Mains</c> (because
/// <c>Starters == 0</c>). Matching declared names only keeps URLs honest: the
/// documented values filter the grid, and anything else is treated as unknown
/// and falls back to the full menu with an explanation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// if (MenuCategoryParser.TryParse(Request.Query["category"], out MenuCategory category))
/// {
///     // A real section: filter on it.
/// }
/// else
/// {
///     // Missing, blank, numeric, combined or unknown: show everything.
/// }
/// </code>
/// <c>TryParse("desserts", out _)</c> returns <c>true</c>;
/// <c>TryParse("1", out _)</c> and <c>TryParse("Starters,Mains", out _)</c>
/// return <c>false</c>.
/// </example>
public static class MenuCategoryParser
{
    /// <summary>
    /// Parses a category name, ignoring surrounding whitespace and casing.
    /// </summary>
    /// <param name="value">
    /// Raw value, typically straight from the query string. May be <c>null</c>,
    /// empty or whitespace.
    /// </param>
    /// <param name="category">
    /// The parsed category when this returns <c>true</c>, otherwise
    /// <see langword="default"/>. Always assigned, so a caller that ignores the
    /// result cannot read an uninitialised value.
    /// </param>
    /// <returns><c>true</c> only for an exact (case-insensitive) declared name.</returns>
    public static bool TryParse(string? value, out MenuCategory category)
    {
        category = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string candidate = value.Trim();

        foreach (MenuCategory definedCategory in Enum.GetValues<MenuCategory>())
        {
            if (string.Equals(definedCategory.ToString(), candidate, StringComparison.OrdinalIgnoreCase))
            {
                category = definedCategory;
                return true;
            }
        }

        return false;
    }
}