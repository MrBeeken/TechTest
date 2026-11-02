/* ==========================================================================
 *  Menu Explorer — StarRatingTagHelper
 * ==========================================================================
 *  Role    Renders an accessible, CSS-styled star rating.
 *  Layer   TagHelpers. Compiled Razor feature, so there is no reflection cost
 *          per render.
 *  Debug   Out-of-range values are clamped, never thrown: bad data cannot take
 *          down a page that would otherwise render. The aria-label always states
 *          the clamped number, so assistive technology never hears an impossible
 *          rating.
 * ========================================================================== */

using System.Text;
using MenuExplorer.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MenuExplorer.TagHelpers;

/// <summary>
/// Renders an accessible, CSS-styled star rating.
/// </summary>
/// <example>
/// <code>
/// &lt;star-rating value="4" /&gt;
/// </code>
/// produces five stars, four of them filled, announced to screen readers as
/// "4 out of 5 stars".
/// </example>
/// <remarks>
/// Values outside 1–5 are clamped rather than throwing, so a bad value can never
/// take down a page that would otherwise render fine.
/// </remarks>
[HtmlTargetElement("star-rating", TagStructure = TagStructure.WithoutEndTag)]
public sealed class StarRatingTagHelper : TagHelper
{
    /// <summary>The rating to display, from 1 to 5.</summary>
    /// <remarks>Clamped to <see cref="MenuItemRatings.Min"/>–<see cref="MenuItemRatings.Max"/> on render.</remarks>
    public int Value { get; set; }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="output"/> is <c>null</c>.</exception>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        int rating = Math.Clamp(Value, MenuItemRatings.Min, MenuItemRatings.Max);

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "star-rating");

        // One accessible node for the whole rating; the glyphs themselves are
        // decorative and hidden, so screen readers hear a sentence, not stars.
        output.Attributes.SetAttribute("role", "img");
        output.Attributes.SetAttribute("aria-label", $"{rating} out of {MenuItemRatings.Max} stars");

        StringBuilder markup = new StringBuilder("<span class=\"star-rating__stars\" aria-hidden=\"true\">");

        for (int starIndex = MenuItemRatings.Min; starIndex <= MenuItemRatings.Max; starIndex++)
        {
            markup.Append(starIndex <= rating
                ? "<span class=\"star-rating__star is-filled\">&#9733;</span>"
                : "<span class=\"star-rating__star\">&#9733;</span>");
        }

        markup.Append("</span>");

        output.Content.SetHtmlContent(markup.ToString());
    }
}