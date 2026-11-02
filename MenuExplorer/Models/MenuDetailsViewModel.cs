/* ==========================================================================
 *  Menu Explorer — MenuDetailsViewModel
 * ==========================================================================
 *  Role    Everything Views/Menu/Details.cshtml needs.
 *  Layer   Models. Constructed by MenuController.Details; no data access.
 *  Debug   RelatedItems is capped at three and never contains the item being
 *          viewed, so the section renders nothing (or three cards) rather than
 *          a duplicate of the page's own dish.
 * ========================================================================== */

namespace MenuExplorer.Models;

/// <summary>View model for <c>Views/Menu/Details.cshtml</c>.</summary>
/// <example>
/// <code>
/// new MenuDetailsViewModel
/// {
///     Item = menuService.GetById(10)!,                       // Malva Pudding
///     RelatedItems = menuService.GetByCategory(MenuCategory.Desserts)
///         .Where(candidate =&gt; candidate.Id != 10)
///         .Take(3)
///         .ToList()
/// };
/// </code>
/// </example>
public sealed class MenuDetailsViewModel
{
    /// <summary>The item being viewed. Never <c>null</c>; an unknown id renders the 404 page instead.</summary>
    public required MenuItem Item { get; init; }

    /// <summary>Other items in the same category, excluding the current one.</summary>
    /// <remarks>At most three, in the service's display order. May be empty.</remarks>
    public required IReadOnlyList<MenuItem> RelatedItems { get; init; }
}