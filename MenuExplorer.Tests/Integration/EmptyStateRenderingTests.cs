/* ==========================================================================
 *  Menu Explorer.Tests — EmptyStateRenderingTests
 * ==========================================================================
 *  Role    Renders the two empty states the shipping data can never produce, so
 *          their heading levels are tested against real markup.
 *  Layer   Integration tests against the real pipeline with a seeded fake menu.
 *  Debug   EmptyStateViewModelTests pins the property; this file pins the element
 *          the partial emits. A failure means a heading level regressed inside a
 *          section, which is an outline defect a screen-reader user feels.
 * ========================================================================== */

using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using MenuExplorer.Tests.TestSupport;

namespace MenuExplorer.Tests.Integration;

/// <summary>
/// Heading-level behaviour of the shared empty panel in both of its placements:
/// page level (<c>h2</c>) and nested inside a section (<c>h3</c>).
/// </summary>
public class EmptyStateRenderingTests : IClassFixture<EmptyMenuAppFactory>
{
    private readonly HttpClient httpClient;

    /// <param name="factory">Boots the app with one low-rated Mains item.</param>
    public EmptyStateRenderingTests(EmptyMenuAppFactory factory) =>
        httpClient = factory.CreateClient();

    /// <summary>
    /// An empty category renders the panel as the page's <c>h2</c>, directly under
    /// the <c>h1</c>.
    /// </summary>
    [Fact]
    public async Task EmptyCategoryRendersThePanelAsH2()
    {
        string html = await GetHtml("/Menu?category=Desserts");

        Assert.Contains("<h2 class=\"empty-state__title\">No desserts today</h2>", html);
        Assert.DoesNotContain("data-menu-card", html);
    }

    /// <summary>
    /// The specials panel sits inside "Today's specials", so it must be an
    /// <c>h3</c> rather than a second <c>h2</c> that closes the section.
    /// </summary>
    [Fact]
    public async Task SpecialsEmptyStateRendersAsH3InsideItsSection()
    {
        string html = await GetHtml("/");

        Assert.Contains("<h3 class=\"empty-state__title\">No specials on the board today</h3>", html);

        // The specials section owns the only h2 on the page; a second one would
        // mean the panel had been promoted back to a peer of its own section.
        List<Match> secondLevelHeadings = Regex.Matches(html, "<h2[ >]").Cast<Match>().ToList();
        Assert.Single(secondLevelHeadings);
    }

    /// <summary>
    /// Both placements keep the outline valid: one <c>h1</c>, no skipped level.
    /// </summary>
    /// <param name="url">A page that renders one of the two empty states.</param>
    [Theory]
    [InlineData("/")]
    [InlineData("/Menu?category=Desserts")]
    public async Task RenderedEmptyStatesKeepTheOutlineValid(string url)
    {
        string html = await GetHtml(url);

        List<int> levels = Regex.Matches(html, "<h([1-6])[ >]")
            .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
            .ToList();

        Assert.NotEmpty(levels);
        Assert.Equal(1, levels[0]);
        Assert.Equal(1, levels.Count(level => level == 1));

        for (int index = 1; index < levels.Count; index++)
        {
            Assert.True(
                levels[index] - levels[index - 1] <= 1,
                $"{url}: heading level jumped from h{levels[index - 1]} to h{levels[index]} at position {index}.");
        }
    }

    private async Task<string> GetHtml(string url)
    {
        HttpResponseMessage response = await httpClient.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }
}