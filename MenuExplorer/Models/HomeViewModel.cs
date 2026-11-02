/* ==========================================================================
 *  Menu Explorer — HomeViewModel
 * ==========================================================================
 *  Role    Everything Views/Home/Index.cshtml needs: the stats panel and
 *          today's specials.
 *  Layer   Models. Constructed by HomeController.Index; no data access.
 *  Debug   Today comes from the injected TimeProvider, so the date shown in the
 *          eyebrow, the rotation offset and the specials are always the same
 *          three values — never two different notions of "now".
 * ========================================================================== */

using System.Globalization;

namespace MenuExplorer.Models;

/// <summary>View model for <c>Views/Home/Index.cshtml</c>.</summary>
/// <example>
/// <code>
/// IReadOnlyList&lt;MenuItem&gt; allItems = menuService.GetAll();
/// DateOnly today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
///
/// new HomeViewModel
/// {
///     AllItems = allItems,
///     Specials = menuService.GetSpecials(today, count: 3),
///     Today = today
/// };
/// </code>
/// Derived values on the shipped 16-item menu: <c>ItemCount</c> 16, <c>CategoryCount</c> 4,
/// <c>AverageRatingDisplay</c> "3.9", <c>TodayDisplay</c> "Thursday 29 October".
/// </example>
public sealed class HomeViewModel
{
    /// <summary>Every item on the menu, used for the at-a-glance statistics.</summary>
    public required IReadOnlyList<MenuItem> AllItems { get; init; }

    /// <summary>Today's rotation, as produced by <see cref="Services.IMenuService.GetSpecials"/>.</summary>
    /// <remarks>Empty when nothing is rated 4 or higher; the view then shows an empty state.</remarks>
    public required IReadOnlyList<MenuItem> Specials { get; init; }

    /// <summary>The day the specials were rotated for, used for the eyebrow line.</summary>
    public required DateOnly Today { get; init; }

    /// <summary>Number of dishes on the menu.</summary>
    public int ItemCount => AllItems.Count;

    /// <summary>Number of sections actually represented in the data.</summary>
    /// <remarks>
    /// Distinct over the items rather than the enum's length, so a section with no
    /// dishes is not counted as if it were on the menu.
    /// </remarks>
    public int CategoryCount => AllItems.Select(item => item.Category).Distinct().Count();

    /// <summary>Number of vegetarian dishes.</summary>
    public int VegetarianCount => AllItems.Count(item => item.IsVegetarian);

    /// <summary>Mean rating to one decimal place, or an en dash when the menu is empty.</summary>
    /// <remarks>Invariant culture so the separator is a dot on every host.</remarks>
    public string AverageRatingDisplay =>
        AllItems.Count == 0
            ? "–"
            : AllItems.Average(item => item.Rating).ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>The date as rendered in the specials eyebrow, e.g. <c>Thursday 29 October</c>.</summary>
    public string TodayDisplay => Today.ToString("dddd d MMMM", CultureInfo.InvariantCulture);
}