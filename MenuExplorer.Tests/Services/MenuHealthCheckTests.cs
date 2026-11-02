/* ==========================================================================
 *  Menu Explorer.Tests — MenuHealthCheckTests
 * ==========================================================================
 *  Role    Covers both branches of the readiness probe.
 *  Layer   Unit tests against a fake IMenuService.
 *  Debug   The unhealthy branch cannot be reached through the shipping app (an
 *          empty menu fails startup first), so this file is the only guard on it.
 * ========================================================================== */

using MenuExplorer.Models;
using MenuExplorer.Services;
using MenuExplorer.Tests.TestSupport;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MenuExplorer.Tests.Services;

/// <summary>
/// Covers both branches of the readiness probe, including the unhealthy one
/// that the running app can never reach with valid data.
/// </summary>
public class MenuHealthCheckTests
{
    [Fact]
    public async Task ReportsHealthyWhenItemsAreLoaded()
    {
        MenuHealthCheck healthCheck = new MenuHealthCheck(new FakeMenuService([CreateItem()]));

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("1 menu items loaded", result.Description);
    }

    [Fact]
    public async Task ReportsUnhealthyWhenTheMenuIsEmpty()
    {
        MenuHealthCheck healthCheck = new MenuHealthCheck(new FakeMenuService([]));

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("empty", result.Description);
    }

    private static MenuItem CreateItem() =>
        new MenuItem
        {
            Id = 1,
            Name = "Soup",
            Description = "A description.",
            Price = 10m,
            Category = MenuCategory.Starters,
            IsVegetarian = true,
            Rating = 4
        };
}