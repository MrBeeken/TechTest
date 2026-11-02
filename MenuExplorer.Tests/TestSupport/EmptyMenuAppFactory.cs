/* ==========================================================================
 *  Menu Explorer.Tests — EmptyMenuAppFactory
 * ==========================================================================
 *  Role    Boots the app against a small in-memory menu, so the states the
 *          shipping data can never produce (an empty category, a menu with no
 *          specials) can still be rendered and asserted on.
 *  Layer   Test support.
 *  Debug   The real Data/menu.json has items in all four sections and twelve
 *          dishes rated 4+, which makes two branches unreachable in an
 *          integration test: the server-rendered empty category (an h2 panel) and
 *          the specials empty state (an h3 panel). Both are heading-level
 *          decisions, so leaving them unrendered left them untested.
 * ========================================================================== */

using MenuExplorer.Models;
using MenuExplorer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MenuExplorer.Tests.TestSupport;

/// <summary>
/// Runs the real pipeline against a menu containing only low-rated Mains, so the
/// empty-category and no-specials branches render.
/// </summary>
public sealed class EmptyMenuAppFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMenuService>();
            services.AddSingleton<IMenuService>(new FakeMenuService(
            [
                new MenuItem
                {
                    Id = 1,
                    Name = "Plain Toast",
                    Description = "Two slices, no ceremony.",
                    Price = 30m,
                    Category = MenuCategory.Mains,
                    IsVegetarian = true,
                    Rating = 2
                }
            ]));
        });
    }
}