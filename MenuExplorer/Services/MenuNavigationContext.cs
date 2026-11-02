/* ==========================================================================
 *  Menu Explorer — MenuNavigationContext
 * ==========================================================================
 *  Role    Carries the menu page's already-resolved category from the controller
 *          to the shared category navigation.
 *  Layer   Services. Scoped, because it describes one request.
 *  Debug   The navigation used to re-parse the query string itself, which meant a
 *          URL like ?category=Desserts&category=Mains filtered on "Desserts"
 *          (model binding takes the first value) while the nav, seeing the joined
 *          "Desserts,Mains", highlighted "All". If a pill ever disagrees with the
 *          grid again, this is the seam: the controller calls MarkMenuPage once and
 *          the nav reads only that.
 * ========================================================================== */

using MenuExplorer.Models;

namespace MenuExplorer.Services;

/// <summary>
/// Per-request record of which menu category the current page actually resolved,
/// so the shared navigation never has to parse the URL a second time.
/// </summary>
/// <remarks>
/// <para>
/// The alternative — <c>Request.Query["category"]</c> inside the partial — made the
/// nav a second, subtly different implementation of the controller's rule. Model
/// binding takes the first value of a repeated parameter while the query
/// collection joins them with a comma, so the two could disagree. One value,
/// set once, cannot.
/// </para>
/// <para>
/// Registered as scoped: one instance per request, so there is no cross-request
/// state and no locking.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // MenuController.Index
/// navigation.MarkMenuPage(selectedCategory);   // null means "All"
///
/// // _CategoryNav.cshtml
/// MenuCategory? active = navigation.ActiveCategory;
/// bool isAllActive = navigation.IsMenuPage &amp;&amp; active is null;
/// </code>
/// </example>
public sealed class MenuNavigationContext
{
    /// <summary>
    /// <c>true</c> once <see cref="MarkMenuPage"/> has been called for this request,
    /// i.e. the page being rendered is part of the menu.
    /// </summary>
    /// <remarks>
    /// The home page and the error pages leave this <c>false</c>, which is what
    /// stops the navigation claiming a current section on a page that is not
    /// showing one.
    /// </remarks>
    public bool IsMenuPage { get; private set; }

    /// <summary>
    /// The category the page resolved, or <c>null</c> when it is showing every
    /// section (the full menu, an unrecognised filter, or a details id that
    /// matched no item).
    /// </summary>
    public MenuCategory? ActiveCategory { get; private set; }

    /// <summary>
    /// Records that this request is a menu page showing <paramref name="category"/>.
    /// </summary>
    /// <param name="category">
    /// The resolved category, or <c>null</c> when the page shows the whole menu.
    /// Pass the value the controller actually used — never the raw query string.
    /// </param>
    /// <remarks>
    /// Called by the menu controller only. A non-menu page never calls it, so the
    /// navigation stays inert there.
    /// </remarks>
    public void MarkMenuPage(MenuCategory? category)
    {
        IsMenuPage = true;
        ActiveCategory = category;
    }
}