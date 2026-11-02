/* ==========================================================================
 *  Menu Explorer.Tests — ErrorPipelineTests
 * ==========================================================================
 *  Role    Drives the production exception handler and HSTS, which no other test
 *          reaches because MenuAppFactory runs in Development.
 *  Layer   Integration tests against the real pipeline in Production.
 *  Debug   A failure here means the page a user sees when something breaks is
 *          broken. If the 500 case fails with the developer exception page, the
 *          environment override did not apply; if it fails with a 200, the
 *          exception handler is no longer registered.
 * ========================================================================== */

using System.Net;
using MenuExplorer.Tests.TestSupport;

namespace MenuExplorer.Tests.Integration;

/// <summary>
/// Covers the branches of <c>Program.cs</c> that only exist outside Development:
/// <c>UseExceptionHandler</c>, <c>UseHsts</c>, and the terminal error page.
/// </summary>
public class ErrorPipelineTests : IClassFixture<ProductionAppFactory>
{
    private readonly ProductionAppFactory factory;

    /// <param name="factory">Boots the app in the Production environment.</param>
    public ErrorPipelineTests(ProductionAppFactory factory) => this.factory = factory;

    /// <summary>
    /// An unhandled exception is rendered as the friendly page with a 500, and
    /// leaks nothing about the stack.
    /// </summary>
    [Fact]
    public async Task UnhandledExceptionRendersTheFriendlyErrorPageWithA500()
    {
        ThrowingMenuService.IsArmed = true;

        try
        {
            HttpResponseMessage response = await factory.CreateProductionClient().GetAsync("/Menu/Details/1");
            string html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("An unexpected error occurred", html);
            Assert.Contains("Reference:", html);

            // A production error page must never echo the exception or its stack.
            Assert.DoesNotContain("Deliberate test fault", html);
            Assert.DoesNotContain("at MenuExplorer.", html);
            Assert.DoesNotContain("StackTrace", html);
        }
        finally
        {
            // Never leave the fault armed for the next test in this class.
            ThrowingMenuService.IsArmed = false;
        }
    }

    /// <summary>
    /// The handler must recover: after one faulted request, normal pages still work.
    /// </summary>
    [Fact]
    public async Task TheAppKeepsServingAfterAnUnhandledException()
    {
        ThrowingMenuService.IsArmed = true;

        try
        {
            using HttpClient client = factory.CreateProductionClient();

            HttpResponseMessage failed = await client.GetAsync("/Menu/Details/1");
            Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);

            ThrowingMenuService.IsArmed = false;

            HttpResponseMessage recovered = await client.GetAsync("/Menu");
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        }
        finally
        {
            ThrowingMenuService.IsArmed = false;
        }
    }

    /// <summary>
    /// HTTPS responses carry HSTS, so a browser is told to stay on TLS.
    /// </summary>
    [Fact]
    public async Task HttpsResponsesCarryStrictTransportSecurity()
    {
        HttpResponseMessage response = await factory.CreateProductionClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.Headers.Contains("Strict-Transport-Security"),
            $"Expected HSTS on '{ProductionAppFactory.TestHost}', got: {string.Join(", ", response.Headers.Select(header => header.Key))}");
    }

    /// <summary>
    /// Hitting the error page directly answers 500, not 200.
    /// </summary>
    [Fact]
    public async Task ErrorPageHitDirectlyIsNotReportedAsASuccess()
    {
        HttpResponseMessage response = await factory.CreateProductionClient().GetAsync("/Home/Error");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}