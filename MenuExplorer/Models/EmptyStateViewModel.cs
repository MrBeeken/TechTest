/* ==========================================================================
 *  Menu Explorer — EmptyStateViewModel
 * ==========================================================================
 *  Role    Content for the reusable _EmptyState partial.
 *  Layer   Models. Two callers: the server-rendered empty category, and the
 *          client-side "no search matches" panel.
 *  Debug   ShowClearSearchAction exists because the two callers need different
 *          recovery actions: a link needs JavaScript-free navigation, a button
 *          needs the script that owns the search box.
 * ========================================================================== */

namespace MenuExplorer.Models;

/// <summary>
/// Content for the reusable <c>_EmptyState</c> partial, which is used both for
/// an empty category filter and for a client-side search with no matches.
/// </summary>
/// <remarks>
/// The partial renders <see cref="Title"/> as a heading, and the level is part of
/// the model: at the top of a page it must be an <c>h2</c>, but inside a section
/// that already owns an <c>h2</c> — "Menu items", "Today's specials" — a second
/// <c>h2</c> would open a new subsection instead of labelling the panel that
/// replaces the grid.
/// </remarks>
/// <example>
/// <code>
/// // Server-side: an empty category, recovered by a link, at page level (h2).
/// new EmptyStateViewModel(
///     title: "No desserts today",
///     message: "Nothing in this section made it onto today's menu.")
/// {
///     ActionUrl = Url.Action("Index", "Menu"),
///     ActionText = "Browse the full menu"
/// };
///
/// // Client-side: no search matches, recovered by a button the script owns.
/// // It lives inside the "Menu items" section, so it is a subsection (h3).
/// new EmptyStateViewModel(
///     title: "No dishes match that search",
///     message: "Try a shorter word or a different ingredient.")
/// {
///     ShowClearSearchAction = true,
///     HeadingLevel = 3
/// };
/// </code>
/// </example>
public sealed class EmptyStateViewModel
{
    private readonly int headingLevel = 2;

    /// <param name="title">Short heading, e.g. <c>No desserts today</c>.</param>
    /// <param name="message">One sentence explaining what to do next.</param>
    public EmptyStateViewModel(string title, string message)
    {
        Title = title;
        Message = message;
    }

    /// <summary>Heading text, rendered at <see cref="HeadingLevel"/>.</summary>
    public string Title { get; }

    /// <summary>Supporting sentence under the heading.</summary>
    public string Message { get; }

    /// <summary>
    /// Heading level used for <see cref="Title"/>. Defaults to <c>2</c>, which is
    /// correct whenever the panel sits directly under the page's <c>h1</c>.
    /// </summary>
    /// <remarks>
    /// Set <c>3</c> when the panel is nested inside a section that already has its
    /// own <c>h2</c>. Bounded to 2–6 so a typo fails at construction instead of
    /// producing an <c>h9</c> in the markup.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is not between 2 and 6, or would be <c>h1</c> and duplicate the
    /// page title.
    /// </exception>
    public int HeadingLevel
    {
        get => headingLevel;
        init => headingLevel = value is >= 2 and <= 6
            ? value
            : throw new ArgumentOutOfRangeException(
                nameof(HeadingLevel),
                value,
                "HeadingLevel must be between 2 and 6.");
    }

    /// <summary>When set, the partial renders a recovery link.</summary>
    /// <remarks>Ignored when <see cref="ShowClearSearchAction"/> is <c>true</c>.</remarks>
    public string? ActionUrl { get; init; }

    /// <summary>Label for the recovery link. Unused when there is no <see cref="ActionUrl"/>.</summary>
    public string ActionText { get; init; } = "Browse the full menu";

    /// <summary>
    /// Renders a "Clear search" button instead of a link. The button is wired up
    /// by <c>wwwroot/js/menu-search.js</c>; without JavaScript it stays hidden
    /// along with the rest of the search empty state.
    /// </summary>
    public bool ShowClearSearchAction { get; init; }
}