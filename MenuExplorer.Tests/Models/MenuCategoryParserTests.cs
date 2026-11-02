/* ==========================================================================
 *  Menu Explorer.Tests — MenuCategoryParserTests
 * ==========================================================================
 *  Role    Locks down the query-string contract for categories.
 *  Layer   Unit tests, pure.
 *  Debug   Every rejected case here is an input Enum.TryParse would have
 *          accepted silently ("1", "Starters,Mains"), so a regression shows up
 *          as a filter that quietly works when it should fall back.
 * ========================================================================== */

using MenuExplorer.Models;

namespace MenuExplorer.Tests.Models;

/// <summary>
/// Locks down the query-string contract for categories, including the values
/// that <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> would have
/// quietly accepted.
/// </summary>
public class MenuCategoryParserTests
{
    [Theory]
    [InlineData("Starters", MenuCategory.Starters)]
    [InlineData("Mains", MenuCategory.Mains)]
    [InlineData("Desserts", MenuCategory.Desserts)]
    [InlineData("Drinks", MenuCategory.Drinks)]
    [InlineData("mains", MenuCategory.Mains)]
    [InlineData("DESSERTS", MenuCategory.Desserts)]
    [InlineData("  Mains  ", MenuCategory.Mains)]
    public void ParsesDeclaredNamesIgnoringCaseAndWhitespace(string input, MenuCategory expected)
    {
        bool parsed = MenuCategoryParser.TryParse(input, out MenuCategory actual);

        Assert.True(parsed);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("Starters,Mains")]
    [InlineData("Starters, Mains")]
    [InlineData("Burgers")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RejectsNumbersCombinationsAndUnknownNames(string? input)
    {
        Assert.False(MenuCategoryParser.TryParse(input, out _));
    }

    [Fact]
    public void RejectsNumericValueThatHappensToBeADefinedMember()
    {
        // Mains is 1. Enum.TryParse accepts "1" and Enum.IsDefined confirms the
        // resulting value, so the naive implementation filtered by Mains here.
        Assert.False(MenuCategoryParser.TryParse("1", out MenuCategory category));
        Assert.Equal(default, category);
    }

    [Fact]
    public void RejectsCombinationThatCollapsesOntoARealMember()
    {
        // Starters is 0 and Mains is 1, so "Starters,Mains" == 1 == Mains.
        Assert.False(MenuCategoryParser.TryParse("Starters,Mains", out _));
    }
}