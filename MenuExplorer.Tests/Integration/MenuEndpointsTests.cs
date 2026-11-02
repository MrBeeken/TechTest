/* ==========================================================================
 *  Menu Explorer.Tests — MenuEndpointsTests
 * ==========================================================================
 *  Role    End-to-end assertions against the real MVC pipeline: routing,
 *          query-string filtering, Razor rendering (partials, ViewComponent, tag
 *          helper), heading order and status-code handling.
 *  Layer   Integration tests. Boots the shipping app in memory via
 *          WebApplicationFactory<Program>, so no controller is tested in isolation.
 *  Debug   Assertions are on rendered HTML and status codes, which is what a
 *          browser and a monitor see. A failure here is a real user-visible change.
 * ========================================================================== */

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using MenuExplorer.Tests.TestSupport;

namespace MenuExplorer.Tests.Integration;

/// <summary>
/// End-to-end tests against the real MVC pipeline: routing, query-string
/// filtering, Razor rendering (partials, ViewComponent, tag helper), and
/// status-code handling.
/// </summary>
public class MenuEndpointsTests : IClassFixture<MenuAppFactory>
{
    private readonly HttpClient httpClient;

    public MenuEndpointsTests(MenuAppFactory factory) =>
        httpClient = factory.CreateClient();

    [Fact]
    public async Task HomePageRendersHeroAndSpecials()
    {
        string html = await GetHtml("/");

        Assert.Contains("Good food", html);
        Assert.Contains("Today's specials", html);
        Assert.Equal(3, CountCards(html));
    }

    [Fact]
    public async Task MenuIndexListsEveryItemFromJson()
    {
        string html = await GetHtml("/Menu");

        Assert.Contains("Showing 16 of 16 dishes", html);
        Assert.Equal(16, CountCards(html));
        Assert.Contains("Malva Pudding", html);
        Assert.Contains("Dry-Aged Sirloin", html);
        Assert.Contains("Rooibos Iced Tea", html);
        Assert.Contains("data-menu-search", html);
    }

    [Fact]
    public async Task MenuIndexCarriesTheWholeMenuSizeForTheClientSideCounter()
    {
        string html = await GetHtml("/Menu?category=Mains");

        // The grid is filtered to 5 cards, but the total must stay 16 so
        // menu-search.js can still report "of 16 dishes" after filtering.
        Assert.Contains("data-total-items=\"16\"", html);
        Assert.Equal(5, CountCards(html));
    }

    [Fact]
    public async Task SearchExplainsItselfWhenJavaScriptIsUnavailable()
    {
        string html = await GetHtml("/Menu");

        // The toolbar is revealed by script, so a no-JS visitor sees no search at
        // all. The note has to say why, and point at the control that still works.
        Match noScript = Regex.Match(html, "<noscript>(?<body>.*?)</noscript>", RegexOptions.Singleline);

        Assert.True(noScript.Success, "The menu page has no <noscript> note.");
        Assert.Contains("JavaScript", noScript.Groups["body"].Value);
        Assert.Contains("category", noScript.Groups["body"].Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DocumentDeclarationNamesTheLanguageForScreenReaders()
    {
        string html = await GetHtml("/Menu");

        // A bare "en" leaves a screen reader guessing between several English
        // voices; the menu is South African (rand prices, local dishes).
        Assert.Contains("<html lang=\"en-ZA\">", html);
    }

    [Fact]
    public async Task SearchToolbarIsHiddenUntilJavaScriptEnablesIt()
    {
        string html = await GetHtml("/Menu");

        // A visible-but-inert search box is a dead control; it must stay hidden
        // until the script that powers it is running.
        Match toolbarMatch = Regex.Match(html, "<div class=\"menu-toolbar\"[^>]*>");
        Assert.True(toolbarMatch.Success);
        Assert.Contains("hidden", toolbarMatch.Value);
    }

    [Fact]
    public async Task CategoryQueryStringFiltersTheGrid()
    {
        string html = await GetHtml("/Menu?category=Desserts");

        Assert.Equal(4, CountCards(html));
        Assert.Contains("Malva Pudding", html);
        Assert.Contains("Dark Chocolate Fondant", html);
        Assert.DoesNotContain("Dry-Aged Sirloin", html);
        Assert.Contains("category-pill is-active", html);
        Assert.Contains("Showing 4 of 16 dishes", html);
    }

    [Fact]
    public async Task CategoryFilterIsCaseInsensitive()
    {
        string html = await GetHtml("/Menu?category=desserts");

        Assert.Equal(4, CountCards(html));
    }

    [Fact]
    public async Task CategoryFilterIgnoresSurroundingWhitespace()
    {
        string html = await GetHtml("/Menu?category=%20Mains%20");

        Assert.Equal(5, CountCards(html));
    }

    [Theory]
    [InlineData("/Menu?category=Burgers")]
    [InlineData("/Menu?category=99")]
    [InlineData("/Menu?category=1")]
    [InlineData("/Menu?category=0")]
    [InlineData("/Menu?category=Starters,Mains")]
    [InlineData("/Menu?category=Starters%2CMains")]
    public async Task UnknownCategoryFallsBackToFullMenuWithAnExplanation(string url)
    {
        string html = await GetHtml(url);

        Assert.Equal(16, CountCards(html));
        Assert.Contains("isn't a menu category", html);
    }

    [Fact]
    public async Task UnknownCategoryStillHighlightsSomethingInTheNavigation()
    {
        string html = await GetHtml("/Menu?category=Burgers");

        // Exactly one pill is active, and it is "All" — the page really is
        // showing everything, so the nav must not contradict it.
        Assert.Equal(1, CountActivePills(html));
        Assert.Matches("class=\"category-pill is-active\"[^>]*>\\s*All", html);
    }

    [Fact]
    public async Task NavigationAgreesWithTheGridWhenTheCategoryParameterRepeats()
    {
        // Model binding takes the first value ("Desserts"); the query collection
        // joins them ("Desserts,Mains"), which the parser rejects. When the nav
        // parsed the URL itself it highlighted "All" over a Desserts-only grid.
        string html = await GetHtml("/Menu?category=Desserts&category=Mains");

        Assert.Equal(4, CountCards(html));
        Assert.Equal(1, CountActivePills(html));
        Assert.Matches("class=\"category-pill is-active\"[^>]*>\\s*Desserts", html);
    }

    [Fact]
    public async Task DetailsPageHighlightsTheItemsOwnSectionEvenWithAStrayCategoryParameter()
    {
        // Item 5 is a Mains dish. A leftover ?category=Drinks must not relabel it.
        string html = await GetHtml("/Menu/Details/5?category=Drinks");

        Assert.Equal(1, CountActivePills(html));
        Assert.Matches("class=\"category-pill is-active\"[^>]*>\\s*Mains", html);
    }

    [Fact]
    public async Task EveryResponseCarriesBaselineSecurityHeaders()
    {
        HttpStatusCode[] expectedFailures = [HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError];

        foreach (HttpStatusCode expected in expectedFailures)
        {
            string url = expected switch
            {
                HttpStatusCode.OK => "/",
                HttpStatusCode.NotFound => "/no-such-page",
                _ => "/Home/Status?code=500"
            };

            HttpResponseMessage response = await httpClient.GetAsync(url);

            Assert.Equal(expected, response.StatusCode);
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
            Assert.Equal("same-origin", response.Headers.GetValues("Referrer-Policy").Single());

            string policy = response.Headers.GetValues("Content-Security-Policy").Single();
            Assert.Contains("default-src 'self'", policy);
            Assert.Contains("frame-ancestors 'none'", policy);

            // Inline script or style would be silently blocked by this policy, so
            // the policy and the markup have to be checked together.
            Assert.DoesNotContain("unsafe-inline", policy);
        }
    }

    [Fact]
    public async Task LongUnknownCategoryIsTruncatedInTheBanner()
    {
        string longCategory = new string('x', 500);
        string html = await GetHtml($"/Menu?category={longCategory}");

        Assert.Contains("…", html);
        Assert.DoesNotContain(longCategory, html);
    }

    [Fact]
    public async Task TruncationNeverSplitsAnEmojiInHalf()
    {
        // 39 ASCII characters put the emoji's high surrogate exactly at the 40th
        // UTF-16 code unit. Cutting there produced a lone surrogate, which the HTML
        // encoder replaces with U+FFFD, so the banner read "xxx…\uFFFD…".
        string category = string.Concat(new string('x', 39), char.ConvertFromUtf32(0x1F600));

        string html = await GetHtml($"/Menu?category={Uri.EscapeDataString(category)}");

        Assert.DoesNotContain("\uFFFD", html);
        Assert.DoesNotContain(category, html);
        Assert.Contains(string.Concat(new string('x', 39), "…"), html);
    }

    [Fact]
    public async Task DetailsPageShowsASingleItem()
    {
        string html = await GetHtml("/Menu/Details/10");

        Assert.Contains("Malva Pudding", html);
        Assert.Contains("R75.00", html);
        Assert.Contains("Back to Desserts", html);
        Assert.Contains("More from Desserts", html);
        Assert.Contains("aria-label=\"5 out of 5 stars\"", html);
    }

    [Fact]
    public async Task DetailsPageReturns404ForUnknownItem()
    {
        HttpResponseMessage response = await httpClient.GetAsync("/Menu/Details/999");
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("isn't on the menu", html);
        Assert.Contains("menu item 999", html);
    }

    [Theory]
    [InlineData("/Menu/Details/abc")]
    [InlineData("/Menu/Details/")]
    [InlineData("/Menu/Details")]
    public async Task DetailsPageReturnsGeneric404WithoutInventingAnItemNumber(string url)
    {
        HttpResponseMessage response = await httpClient.GetAsync(url);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Heading and body must agree: "That dish isn't on the menu" over "we
        // couldn't find the page" contradicted itself, and the old body rendered
        // "We couldn't find menu item 0".
        Assert.Contains("couldn't find that page", html);
        Assert.DoesNotContain("menu item 0", html);
    }

    [Fact]
    public async Task UnknownRouteReturnsFriendly404()
    {
        HttpResponseMessage response = await httpClient.GetAsync("/no-such-page");
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("couldn't find the page", html);
    }

    [Fact]
    public async Task CategoryNavigationIsSharedByEveryPage()
    {
        string home = await GetHtml("/");
        string details = await GetHtml("/Menu/Details/5");

        foreach (string html in new[] { home, details })
        {
            Assert.Contains("aria-label=\"Menu categories\"", html);
            Assert.Contains("Starters", html);
            Assert.Contains("Drinks", html);
        }
    }

    [Fact]
    public async Task DetailsPageHighlightsItsOwnCategory()
    {
        string html = await GetHtml("/Menu/Details/10");

        Assert.Contains("Desserts", html);
        Assert.Equal(1, CountActivePills(html));
    }

    [Fact]
    public async Task StarRatingTagHelperRendersOnTheGrid()
    {
        string html = await GetHtml("/Menu");

        Assert.Contains("class=\"star-rating\"", html);
        Assert.Contains("aria-label=\"5 out of 5 stars\"", html);
        Assert.Contains("aria-label=\"3 out of 5 stars\"", html);
    }

    [Fact]
    public async Task StarRatingTagHelperDoesNotEmitItsValueAttribute()
    {
        string html = await GetHtml("/Menu");

        // The real Razor pipeline consumes the bound "value" attribute while the
        // unit test can only vouch for Process(). This pins the markup a browser
        // actually receives: no stray value="4" on the span.
        List<string> ratingElements = Regex.Matches(html, "<span class=\"star-rating\"[^>]*>")
            .Cast<Match>()
            .Select(match => match.Value)
            .ToList();

        Assert.NotEmpty(ratingElements);

        foreach (string element in ratingElements)
        {
            Assert.DoesNotContain("value=", element);
            Assert.Contains("aria-label=", element);
        }
    }

    [Fact]
    public async Task AppUsesCustomCssAndNoCssFramework()
    {
        string html = await GetHtml("/Menu");

        Assert.Contains("/css/site.css", html);
        Assert.DoesNotContain("bootstrap", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cdn.", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthEndpointReportsHealthy()
    {
        HttpResponseMessage response = await httpClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MenuDataFileIsNotServedAsAStaticAsset()
    {
        HttpResponseMessage response = await httpClient.GetAsync("/Data/menu.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MenuGridHasASectionHeadingSoCardTitlesDoNotSkipALevel()
    {
        string html = await GetHtml("/Menu");

        // The visual design has no "Menu items" heading, but the card titles are
        // h3: without a hidden h2 above them the outline jumps 1 -> 3, which is a
        // WCAG 1.3.1 failure and breaks screen-reader outline navigation.
        Assert.Contains("id=\"menu-results-heading\"", html);
        Assert.Contains("<h2 class=\"visually-hidden\"", html);
        Assert.Contains("<h3 class=\"card__title\"", html);
    }

    [Fact]
    public async Task SearchEmptyStateIsASubsectionOfTheMenuResultsSection()
    {
        string html = await GetHtml("/Menu");

        // "No dishes match that search" replaces the grid *inside* the "Menu items"
        // section, so it is an h3. As an h2 it read as a peer of "Menu items",
        // which closes that section and orphans the panel from its heading.
        Assert.Contains(
            "<h3 class=\"empty-state__title\">No dishes match that search</h3>",
            html);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Menu")]
    [InlineData("/Menu?category=Desserts")]
    [InlineData("/Menu/Details/1")]
    [InlineData("/no-such-page")]
    public async Task HeadingOutlineNeverSkipsALevel(string url)
    {
        HttpResponseMessage response = await httpClient.GetAsync(url);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        List<int> levels = Regex.Matches(html, "<h([1-6])[ >]")
            .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
            .ToList();

        // Assert.NotEmpty first: indexing levels[0] on an empty list threw an
        // ArgumentOutOfRangeException instead of failing as a missing heading.
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

    [Fact]
    public async Task MissingItemDetailsPageStillHighlightsANavigationPill()
    {
        HttpResponseMessage response = await httpClient.GetAsync("/Menu/Details/999");
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // The page shows a 404 body, and the nav must not contradict it by
        // highlighting nothing: "All" is the honest state, because the link that
        // would have been active no longer resolves to an item.
        Assert.Equal(1, CountActivePills(html));
        Assert.Matches("class=\"category-pill is-active\"[^>]*>\\s*All", html);
    }

    [Theory]
    [InlineData("404", HttpStatusCode.NotFound, "couldn't find that page")]
    [InlineData("418", (HttpStatusCode)418, "HTTP 418")]
    [InlineData("429", HttpStatusCode.TooManyRequests, "HTTP 429")]
    [InlineData("500", HttpStatusCode.InternalServerError, "HTTP 500")]
    [InlineData("503", HttpStatusCode.ServiceUnavailable, "HTTP 503")]
    public async Task StatusPageReflectsTheRequestedCode(string code, HttpStatusCode expected, string expectedText)
    {
        HttpResponseMessage response = await httpClient.GetAsync($"/Home/Status?code={code}");

        // A direct hit on the error page must not be reported as a success, or
        // monitors read it as healthy and crawlers index it as content.
        Assert.Equal(expected, response.StatusCode);
        Assert.Contains(expectedText, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    [Theory]
    [InlineData("/Home/Status?code=abc")]           // unparseable: bounded to null
    [InlineData("/Home/Status")]                    // no code at all
    [InlineData("/Home/Status?code=0")]             // previously rendered "HTTP 0"
    [InlineData("/Home/Status?code=-1")]            // negative
    [InlineData("/Home/Status?code=999999999999")]  // overflows int, so binds to null
    [InlineData("/Home/Status?code=302")]           // not an error status
    public async Task StatusPageNeverRendersAnInventedStatusCode(string url)
    {
        HttpResponseMessage response = await httpClient.GetAsync(url);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        // Only values the status-code middleware can never supply fall back to 500.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("HTTP 500", html);
        Assert.DoesNotContain("HTTP 0", html);
        Assert.DoesNotContain("HTTP 302", html);
    }

    [Fact]
    public async Task UnauthorizedStatusPageCarriesAnAuthenticationChallenge()
    {
        HttpResponseMessage response = await httpClient.GetAsync("/Home/Status?code=401");

        // RFC 9110 §15.5.2: a 401 without WWW-Authenticate is malformed, and a
        // client cannot tell what credentials to send.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEmpty(response.Headers.WwwAuthenticate);
    }

    [Theory]
    [InlineData("/Home/Status?code=503")]
    [InlineData("/Home/Error")]
    public async Task ErrorPagesForbidCaching(string url)
    {
        HttpResponseMessage response = await httpClient.GetAsync(url);
        CacheControlHeaderValue? cacheControl = response.Headers.CacheControl;

        Assert.NotNull(cacheControl);
        Assert.True(cacheControl.NoStore);

        // A cached error page outlives the outage it described.
        Assert.True(cacheControl.NoCache);
    }

    private async Task<string> GetHtml(string url)
    {
        HttpResponseMessage response = await httpClient.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Decode so assertions are about content, not about which entity name
        // Razor's HTML encoder happens to choose for an apostrophe.
        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }

    private static int CountCards(string html) =>
        Regex.Matches(html, "data-menu-card").Count;

    private static int CountActivePills(string html) =>
        Regex.Matches(html, "category-pill is-active").Count;
}