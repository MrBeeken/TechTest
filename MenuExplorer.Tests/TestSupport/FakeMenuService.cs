/* ==========================================================================
 *  Menu Explorer.Tests — FakeMenuService
 * ==========================================================================
 *  Role    In-memory IMenuService for tests that need a specific menu shape,
 *          including an empty one.
 *  Layer   Test support.
 *  Debug   Returns plain read-only views rather than the shipping wrappers: these
 *          tests care about the health check's logic, not about immutability,
 *          which is pinned against the real service instead.
 * ========================================================================== */

using MenuExplorer.Models;
using MenuExplorer.Services;

namespace MenuExplorer.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="IMenuService"/> for tests that need a specific menu
/// shape (including an empty one) without touching the file system.
/// </summary>
internal sealed class FakeMenuService : IMenuService
{
    private readonly IReadOnlyList<MenuItem> items;

    public FakeMenuService(IReadOnlyList<MenuItem>? items = null) =>
        this.items = items ?? [];

    public IReadOnlyList<MenuItem> GetAll() => items;

    public IReadOnlyList<MenuItem> GetByCategory(MenuCategory? category) =>
        category is null
            ? items
            : items.Where(item => item.Category == category.Value).ToList();

    public MenuItem? GetById(int id) => items.FirstOrDefault(item => item.Id == id);

    public IReadOnlyList<MenuCategory> GetCategories() => Enum.GetValues<MenuCategory>();

    public IReadOnlyDictionary<MenuCategory, int> GetCategoryCounts() =>
        items.GroupBy(item => item.Category).ToDictionary(group => group.Key, group => group.Count());

    public IReadOnlyList<MenuItem> GetSpecials(DateOnly date, int count) =>
        items.Where(item => item.Rating >= 4).Take(count).ToList();
}