/* ==========================================================================
 *  Menu Explorer.Tests — ThrowingMenuService
 * ==========================================================================
 *  Role    A decorator that faults one method, so the production exception
 *          pipeline can be exercised without a test-only endpoint in the app.
 *  Layer   Test support.
 *  Debug   Only GetById throws, and only when armed. The error page renders the
 *          shared layout, whose navigation calls GetAll/GetCategoryCounts, so a
 *          service that threw from every member would fault the error page too and
 *          the test would measure nothing.
 * ========================================================================== */

using MenuExplorer.Models;
using MenuExplorer.Services;

namespace MenuExplorer.Tests.TestSupport;

/// <summary>
/// Wraps a real <see cref="IMenuService"/> and throws from <see cref="GetById"/>
/// when armed, which is enough to drive an unhandled exception into the
/// production exception handler.
/// </summary>
internal sealed class ThrowingMenuService : IMenuService
{
    private readonly IMenuService inner;

    /// <param name="inner">The service that answers everything else.</param>
    public ThrowingMenuService(IMenuService inner) => this.inner = inner;

    /// <summary>
    /// Whether <see cref="GetById"/> should throw. Static because the container
    /// resolves a fresh decorator per request and a test must be able to arm it
    /// before making the request.
    /// </summary>
    public static bool IsArmed { get; set; }

    /// <summary>Also exposes the wrapped service, for tests that need real data.</summary>
    public IMenuService Inner => inner;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Always, when <see cref="IsArmed"/> is set.</exception>
    public MenuItem? GetById(int id) =>
        IsArmed
            ? throw new InvalidOperationException("Deliberate test fault: the details page could not be built.")
            : inner.GetById(id);

    /// <inheritdoc />
    public IReadOnlyList<MenuItem> GetAll() => inner.GetAll();

    /// <inheritdoc />
    public IReadOnlyList<MenuItem> GetByCategory(MenuCategory? category) => inner.GetByCategory(category);

    /// <inheritdoc />
    public IReadOnlyList<MenuCategory> GetCategories() => inner.GetCategories();

    /// <inheritdoc />
    public IReadOnlyDictionary<MenuCategory, int> GetCategoryCounts() => inner.GetCategoryCounts();

    /// <inheritdoc />
    public IReadOnlyList<MenuItem> GetSpecials(DateOnly date, int count) => inner.GetSpecials(date, count);
}