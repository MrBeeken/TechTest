/* ==========================================================================
 *  Menu Explorer — MenuController
 * ==========================================================================
 *  Role    The browsable menu: an optional category filter on the index, and a
 *          details page per item.
 *  Layer   Controllers. Parses input into typed values, asks IMenuService for
 *          data, selects a view. No filtering rules, no formatting.
 *  Debug   An unrecognised category is not an error: it renders the full menu
 *          with an explanation banner. An unknown or invalid id renders the
 *          shared 404 page with the requested id attached.
 * ========================================================================== */

using MenuExplorer.Models;
using MenuExplorer.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuExplorer.Controllers;

/// <summary>
/// Browsable menu: an optional category filter on the index, and a details
/// page per item.
/// </summary>
/// <remarks>
/// The controller is deliberately thin — it translates the query string into a
/// typed domain value, asks <see cref="IMenuService"/> for data, and selects a
/// view. Formatting and layout decisions live in Razor.
/// </remarks>
/// <example>
/// <code>
/// GET /Menu                        -> the full menu
/// GET /Menu?category=Desserts      -> filtered, case-insensitively
/// GET /Menu?category=Burgers       -> full menu plus a fallback notice
/// GET /Menu/Details/5              -> one item plus related dishes
/// </code>
/// </example>
public sealed class MenuController : Controller
{
    /// <summary>How many related dishes the details page shows.</summary>
    private const int RelatedItemCount = 3;

    /// <summary>
    /// Longest echo of an unrecognised category kept for display. Bounded here,
    /// at the boundary, so no view model can carry a multi-kilobyte query string
    /// into a response body.
    /// </summary>
    private const int MaxUnknownCategoryLength = 40;

    private readonly IMenuService menuService;
    private readonly MenuNavigationContext navigation;

    /// <param name="menuService">Read-only menu source; registered as a singleton.</param>
    /// <param name="navigation">
    /// Per-request record of the resolved category, read by the shared navigation.
    /// </param>
    public MenuController(IMenuService menuService, MenuNavigationContext navigation)
    {
        this.menuService = menuService;
        this.navigation = navigation;
    }

    /// <summary>
    /// GET <c>/Menu?category=Desserts</c> — the card grid, optionally filtered.
    /// </summary>
    /// <param name="category">
    /// Raw category name from the query string. Anything that is not a declared
    /// <see cref="MenuCategory"/> name is treated as unknown rather than invalid.
    /// </param>
    /// <returns>The <c>Menu/Index</c> view bound to a <see cref="MenuIndexViewModel"/>.</returns>
    [HttpGet]
    public IActionResult Index(string? category)
    {
        MenuCategory? selectedCategory = null;
        string? unknownCategory = null;

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (MenuCategoryParser.TryParse(category, out MenuCategory parsedCategory))
            {
                selectedCategory = parsedCategory;
            }
            else
            {
                // Not an error worth a 400: show the full menu and explain the
                // fallback, so a mistyped or stale link still lands somewhere useful.
                unknownCategory = TruncateForDisplay(category.Trim());
            }
        }

        MenuIndexViewModel model = new MenuIndexViewModel
        {
            Items = menuService.GetByCategory(selectedCategory),
            Category = selectedCategory,
            UnknownCategory = unknownCategory,
            TotalItemCount = menuService.GetAll().Count
        };

        // The one place the nav learns which section is current. It is the value
        // the grid was actually filtered on, so the two cannot disagree.
        navigation.MarkMenuPage(selectedCategory);

        return View(model);
    }

    /// <summary>
    /// GET <c>/Menu/Details/5</c> — one dish, plus up to three others from the
    /// same category.
    /// </summary>
    /// <param name="id">
    /// Item id. Nullable so a missing or unparseable id is a clean 404 rather
    /// than a bare "item 0". The <c>{id:int?}</c> route constraint in
    /// <c>Program.cs</c> rejects non-numeric ids before model binding, so null
    /// here means "omitted".
    /// </param>
    /// <returns>
    /// The <c>Menu/Details</c> view for a known id; otherwise the shared 404 page
    /// with a 404 status code.
    /// </returns>
    [HttpGet]
    public IActionResult Details(int? id)
    {
        // The page is part of the menu even when nothing resolves, so the nav
        // falls back to "All" instead of highlighting nothing (or, previously,
        // whatever ?category= happened to be in the URL).
        navigation.MarkMenuPage(null);

        if (id is null or <= 0)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound", new NotFoundViewModel());
        }

        MenuItem? item = menuService.GetById(id.Value);

        if (item is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound", new NotFoundViewModel { RequestedId = id.Value });
        }

        // The item's own section wins over any query-string leftover: a details
        // URL with ?category=Drinks cannot mislabel a Mains dish.
        navigation.MarkMenuPage(item.Category);

        List<MenuItem> relatedItems = menuService
            .GetByCategory(item.Category)
            .Where(candidate => candidate.Id != item.Id)
            .Take(RelatedItemCount)
            .ToList();

        return View(new MenuDetailsViewModel { Item = item, RelatedItems = relatedItems });
    }

    /// <summary>
    /// Shortens an unrecognised category for the fallback banner, keeping the
    /// response a predictable size no matter what arrived in the query string.
    /// </summary>
    /// <param name="value">The trimmed, unrecognised category value.</param>
    /// <returns>
    /// The value, or its first <see cref="MaxUnknownCategoryLength"/> UTF-16 code
    /// units followed by an ellipsis.
    /// </returns>
    /// <remarks>
    /// The cut is moved back one position when it would land between the halves
    /// of a surrogate pair (an emoji, or any character above U+FFFF). A lone
    /// surrogate is not valid UTF-16 text, so the HTML encoder would replace it
    /// with U+FFFD and the banner would show a broken glyph in place of the
    /// quoted value. Grapheme clusters (a letter plus its combining mark) can
    /// still be split: that is cosmetic, whereas an unpaired surrogate is text
    /// corruption, so only the corruption case is special-cased.
    /// </remarks>
    /// <example>
    /// <code>
    /// TruncateForDisplay("Burgers")            -> "Burgers"
    /// TruncateForDisplay(new string('x', 45))  -> "xxxxxxxx…"  (40 + ellipsis)
    /// </code>
    /// </example>
    private static string TruncateForDisplay(string value)
    {
        if (value.Length <= MaxUnknownCategoryLength)
        {
            return value;
        }

        int length = MaxUnknownCategoryLength;

        if (char.IsHighSurrogate(value[length - 1]))
        {
            length--;
        }

        return string.Concat(value.AsSpan(0, length), "…");
    }
}