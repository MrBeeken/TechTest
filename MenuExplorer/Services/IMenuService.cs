/* ==========================================================================
 *  Menu Explorer — IMenuService
 * ==========================================================================
 *  Role    The read-only contract every consumer depends on.
 *  Layer   Services. Deliberately narrow: no writes, no paging, no HTTP types.
 *  Debug   Every member returns a read-only view, so a stack trace pointing at
 *          a mutation of menu data is always a bug in the implementation, never
 *          in a caller that merely held the result.
 * ========================================================================== */

using MenuExplorer.Models;

namespace MenuExplorer.Services;

/// <summary>
/// Read-only access to the restaurant menu.
/// </summary>
/// <remarks>
/// The menu is immutable for the lifetime of the process, so implementations
/// are registered as singletons and may cache aggressively. Every method
/// returns a read-only view so callers cannot mutate the shared data, and
/// callers may enumerate it safely from Razor.
/// </remarks>
/// <example>
/// <code>
/// IReadOnlyList&lt;MenuItem&gt; all = menuService.GetAll();
/// IReadOnlyList&lt;MenuItem&gt; desserts = menuService.GetByCategory(MenuCategory.Desserts);
/// MenuItem? one = menuService.GetById(10);
/// IReadOnlyList&lt;MenuItem&gt; today = menuService.GetSpecials(date, count: 3);
/// </code>
/// </example>
public interface IMenuService
{
    /// <summary>All items, ordered by category then name.</summary>
    /// <returns>A read-only list that is safe to enumerate from multiple requests.</returns>
    IReadOnlyList<MenuItem> GetAll();

    /// <summary>Items in one category, or all items when <paramref name="category"/> is null.</summary>
    /// <param name="category">The section to filter on, or <c>null</c> for the full menu.</param>
    /// <returns>A read-only list; never <c>null</c>.</returns>
    IReadOnlyList<MenuItem> GetByCategory(MenuCategory? category);

    /// <summary>Finds a single item by id, or <c>null</c> when it does not exist.</summary>
    /// <param name="id">The item id from the URL.</param>
    /// <returns>The item, or <c>null</c> for an unknown (or non-positive) id.</returns>
    MenuItem? GetById(int id);

    /// <summary>Categories in their display order, for the shared navigation bar.</summary>
    /// <returns>One entry per declared <see cref="MenuCategory"/>, in declaration order.</returns>
    IReadOnlyList<MenuCategory> GetCategories();

    /// <summary>
    /// Item count per category, precomputed once at load.
    /// </summary>
    /// <remarks>
    /// Exists so the global navigation can render its counts without filtering
    /// the whole menu once per category on every single page render. A category
    /// with no items is absent from the dictionary rather than present with zero;
    /// callers use <c>GetValueOrDefault</c>.
    /// </remarks>
    /// <returns>A read-only dictionary keyed by category.</returns>
    IReadOnlyDictionary<MenuCategory, int> GetCategoryCounts();

    /// <summary>
    /// A small, deterministic "today's specials" selection: highly rated items
    /// rotated by calendar day so the page changes daily but stays testable.
    /// </summary>
    /// <param name="date">The day to rotate for. Injected, never read from the clock here.</param>
    /// <param name="count">
    /// How many to return. Capped at the number of eligible items, so a large
    /// request never repeats a dish.
    /// </param>
    /// <returns>
    /// Zero or more items rated 4 or higher, in id order from the rotation offset.
    /// Empty when nothing qualifies.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is zero or negative.</exception>
    IReadOnlyList<MenuItem> GetSpecials(DateOnly date, int count);
}