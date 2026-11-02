/* ==========================================================================
 *  Menu Explorer.Tests — StarRatingTagHelperTests
 * ==========================================================================
 *  Role    Pins the tag helper's markup, accessible label and clamping.
 *  Layer   Unit tests, no MVC host: the helper is invoked directly.
 *  Debug   A change in filled-star count or aria-label breaks here, which is the
 *          fastest way to spot an accessibility regression in the rating.
 * ========================================================================== */

using MenuExplorer.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MenuExplorer.Tests.TagHelpers;

/// <summary>
/// Exercises the custom <c>&lt;star-rating&gt;</c> tag helper directly, without
/// spinning up MVC, so the generated markup and clamping rules are pinned down.
/// </summary>
public class StarRatingTagHelperTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void RendersOneFilledStarPerPoint(int value)
    {
        TagHelperOutput output = Render(value);
        string markup = output.Content.GetContent();

        Assert.Equal("span", output.TagName);
        Assert.Equal("star-rating", output.Attributes["class"].Value);
        Assert.Equal("img", output.Attributes["role"].Value);
        Assert.Equal($"{value} out of 5 stars", output.Attributes["aria-label"].Value);
        Assert.Equal(value, CountOccurrences(markup, "is-filled"));
        Assert.Equal(5, CountOccurrences(markup, "&#9733;"));
    }

    [Fact]
    public void ClampsValuesAboveTheMaximum()
    {
        TagHelperOutput output = Render(99);

        Assert.Equal("5 out of 5 stars", output.Attributes["aria-label"].Value);
        Assert.Equal(5, CountOccurrences(output.Content.GetContent(), "is-filled"));
    }

    [Fact]
    public void ClampsValuesBelowTheMinimum()
    {
        TagHelperOutput output = Render(0);

        Assert.Equal("1 out of 5 stars", output.Attributes["aria-label"].Value);
        Assert.Equal(1, CountOccurrences(output.Content.GetContent(), "is-filled"));
    }

    [Fact]
    public void MarksGlyphsAsDecorativeAndTheWrapperAsTheAccessibleNode()
    {
        TagHelperOutput output = Render(4);
        string markup = output.Content.GetContent();

        Assert.Contains("aria-hidden=\"true\"", markup);
        Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
    }

    private static TagHelperOutput Render(int value)
    {
        StarRatingTagHelper starRatingTagHelper = new StarRatingTagHelper { Value = value };

        TagHelperContext tagHelperContext = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "test");

        TagHelperOutput tagHelperOutput = new TagHelperOutput(
            "star-rating",
            new TagHelperAttributeList(),
            (useCachedResult, htmlEncoder) =>
                Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        starRatingTagHelper.Process(tagHelperContext, tagHelperOutput);
        return tagHelperOutput;
    }

    private static int CountOccurrences(string value, string search) =>
        value.Split(search).Length - 1;
}