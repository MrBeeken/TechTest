/* ==========================================================================
 *  Menu Explorer — MenuHealthCheck
 * ==========================================================================
 *  Role    Readiness probe behind GET /health.
 *  Layer   Services / health-check infrastructure. Transient, because the
 *          health-check middleware resolves it per probe.
 *  Debug   Reports the loaded item count when healthy, which distinguishes
 *          "running but empty" from "not running at all" in a probe response.
 * ========================================================================== */

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MenuExplorer.Services;

/// <summary>
/// Readiness check that the menu data is loaded and non-empty. Registered at
/// <c>/health</c> so a load balancer or uptime monitor can probe the app.
/// </summary>
/// <remarks>
/// Startup already fails on unreadable data, so this can only report unhealthy if
/// the service is constructed with an empty menu (as the unit tests do). It still
/// earns its place: it is the one check that proves the singleton is resolving.
/// </remarks>
/// <example>
/// <code>
/// GET /health  ->  200 "Healthy"     (items loaded)
/// GET /health  ->  503 "Unhealthy"   (menu is empty)
/// </code>
/// </example>
public sealed class MenuHealthCheck : IHealthCheck
{
    private readonly IMenuService menuService;

    /// <param name="menuService">The singleton menu to probe.</param>
    public MenuHealthCheck(IMenuService menuService) => this.menuService = menuService;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        int itemCount = menuService.GetAll().Count;

        // Already in memory, so there is nothing to await; the async signature is
        // the interface's, not an indication of I/O.
        return Task.FromResult(itemCount > 0
            ? HealthCheckResult.Healthy($"{itemCount} menu items loaded.")
            : HealthCheckResult.Unhealthy("Menu data is empty."));
    }
}