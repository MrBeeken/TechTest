/* ==========================================================================
 *  Menu Explorer.Tests — ProductionAppFactory
 * ==========================================================================
 *  Role    Boots the app in the Production environment, where the exception
 *          handler and HSTS are actually registered.
 *  Layer   Test support.
 *  Debug   MenuAppFactory forces Development (so failures show the developer
 *          page), which means the production branches of Program.cs were never
 *          executed by any test. This factory is the other half: it runs the real
 *          production pipeline and swaps in ThrowingMenuService to force a 500.
 *          The client's base address is a non-localhost host on purpose: HSTS
 *          skips localhost by design, so a localhost request would silently prove
 *          nothing.
 * ========================================================================== */

using MenuExplorer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MenuExplorer.Tests.TestSupport;

/// <summary>
/// Runs the shipping app with <c>ASPNETCORE_ENVIRONMENT=Production</c>, so
/// <c>UseExceptionHandler</c> and <c>UseHsts</c> are part of the pipeline.
/// </summary>
/// <remarks>
/// <see cref="IMenuService"/> is replaced by <see cref="ThrowingMenuService"/>
/// wrapping a freshly built <see cref="JsonMenuService"/>. It cannot be a simple
/// decorator registration: the outer type would depend on the interface it is
/// registered as, and resolution would recurse.
/// </remarks>
public sealed class ProductionAppFactory : WebApplicationFactory<Program>
{
    /// <summary>Host used by clients, chosen so HSTS does not exclude it.</summary>
    public const string TestHost = "https://menu.example";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMenuService>();

            services.AddSingleton<IMenuService>(provider => new ThrowingMenuService(
                new JsonMenuService(
                    provider.GetRequiredService<IOptions<MenuDataOptions>>(),
                    provider.GetRequiredService<IWebHostEnvironment>(),
                    provider.GetRequiredService<ILogger<JsonMenuService>>())));
        });
    }

    /// <summary>
    /// Creates a client that talks to <see cref="TestHost"/> and never follows a
    /// redirect, so a redirect is asserted rather than followed.
    /// </summary>
    /// <returns>An HTTP client for the production pipeline.</returns>
    public HttpClient CreateProductionClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(TestHost),
            AllowAutoRedirect = false
        });
}