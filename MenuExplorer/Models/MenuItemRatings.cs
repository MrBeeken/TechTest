/* ==========================================================================
 *  Menu Explorer — MenuItemRatings
 * ==========================================================================
 *  Role    The single source of truth for the allowed rating range.
 *  Layer   Models. Constants only.
 *  Debug   Shared by the loader's validation and the star-rating clamp, so a
 *          rating the data rejects can never be one the view renders differently.
 * ========================================================================== */

namespace MenuExplorer.Models;

/// <summary>
/// Allowed range for <see cref="MenuItem.Rating"/>, shared by the loader's
/// validation and the <c>star-rating</c> tag helper.
/// </summary>
/// <remarks>
/// Kept as two constants rather than a range type because both consumers want a
/// bound, not a collection: the reader rejects values outside it, the tag helper
/// clamps to it.
/// </remarks>
/// <example>
/// <code>
/// if (rating is &lt; MenuItemRatings.Min or &gt; MenuItemRatings.Max)
/// {
///     throw new MenuDataException($"rating {rating}; expected {MenuItemRatings.Min}–{MenuItemRatings.Max}.");
/// }
/// </code>
/// </example>
public static class MenuItemRatings
{
    /// <summary>Lowest rating the data may contain, and the clamp floor for display.</summary>
    public const int Min = 1;

    /// <summary>Highest rating the data may contain, and the number of stars rendered.</summary>
    public const int Max = 5;
}