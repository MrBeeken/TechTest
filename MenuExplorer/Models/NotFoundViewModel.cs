/* ==========================================================================
 *  Menu Explorer — NotFoundViewModel
 * ==========================================================================
 *  Role    Content for the shared 404 page.
 *  Layer   Models. A typed replacement for ViewData["RequestedId"], so the view
 *          is compiler-checked like every other view in the app.
 *  Debug   RequestedId is echoed back so a user reporting a broken link quotes
 *          the exact id that failed, and it is null when there was no id to quote.
 * ========================================================================== */

namespace MenuExplorer.Models;

/// <summary>View model for <c>Views/Shared/NotFound.cshtml</c>.</summary>
/// <example>
/// <code>
/// return View("NotFound", new NotFoundViewModel { RequestedId = 999 });
/// // renders: "That dish isn't on the menu" + "We couldn't find menu item 999."
///
/// return View("NotFound", new NotFoundViewModel());
/// // renders: "We couldn't find that page" + "…It may have moved…"
/// </code>
/// </example>
public sealed class NotFoundViewModel
{
    /// <summary>
    /// The id that was requested, when one could be parsed. <c>null</c> means the
    /// request had no usable id (an unknown route, or <c>/Menu/Details/abc</c>
    /// rejected by the route constraint), so the page must not invent one.
    /// </summary>
    /// <remarks>
    /// This is the only input to the page: the view derives both its heading and
    /// its body from it, so the two can never disagree.
    /// </remarks>
    public int? RequestedId { get; init; }
}