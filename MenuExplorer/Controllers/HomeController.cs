/* ==========================================================================
 *  Menu Explorer — HomeController
 * ==========================================================================
 *  Role    Landing page (hero + today's specials), the terminal error page, and
 *          the friendly page for bodiless status codes.
 *  Layer   Controllers. Parses nothing but a status code; all data comes from
 *          IMenuService and the injected TimeProvider.
 *  Debug   TimeProvider is injected so "today" is deterministic in tests and in
 *          the specials log line; Error() exposes the request id for support.
 * ========================================================================== */

using System.Diagnostics;
using MenuExplorer.Models;
using MenuExplorer.Services;
using Microsoft.AspNetCore.Mvc;

namespace MenuExplorer.Controllers;

/// <summary>
/// Serves the landing page and the two shared failure pages.
/// </summary>
public sealed class HomeController : Controller
{
    private readonly IMenuService menuService;
    private readonly TimeProvider timeProvider;

    /// <param name="menuService">Read-only menu source.</param>
    /// <param name="timeProvider">Supplies "today" for the specials rotation.</param>
    public HomeController(IMenuService menuService, TimeProvider timeProvider)
    {
        this.menuService = menuService;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// Landing page: a hero, menu statistics, and today's rotating specials.
    /// </summary>
    /// <returns>The <c>Home/Index</c> view bound to a <see cref="HomeViewModel"/>.</returns>
    [HttpGet]
    public IActionResult Index()
    {
        IReadOnlyList<MenuItem> allItems = menuService.GetAll();
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        HomeViewModel model = new HomeViewModel
        {
            AllItems = allItems,
            Specials = menuService.GetSpecials(today, count: 3),
            Today = today
        };

        return View(model);
    }

    /// <summary>
    /// Terminal error page, wired up by <c>UseExceptionHandler</c> in
    /// non-development environments.
    /// </summary>
    /// <returns>
    /// The <c>Shared/Error</c> view. The request id lets a user quote something
    /// that can be found in the server logs.
    /// </returns>
    /// <remarks>
    /// The response status is set to 500 explicitly. The exception handler sets it
    /// before re-executing, but a direct request to this URL would otherwise answer
    /// 200 — the same defect the status page had, on the one page whose copy says
    /// the request failed.
    /// </remarks>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    /// <summary>
    /// Friendly page for responses that would otherwise have no body, invoked by
    /// <c>UseStatusCodePagesWithReExecute</c> as <c>/Home/Status?code={0}</c>.
    /// </summary>
    /// <param name="code">
    /// The status code to render. Optional because model binding produces
    /// <c>null</c> for a missing or unparseable value (<c>?code=abc</c>), which
    /// must not be rendered as "HTTP 0".
    /// </param>
    /// <returns>
    /// The 404 view for a not-found status, otherwise <c>Home/Status</c>. The
    /// response status code is always set to the rendered code, so a direct hit on
    /// this URL is not reported to monitors and crawlers as a success.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Every real error status is rendered as-is. The middleware re-executes for
    /// any bodiless <c>400</c>–<c>599</c>, so narrowing the set to a hand-picked
    /// allow-list would turn a 502 or a 504 into "HTTP 500" and hide the actual
    /// cause from whoever is debugging the incident.
    /// </para>
    /// <para>
    /// Only values the middleware can never supply — missing, unparseable,
    /// negative, above 599, or a success code — fall back to 500. That is the
    /// "HTTP 0" bug with a general rule behind it: the page never invents a
    /// status, it reports the one it was handed or says the request failed.
    /// </para>
    /// <para>
    /// <c>NoStore</c> matches <see cref="Error"/>: an error page cached by a proxy
    /// or a browser outlives the outage it described.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /Home/Status?code=503 -> 503 + "The server responded with HTTP 503."
    /// GET /Home/Status?code=418 -> 418 (an error status is not ours to rewrite)
    /// GET /Home/Status?code=abc -> 500 + "The server responded with HTTP 500."
    /// </code>
    /// </example>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int? code)
    {
        int statusCode = code is int requested && IsErrorStatus(requested)
            ? requested
            : StatusCodes.Status500InternalServerError;

        Response.StatusCode = statusCode;

        if (statusCode == StatusCodes.Status401Unauthorized)
        {
            // RFC 9110 requires a challenge on every 401. This app has no
            // authentication scheme to name, so it declares the generic
            // form-based one instead of sending a 401 that no client can act on.
            Response.Headers.WWWAuthenticate = "FormBased";
        }

        if (statusCode == StatusCodes.Status404NotFound)
        {
            // No usable id reached this point, so the page stays generic.
            return View("NotFound", new NotFoundViewModel());
        }

        return View("Status", statusCode);
    }

    /// <summary>
    /// Whether a value is a status the HTTP error pipeline can legitimately ask
    /// this page to render.
    /// </summary>
    /// <param name="candidate">The bound <c>?code=</c> value.</param>
    /// <returns><c>true</c> for 400–599 inclusive; <c>false</c> for anything else.</returns>
    private static bool IsErrorStatus(int candidate) => candidate is >= 400 and <= 599;
}