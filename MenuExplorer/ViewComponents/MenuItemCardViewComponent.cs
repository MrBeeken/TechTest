/* ==========================================================================
 *  Menu Explorer — MenuItemCardViewComponent
 * ==========================================================================
 *  Role    Renders a single menu item card as a ViewComponent.
 *  Layer   ViewComponents. No data access; the caller supplies the item.
 *  Debug   Its default view delegates to _MenuItemCard.cshtml, so if a card looks
 *          wrong on the home page but right on the grid, the bug is in the
 *          invocation, not in the markup — there is only one copy of the markup.
 * ========================================================================== */

using MenuExplorer.Models;
using Microsoft.AspNetCore.Mvc;

namespace MenuExplorer.ViewComponents;

/// <summary>
/// Renders a single menu item card.
/// </summary>
/// <remarks>
/// This is the optional "ViewComponent instead of a plain partial" stretch
/// goal. It exists as an alternative way to invoke the card (used on the home
/// page) without duplicating markup: its default view delegates to the same
/// <c>_MenuItemCard.cshtml</c> partial used by the menu grid, so there is one
/// source of truth for how a card looks.
/// </remarks>
/// <example>
/// <code>
/// @foreach (MenuItem item in Model.Specials)
/// {
///     @await Component.InvokeAsync("MenuItemCard", new { item })
/// }
/// </code>
/// </example>
[ViewComponent(Name = "MenuItemCard")]
public sealed class MenuItemCardViewComponent : ViewComponent
{
    /// <summary>
    /// Returns the view for one item.
    /// </summary>
    /// <param name="item">The item to render. Must not be <c>null</c>.</param>
    /// <returns>The default view, bound to <paramref name="item"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <c>null</c>.</exception>
    public IViewComponentResult Invoke(MenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return View(item);
    }
}