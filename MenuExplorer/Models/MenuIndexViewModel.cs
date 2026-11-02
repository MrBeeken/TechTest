/* ==========================================================================
 *  Menu Explorer — MenuIndexViewModel
 * ==========================================================================
 *  Role    Everything Views/Menu/Index.cshtml needs, and nothing else.
 *  Layer   Models. Constructed by MenuController.Index; no data access.
 *  Debug   Category is the *resolved* section (null means "All"), while
 *          UnknownCategory is the raw value that failed to resolve. Keeping them
 *          separate is what lets the page show a fallback notice and still
 *          highlight the right pill.
 * ========================================================================== */

namespace MenuExplorer.Models;

/// <summary>View model for <c>Views/Menu/Index.cshtml</c>.</summary>
/// <example>
/// The two shapes the controller produces:
/// <code>
/// // GET /Menu
/// new MenuIndexViewModel
/// {
///     Items = menuService.GetAll(),        // 16 items
///     Category = null,                     // heading: "The full menu"
///     UnknownCategory = null,
///     TotalItemCount = 16
/// };
///
/// // GET /Menu?category=Burgers
/// new MenuIndexViewModel
/// {
///     Items = menuService.GetAll(),        // full menu, not empty
///     Category = null,                     // resolved category is "All"
///     UnknownCategory = "Burgers",         // drives the notice banner
///     TotalItemCount = 16
/// };
/// </code>
/// </example>
public sealed class MenuIndexViewModel
{
    /// <summary>Items to render, already filtered by <see cref="Category"/>.</summary>
    public required IReadOnlyList<MenuItem> Items { get; init; }

    /// <summary>The category currently filtered on, or <c>null</c> for "All".</summary>
    /// <remarks>
    /// Set only when the query string named a real category. An unrecognised
    /// value leaves this <c>null</c>, because the page really is showing everything.
    /// </remarks>
    public MenuCategory? Category { get; init; }

    /// <summary>
    /// Set when the query string contained a category that does not exist, so
    /// the page can explain why it fell back to the full menu.
    /// </summary>
    /// <remarks>
    /// Truncated by the controller at the request boundary; the view only ever
    /// sees a short value, and Razor HTML-encodes it on the way out.
    /// </remarks>
    public string? UnknownCategory { get; init; }

    /// <summary>Total number of items on the menu, regardless of filtering.</summary>
    /// <remarks>
    /// Also emitted as <c>data-total-items</c> on the grid so the client-side
    /// search counter keeps reporting the menu-wide total on a filtered page.
    /// </remarks>
    public int TotalItemCount { get; init; }

    /// <summary>Page heading and document title fragment, e.g. <c>Desserts</c> or <c>The full menu</c>.</summary>
    public string Heading => Category?.ToString() ?? "The full menu";

    /// <summary>Whether the filtered result has anything to show.</summary>
    public bool HasItems => Items.Count > 0;
}