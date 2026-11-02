/* ==========================================================================
 *  Menu Explorer.Tests — MenuJsonReaderTests
 * ==========================================================================
 *  Role    Pins the JSON contract: what is accepted, what is rejected, and the
 *          resulting display order.
 *  Layer   Unit tests, pure. No file system, no host: JSON in, items out.
 *  Debug   Each rejection test asserts on the message an operator would read, so
 *          a message that stops being actionable fails here, not in production.
 * ========================================================================== */

using System.Text;
using System.Text.Json;
using MenuExplorer.Models;
using MenuExplorer.Services;

namespace MenuExplorer.Tests.Services;

/// <summary>
/// Unit tests for the JSON contract: what is accepted, what is rejected, and
/// what the resulting order is. These run without touching the file system.
/// </summary>
public class MenuJsonReaderTests
{
    [Fact]
    public void Parse_ReadsEveryField()
    {
        IReadOnlyList<MenuItem> items = Parse(
            """
            [
              {
                "id": 7,
                "name": "Malva Pudding",
                "description": "Warm apricot sponge.",
                "price": 75.50,
                "category": "Desserts",
                "isVegetarian": true,
                "rating": 5
              }
            ]
            """);

        MenuItem item = Assert.Single(items);
        Assert.Equal(7, item.Id);
        Assert.Equal("Malva Pudding", item.Name);
        Assert.Equal("Warm apricot sponge.", item.Description);
        Assert.Equal(75.50m, item.Price);
        Assert.Equal(MenuCategory.Desserts, item.Category);
        Assert.True(item.IsVegetarian);
        Assert.Equal(5, item.Rating);
    }

    [Fact]
    public void Parse_OrdersByCategoryThenName()
    {
        IReadOnlyList<MenuItem> items = Parse(BuildJson(
            Item(1, "Zulu Drink", category: "Drinks"),
            Item(2, "Bravo Starter", category: "Starters"),
            Item(3, "Alpha Starter", category: "Starters")));

        Assert.Equal(
            new[] { "Alpha Starter", "Bravo Starter", "Zulu Drink" },
            items.Select(item => item.Name));
    }

    [Fact]
    public void Parse_AcceptsCategoryNamesInAnyCase()
    {
        IReadOnlyList<MenuItem> items = Parse(BuildJson(Item(1, "Soup", category: "starters")));

        Assert.Equal(MenuCategory.Starters, items[0].Category);
    }

    [Fact]
    public void Parse_RejectsEmptyArray()
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(() => Parse("[]"));

        Assert.Contains("does not contain any menu items", exception.Message);
    }

    [Fact]
    public void Parse_RejectsNullArrayElements()
    {
        // System.Text.Json happily materialises "[null]" as a list with a null
        // element; the reader must turn that into an actionable data error.
        MenuDataException exception = Assert.Throws<MenuDataException>(() => Parse("[null]"));

        Assert.Contains("null entry at index 0", exception.Message);
    }

    [Fact]
    public void Parse_RejectsNullElementsAmongValidItems()
    {
        string validItem = """
            { "id": 1, "name": "Soup", "description": "D", "price": 10, "category": "Starters", "rating": 3 }
            """;

        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse($"[{validItem}, null]"));

        Assert.Contains("null entry at index 1", exception.Message);
        Assert.DoesNotContain("NullReferenceException", exception.Message);
    }

    [Fact]
    public void Parse_RejectsDuplicateIds()
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse(BuildJson(Item(5), Item(5, "Second"))));

        Assert.Contains("duplicate item id 5", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-2)]
    public void Parse_RejectsRatingsOutsideOneToFive(int rating)
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse(BuildJson(Item(rating: rating))));

        Assert.Contains("expected 1", exception.Message);
    }

    [Fact]
    public void Parse_RejectsNegativePrice()
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse(BuildJson(Item(price: -1m))));

        Assert.Contains("negative price", exception.Message);
    }

    [Fact]
    public void Parse_RejectsBlankName()
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse(BuildJson(Item(name: "   "))));

        Assert.Contains("blank name", exception.Message);
    }

    [Fact]
    public void Parse_RejectsBlankDescription()
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse(BuildJson(Item(description: "  "))));

        Assert.Contains("blank description", exception.Message);
    }

    [Fact]
    public void Parse_RejectsNonPositiveId()
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse(BuildJson(Item(id: 0))));

        Assert.Contains("non-positive id", exception.Message);
    }

    [Fact]
    public void Parse_WrapsMalformedJson()
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(() => Parse("[{"));

        Assert.Contains("is not valid JSON", exception.Message);
        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void Parse_ReportsUnknownCategoryWithTheAllowedValues()
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse(BuildJson(Item(1, "Burger", category: "Burgers"))));

        Assert.Contains("invalid category", exception.Message);
        Assert.Contains("Burgers", exception.Message);
        Assert.Contains("Starters", exception.Message);
        Assert.Contains("Drinks", exception.Message);

        // It is valid JSON with an invalid value, so it must not be reported as
        // malformed JSON.
        Assert.DoesNotContain("is not valid JSON", exception.Message);
    }

    [Fact]
    public void Parse_RejectsNumericCategoryValues()
    {
        // Enum.TryParse would treat "1" as Mains; the converter only accepts names.
        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse(BuildJson(Item(1, "Soup", category: "1"))));

        Assert.Contains("invalid category", exception.Message);
    }

    [Fact]
    public void Parse_RejectsMissingRequiredProperties()
    {
        MenuDataException exception = Assert.Throws<MenuDataException>(
            () => Parse("""[{ "id": 1, "name": "No description", "price": 10, "category": "Mains", "rating": 3 }]"""));

        Assert.Contains("is not valid JSON", exception.Message);
    }

    [Fact]
    public void Parse_FormatsPriceWithRandSymbol()
    {
        MenuItem item = Assert.Single(Parse(BuildJson(Item(price: 85.5m))));

        Assert.Equal("R85.50", item.PriceDisplay);
    }

    [Fact]
    public void Parse_ReadsImageFileAndComposesItsUrl()
    {
        MenuItem item = Assert.Single(Parse(
            """
            [
              {
                "id": 10,
                "name": "Malva Pudding",
                "description": "Warm apricot sponge.",
                "price": 75.50,
                "category": "Desserts",
                "imageFile": "10-malva-pudding.jpg",
                "rating": 4
              }
            ]
            """));

        Assert.Equal("10-malva-pudding.jpg", item.ImageFile);
        Assert.Equal("/images/menu/10-malva-pudding.jpg", item.ImageUrl);
    }

    [Fact]
    public void Parse_TreatsAnAbsentImageFileAsNoImage()
    {
        MenuItem item = Assert.Single(Parse(BuildJson(Item(1))));

        // Null, not an empty string: the views switch between a photograph and
        // the category glyph on null, so the absent case must not be "".
        Assert.Null(item.ImageFile);
        Assert.Null(item.ImageUrl);
    }

    [Theory]
    [InlineData("../appsettings.json")]
    [InlineData("images/menu/01-soup.jpg")]
    [InlineData("..\\\\appsettings.json")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\\\photos\\\\soup.jpg")]
    [InlineData("01-soup")]
    [InlineData(".jpg")]
    [InlineData("   ")]
    public void Parse_RejectsImageFilesThatAreNotASingleFileName(string imageFile)
    {
        string json =
            $$"""
              [
                {
                  "id": 1,
                  "name": "Soup",
                  "description": "D",
                  "price": 10,
                  "category": "Starters",
                  "imageFile": "{{imageFile}}",
                  "rating": 3
                }
              ]
              """;

        MenuDataException exception = Assert.Throws<MenuDataException>(() => Parse(json));

        Assert.Contains("imageFile", exception.Message);
        Assert.Contains("images/menu", exception.Message);
    }

    private static IReadOnlyList<MenuItem> Parse(string json) =>
        MenuJsonReader.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json)), "test.json");

    private static string BuildJson(params ItemJson[] items) =>
        "[" + string.Join(",", items.Select(item =>
            $$"""
              {
                "id": {{item.Id}},
                "name": "{{item.Name}}",
                "description": "{{item.Description}}",
                "price": {{item.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
                "category": "{{item.Category}}",
                "isVegetarian": {{(item.IsVegetarian ? "true" : "false")}},
                "rating": {{item.Rating}}
              }
              """)) + "]";

    private static ItemJson Item(
        int id = 1,
        string name = "Dish",
        string description = "A description",
        decimal price = 10m,
        int rating = 4,
        string category = "Mains") =>
        new(id, name, description, price, category, true, rating);

    private sealed record ItemJson(
        int Id,
        string Name,
        string Description,
        decimal Price,
        string Category,
        bool IsVegetarian,
        int Rating);
}