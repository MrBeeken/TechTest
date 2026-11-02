/* ==========================================================================
 *  Menu Explorer.Tests — MenuAppFactory
 * ==========================================================================
 *  Role    Boots the real application in memory for integration tests.
 *  Layer   Test support.
 *  Debug   The app anchors its content root to AppContext.BaseDirectory and the
 *          web project copies Data/menu.json into this project's output, so the
 *          tests read the same JSON file the app ships with — not a fixture.
 *          The Development environment is forced so a failing test shows the
 *          developer exception page rather than a swallowed 500.
 * ========================================================================== */

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MenuExplorer.Tests.TestSupport;

/// <summary>
/// Boots the real application in memory so integration tests exercise the true
/// MVC pipeline (routing, Razor compilation, tag helpers, status codes).
/// </summary>
/// <remarks>
/// The application anchors its content root to <c>AppContext.BaseDirectory</c>,
/// and the web project's <c>Data/menu.json</c> is copied into this test
/// project's output, so the tests read the same JSON file the app ships with.
/// </remarks>
public sealed class MenuAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Development");
}