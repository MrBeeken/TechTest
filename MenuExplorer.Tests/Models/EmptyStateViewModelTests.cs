/* ==========================================================================
 *  Menu Explorer.Tests — EmptyStateViewModelTests
 * ==========================================================================
 *  Role    Locks down the heading-level contract of the shared empty panel.
 *  Layer   Unit tests, pure.
 *  Debug   EmptyStateViewModel is rendered inside several sections. If the level
 *          guard ever regresses, the failure shows up as an outline defect on a
 *          page rather than as an exception, so it is pinned here instead.
 * ========================================================================== */

using MenuExplorer.Models;

namespace MenuExplorer.Tests.Models;

/// <summary>
/// Heading-level contract of <see cref="EmptyStateViewModel"/>: the shared empty
/// panel is the same markup at page level and inside a section, so the level is
/// data and is validated at construction.
/// </summary>
public class EmptyStateViewModelTests
{
    /// <summary>A panel that sits directly under the page's <c>h1</c> stays at h2.</summary>
    [Fact]
    public void HeadingLevelDefaultsToH2SoPageLevelPanelsKeepTheirRank()
    {
        EmptyStateViewModel model = new EmptyStateViewModel("No desserts today", "Nothing made it onto the menu.");

        Assert.Equal(2, model.HeadingLevel);
    }

    /// <summary>A panel nested under an existing <c>h2</c> can drop to h3.</summary>
    [Fact]
    public void HeadingLevelCanBeLoweredForNestedPanels()
    {
        EmptyStateViewModel model = new EmptyStateViewModel("No dishes match that search", "Try another word.")
        {
            HeadingLevel = 3
        };

        Assert.Equal(3, model.HeadingLevel);
    }

    /// <summary>
    /// Construction fails for a level that would duplicate the page title or
    /// produce markup no browser can render.
    /// </summary>
    /// <param name="invalidLevel">A level outside 2–6.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(-3)]
    public void HeadingLevelOutsideTheSupportedRangeIsRejected(int invalidLevel)
    {
        EmptyStateViewModel model = new EmptyStateViewModel("Title", "Message");

        Assert.Throws<ArgumentOutOfRangeException>(() => new EmptyStateViewModel("Title", "Message")
        {
            HeadingLevel = invalidLevel
        });

        // The default instance is untouched by the rejected initialiser.
        Assert.Equal(2, model.HeadingLevel);
    }
}