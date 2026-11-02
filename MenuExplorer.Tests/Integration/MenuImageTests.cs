/* ==========================================================================
 *  Menu Explorer.Tests — MenuImageTests
 * ==========================================================================
 *  Role    Pins the dish-photograph pipeline end to end: the mapping in
 *          Data/menu.json, the markup the views emit, and the bytes the static
 *          pipeline serves for each mapped file.
 *  Layer   Integration tests. The menu itself comes from IMenuService, so these
 *          assertions follow the data file instead of a hardcoded list of dishes.
 *  Debug   A failure names the item and file involved. If an image is renamed on
 *          disk without editing menu.json, startup already fails; this suite
 *          catches the reverse — a file on disk that nothing points at.
 * ========================================================================== */

using System.Net;
using System.Text.RegularExpressions;
using MenuExplorer.Models;
using MenuExplorer.Services;
using MenuExplorer.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace MenuExplorer.Tests.Integration;

/// <summary>
/// End-to-end tests for the dish photographs: mapping, rendered markup, and the
/// static-file responses a browser would actually receive.
/// </summary>
public class MenuImageTests : IClassFixture<MenuAppFactory>
{
    private readonly HttpClient httpClient;
    private readonly IReadOnlyList<MenuItem> items;

    public MenuImageTests(MenuAppFactory factory)
    {
        httpClient = factory.CreateClient();
        items = factory.Services.GetRequiredService<IMenuService>().GetAll();
    }

    /// <summary>
    /// Folder the images live in, resolved the way a request would: under the
    /// web root of the running app.
    /// </summary>
    private static string ImageFolder =>
        Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "menu");

    [Fact]
    public void EveryItemInTheShippingMenuHasAPhotograph()
    {
        Assert.NotEmpty(items);

        // All 16 dishes were photographed. If one is ever added without an
        // image, the card silently falls back to a glyph, so it is worth a
        // failure to make that a decision rather than an accident.
        Assert.All(items, item => Assert.False(
            string.IsNullOrEmpty(item.ImageFile),
            $"Item {item.Id} '{item.Name}' has no imageFile."));
    }

    [Fact]
    public void EveryMappedFileNameIsUniqueAndOrderedByItemId()
    {
        List<string> files = items
            .Select(item => item.ImageFile)
            .Where(file => file is not null)
            .Select(file => file!)
            .ToList();

        Assert.Equal(files.Count, files.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        // The 'NN-' prefix is the item id, which is what makes the folder
        // browsable: 07 belongs to item 7. Assert that the two agree.
        Assert.All(items, item => Assert.StartsWith($"{item.Id:00}-", item.ImageFile, StringComparison.Ordinal));
    }

    [Fact]
    public void EveryImageOnDiskBelongsToExactlyOneItem()
    {
        string[] mapped = items
            .Select(item => item.ImageFile)
            .Where(file => file is not null)
            .Select(file => file!)
            .ToArray();

        string[] onDisk = Directory
            .GetFiles(ImageFolder)
            .Select(Path.GetFileName)
            .Select(name => name!)
            .ToArray();

        Assert.Equal(onDisk.Length, mapped.Length);
        Assert.Empty(onDisk.Except(mapped, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MenuGridRendersEveryPhotographAsADecorativeLazyImage()
    {
        string html = await GetHtml("/Menu");

        foreach (MenuItem item in items)
        {
            Match image = Regex.Match(
                html,
                $"""<img class="card__image"\s+src="{Regex.Escape(item.ImageUrl!)}"(?<attributes>[^>]*)>""");

            Assert.True(image.Success, $"Card for item {item.Id} '{item.Name}' does not show {item.ImageUrl}.");

            // Empty alt: the dish name is the link text immediately below, so a
            // description here would be read out twice. Lazy: cards below the
            // fold must not compete with the first screenful.
            Assert.Contains("alt=\"\"", image.Groups["attributes"].Value);
            Assert.Contains("loading=\"lazy\"", image.Groups["attributes"].Value);
        }
    }

    [Fact]
    public async Task DetailsPageRendersThePhotographOutsideTheLazyPath()
    {
        MenuItem item = items.First(item => item.ImageFile is not null);

        string html = await GetHtml($"/Menu/Details/{item.Id}");

        Match image = Regex.Match(html, $"""<img class="detail__image"\s+src="{Regex.Escape(item.ImageUrl!)}"(?<attributes>[^>]*)>""");

        Assert.True(image.Success, $"Details page for '{item.Name}' does not show {item.ImageUrl}.");

        // The hero is the largest thing above the fold, so it must not be lazy.
        Assert.DoesNotContain("loading=\"lazy\"", image.Groups["attributes"].Value);
    }

    [Fact]
    public async Task EveryPhotographIsServedAsAJpeg()
    {
        foreach (MenuItem item in items.Where(item => item.ImageFile is not null))
        {
            using HttpResponseMessage response = await httpClient.GetAsync(item.ImageUrl);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);

            // A photograph with no bytes is a 200 that renders as a broken tile.
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 1024, $"{item.ImageUrl} is only {bytes.Length} bytes.");
        }
    }

    private async Task<string> GetHtml(string url)
    {
        HttpResponseMessage response = await httpClient.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }
}