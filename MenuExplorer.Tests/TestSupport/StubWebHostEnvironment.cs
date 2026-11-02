/* ==========================================================================
 *  Menu Explorer.Tests — StubWebHostEnvironment
 * ==========================================================================
 *  Role    Minimal IWebHostEnvironment for loader tests.
 *  Layer   Test support.
 *  Debug   Only ContentRootPath is ever read by JsonMenuService; everything else
 *          exists to satisfy the interface, so a failure caused by these values
 *          would mean the service started depending on more of the environment.
 * ========================================================================== */

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace MenuExplorer.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IWebHostEnvironment"/> so the menu loader can be tested
/// against a temporary directory without booting an application host.
/// </summary>
internal sealed class StubWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "MenuExplorer.Tests";

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string WebRootPath { get; set; } = string.Empty;

    public string EnvironmentName { get; set; } = Environments.Development;

    public string ContentRootPath { get; set; } = string.Empty;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}