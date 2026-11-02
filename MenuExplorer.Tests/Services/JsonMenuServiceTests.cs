/* ==========================================================================
 *  Menu Explorer.Tests — JsonMenuServiceTests
 * ==========================================================================
 *  Role    Pins the file-backed loader: path resolution, failure messages,
 *          read-only guarantees, filtering and the specials rotation.
 *  Layer   Unit tests. Every case builds a real service over a temp directory,
 *          so the code under test is the shipping code, not a stub.
 *  Debug   A failure here names the rule that broke; the assertion messages quote
 *          the exact message users would see if the rule were missing.
 * ========================================================================== */

using System.Globalization;
using MenuExplorer.Models;
using MenuExplorer.Services;
using MenuExplorer.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MenuExplorer.Tests.Services;

/// <summary>
/// Tests the file-backed loader: path resolution, failure messages, filtering,
/// and the deterministic "today's specials" rotation.
/// </summary>
public class JsonMenuServiceTests
{
    private const string SampleMenu = """
        [
          { "id": 1, "name": "Soup", "description": "Starters one.", "price": 50, "category": "Starters", "isVegetarian": true, "rating": 5 },
          { "id": 2, "name": "Steak", "description": "Mains one.", "price": 250, "category": "Mains", "isVegetarian": false, "rating": 5 },
          { "id": 3, "name": "Risotto", "description": "Mains two.", "price": 180, "category": "Mains", "isVegetarian": true, "rating": 4 },
          { "id": 4, "name": "Posset", "description": "Desserts one.", "price": 70, "category": "Desserts", "isVegetarian": true, "rating": 3 }
        ]
        """;

    [Fact]
    public void LoadsItemsFromRelativePathUnderContentRoot()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        string expectedPath = tempDirectory.WriteFile("Data/menu.json", SampleMenu);

        JsonMenuService service = CreateService(tempDirectory.Path);

        Assert.Equal(4, service.GetAll().Count);
        Assert.Equal(expectedPath, service.SourcePath);
    }

    [Fact]
    public void SupportsAbsolutePathInConfiguration()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        string absolutePath = tempDirectory.WriteFile("elsewhere/menu.json", SampleMenu);

        JsonMenuService service = new JsonMenuService(
            Options.Create(new MenuDataOptions { FilePath = absolutePath }),
            new StubWebHostEnvironment { ContentRootPath = tempDirectory.Path },
            NullLogger<JsonMenuService>.Instance);

        Assert.Equal(absolutePath, service.SourcePath);
        Assert.Equal(4, service.GetAll().Count);
    }

    [Fact]
    public void FallsBackToDefaultPathWhenConfigurationIsBlank()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        string expectedPath = tempDirectory.WriteFile("Data/menu.json", SampleMenu);

        JsonMenuService service = new JsonMenuService(
            Options.Create(new MenuDataOptions { FilePath = "   " }),
            new StubWebHostEnvironment { ContentRootPath = tempDirectory.Path },
            NullLogger<JsonMenuService>.Instance);

        Assert.Equal(expectedPath, service.SourcePath);
    }

    [Fact]
    public void MissingFileFailsWithActionableMessage()
    {
        using TempDirectory tempDirectory = new TempDirectory();

        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => CreateService(tempDirectory.Path));

        Assert.Contains("Menu data file not found", exception.Message);
        Assert.Contains("MenuData:FilePath", exception.Message);
    }

    [Fact]
    public void InvalidDataFailsAtConstructionWithMenuDataException()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", "[null]");

        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => CreateService(tempDirectory.Path));

        // The loader must surface the actionable data error, never an NRE.
        Assert.Contains("null entry", exception.Message);
    }

    [Fact]
    public void ImageThatExistsOnDiskIsAccepted()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile(
            "Data/menu.json",
            """[{ "id": 1, "name": "Soup", "description": "D", "price": 10, "category": "Starters", "imageFile": "01-soup.jpg", "rating": 3 }]""");
        tempDirectory.WriteFile("wwwroot/images/menu/01-soup.jpg", "not really a jpeg");

        JsonMenuService service = CreateService(
            tempDirectory.Path,
            Path.Combine(tempDirectory.Path, "wwwroot"));

        Assert.Equal("/images/menu/01-soup.jpg", service.GetById(1)!.ImageUrl);
    }

    [Fact]
    public void ImageThatIsMissingFailsAtConstructionNamingTheItem()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile(
            "Data/menu.json",
            """[{ "id": 12, "name": "Lemon Posset", "description": "D", "price": 70, "category": "Desserts", "imageFile": "12-lemon-posset.jpg", "rating": 4 }]""");
        Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "wwwroot", "images", "menu"));

        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => CreateService(tempDirectory.Path, Path.Combine(tempDirectory.Path, "wwwroot")));

        // The file name alone is not enough to fix a copy-paste: name the row too.
        Assert.Contains("12-lemon-posset.jpg", exception.Message);
        Assert.Contains("item 12 'Lemon Posset'", exception.Message);
        Assert.Contains("images/menu", exception.Message);
    }

    [Fact]
    public void UnknownWebRootOnlyFailsItemsThatActuallyAskForAnImage()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);

        // The stub's web root is empty, which is unusable rather than a valid
        // folder. Items without an imageFile must still load, because the app
        // renders a category glyph for them instead.
        JsonMenuService service = CreateService(tempDirectory.Path);

        Assert.Equal(4, service.GetAll().Count);
        Assert.All(service.GetAll(), item => Assert.Null(item.ImageUrl));
    }

    [Fact]
    public void GetByCategoryFiltersAndNullReturnsEverything()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        Assert.Equal(2, service.GetByCategory(MenuCategory.Mains).Count);
        Assert.All(service.GetByCategory(MenuCategory.Mains), item => Assert.Equal(MenuCategory.Mains, item.Category));
        Assert.Equal(4, service.GetByCategory(null).Count);
    }

    [Fact]
    public void GetByIdReturnsNullForUnknownId()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        Assert.Equal("Steak", service.GetById(2)!.Name);
        Assert.Null(service.GetById(999));
    }

    [Fact]
    public void GetCategoriesFollowsDeclarationOrder()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        Assert.Equal(
            new[] { MenuCategory.Starters, MenuCategory.Mains, MenuCategory.Desserts, MenuCategory.Drinks },
            service.GetCategories());
    }

    [Fact]
    public void GetCategoryCountsReportsCountsPerCategory()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        IReadOnlyDictionary<MenuCategory, int> counts = service.GetCategoryCounts();

        Assert.Equal(1, counts[MenuCategory.Starters]);
        Assert.Equal(2, counts[MenuCategory.Mains]);
        Assert.Equal(1, counts[MenuCategory.Desserts]);
        Assert.False(counts.ContainsKey(MenuCategory.Drinks));
    }

    [Fact]
    public void GetAllCannotBeMutatedThroughACast()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        IReadOnlyList<MenuItem> items = service.GetAll();

        Assert.Throws<NotSupportedException>(() => ((IList<MenuItem>)items).Add(items[0]));
        Assert.Throws<NotSupportedException>(() => ((IList<MenuItem>)service.GetByCategory(MenuCategory.Mains)).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<MenuItem>)service.GetSpecials(DateOnly.MinValue, 1)).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<MenuCategory>)service.GetCategories()).Clear());
    }

    [Fact]
    public void CategoryCountsCannotBeMutatedThroughACast()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        IReadOnlyDictionary<MenuCategory, int> counts = service.GetCategoryCounts();

        // IReadOnlyDictionary is an interface, not a guarantee: an unwrapped
        // Dictionary would let any view write into the singleton for the life of
        // the process, and would not be thread-safe while doing it.
        Assert.Throws<NotSupportedException>(() => ((IDictionary<MenuCategory, int>)counts)[MenuCategory.Mains] = 999);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<MenuCategory, int>)counts).Remove(MenuCategory.Mains));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<MenuCategory, int>)counts).Clear());

        Assert.Equal(2, service.GetCategoryCounts()[MenuCategory.Mains]);
    }

    [Fact]
    public void GetSpecialsIsDeterministicForTheSameDay()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);
        DateOnly day = new DateOnly(2026, 10, 29);

        List<int> first = service.GetSpecials(day, count: 2).Select(item => item.Id).ToList();
        List<int> second = service.GetSpecials(day, count: 2).Select(item => item.Id).ToList();

        Assert.Equal(first, second);
        Assert.Equal(2, first.Count);
        Assert.Equal(2, first.Distinct().Count());
    }

    [Fact]
    public void GetSpecialsOnlyUsesItemsRatedFourOrHigher()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        IReadOnlyList<MenuItem> specials = service.GetSpecials(new DateOnly(2026, 10, 29), count: 3);

        Assert.All(specials, item => Assert.True(item.Rating >= 4));
        Assert.DoesNotContain(specials, item => item.Id == 4);
    }

    [Fact]
    public void GetSpecialsRotatesAcrossDays()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        MenuItem today = service.GetSpecials(new DateOnly(2026, 10, 29), count: 1).Single();
        MenuItem tomorrow = service.GetSpecials(new DateOnly(2026, 10, 30), count: 1).Single();

        Assert.NotEqual(today.Id, tomorrow.Id);
    }

    [Fact]
    public void GetSpecialsNeverRepeatsWhenCountExceedsPool()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        // Only three items are rated 4+, so asking for ten must return three.
        IReadOnlyList<MenuItem> specials = service.GetSpecials(new DateOnly(2026, 10, 29), count: 10);

        Assert.Equal(3, specials.Count);
        Assert.Equal(3, specials.Select(item => item.Id).Distinct().Count());
    }

    [Fact]
    public void GetSpecialsReturnsEmptyWhenNothingIsWellRated()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile(
            "Data/menu.json",
            """[{ "id": 1, "name": "Plain", "description": "D", "price": 10, "category": "Mains", "rating": 2 }]""");
        JsonMenuService service = CreateService(tempDirectory.Path);

        Assert.Empty(service.GetSpecials(new DateOnly(2026, 10, 29), count: 3));
    }

    [Fact]
    public void GetSpecialsRejectsNonPositiveCount()
    {
        using TempDirectory tempDirectory = new TempDirectory();
        tempDirectory.WriteFile("Data/menu.json", SampleMenu);
        JsonMenuService service = CreateService(tempDirectory.Path);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.GetSpecials(DateOnly.MinValue, count: 0));
    }

    private static JsonMenuService CreateService(string contentRoot, string? webRoot = null) =>
        new(
            Options.Create(new MenuDataOptions { FilePath = "Data/menu.json" }),
            new StubWebHostEnvironment { ContentRootPath = contentRoot, WebRootPath = webRoot ?? string.Empty },
            NullLogger<JsonMenuService>.Instance);
}