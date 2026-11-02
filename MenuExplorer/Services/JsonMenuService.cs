/* ==========================================================================
 *  Menu Explorer — JsonMenuService
 * ==========================================================================
 *  Role    Load the menu JSON once, validate it, and serve it from memory.
 *  Layer   Services. Owns file I/O; knows nothing about HTTP or HTML.
 *  Lifetime Singleton (see Program.cs). The data is immutable for the process,
 *          which is what makes a singleton safe and removes per-request I/O.
 *  Debug   Startup logs one structured line with the item count, category count
 *          and resolved absolute path. Any data problem throws MenuDataException
 *          during construction, so a bad deploy fails before serving traffic.
 * ========================================================================== */

using System.Collections.ObjectModel;
using MenuExplorer.Models;
using Microsoft.Extensions.Options;

namespace MenuExplorer.Services;

/// <summary>
/// Loads the menu from a JSON file once, validates it, and serves it from
/// memory for the rest of the process lifetime.
/// </summary>
/// <remarks>
/// <para>
/// Loading happens in the constructor so that a malformed or missing data file
/// fails the application at startup rather than on a user's first request.
/// </para>
/// <para>
/// Every collection handed out is wrapped in a <see cref="ReadOnlyCollection{T}"/>
/// or <see cref="ReadOnlyDictionary{TKey, TValue}"/>: <see cref="IReadOnlyList{T}"/>
/// and <see cref="IReadOnlyDictionary{TKey, TValue}"/> are interfaces, not
/// guarantees, so a caller could otherwise downcast to the backing list or
/// dictionary and mutate the shared singleton. Wrapping makes the immutability
/// claim a runtime guarantee instead of a convention.
/// </para>
/// </remarks>
/// <example>
/// The service is registered once and injected, never constructed by hand:
/// <code>
/// builder.Services.AddSingleton&lt;IMenuService, JsonMenuService&gt;();
/// ...
/// IReadOnlyList&lt;MenuItem&gt; mains = menuService.GetByCategory(MenuCategory.Mains);
/// </code>
/// </example>
public sealed class JsonMenuService : IMenuService
{
    /// <summary>
    /// Categories in declaration order, wrapped once. Shared by every render of
    /// the navigation bar, so the enum's array is not reallocated per request.
    /// </summary>
    private static readonly ReadOnlyCollection<MenuCategory> Categories =
        Array.AsReadOnly(Enum.GetValues<MenuCategory>());

    /// <summary>
    /// Folder under the web root that holds the dish photographs, relative and
    /// slash-delimited so it composes into a URL the same way on every OS.
    /// </summary>
    private const string ImageDirectory = "images/menu";

    private readonly ReadOnlyCollection<MenuItem> items;
    private readonly IReadOnlyDictionary<int, MenuItem> itemsById;
    private readonly IReadOnlyDictionary<MenuCategory, int> categoryCounts;

    /// <summary>
    /// Resolves, reads and validates the menu file, then indexes it in memory.
    /// </summary>
    /// <param name="options">Supplies <see cref="MenuDataOptions.FilePath"/>.</param>
    /// <param name="environment">Supplies the content root for relative paths.</param>
    /// <param name="logger">Receives one informational line on success.</param>
    /// <exception cref="MenuDataException">
    /// The file is missing, unreadable, not valid JSON, or violates a
    /// <see cref="MenuItem"/> invariant. The message names the file and, where
    /// applicable, the offending item.
    /// </exception>
    public JsonMenuService(
        IOptions<MenuDataOptions> options,
        IWebHostEnvironment environment,
        ILogger<JsonMenuService> logger)
    {
        string configuredPath = options.Value.FilePath;
        string relativePath = string.IsNullOrWhiteSpace(configuredPath)
            ? MenuDataOptions.DefaultFilePath
            : configuredPath;

        SourcePath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(environment.ContentRootPath, relativePath);

        if (!File.Exists(SourcePath))
        {
            throw new MenuDataException(
                $"Menu data file not found at '{SourcePath}'. " +
                $"Check the '{MenuDataOptions.SectionName}:{nameof(MenuDataOptions.FilePath)}' setting " +
                "and ensure Data/menu.json is copied to the output directory.");
        }

        IReadOnlyList<MenuItem> loadedItems;

        try
        {
            using FileStream stream = File.OpenRead(SourcePath);
            loadedItems = MenuJsonReader.Parse(stream, SourcePath);
        }
        catch (IOException exception)
        {
            throw new MenuDataException($"Could not read menu data file '{SourcePath}': {exception.Message}", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new MenuDataException($"Access denied reading menu data file '{SourcePath}': {exception.Message}", exception);
        }

        // Wrapped so a caller cannot downcast back to List<MenuItem> and mutate
        // the singleton's data for every subsequent request.
        items = new ReadOnlyCollection<MenuItem>(loadedItems.ToList());

        // Both lookup tables are wrapped for the same reason: an unwrapped
        // Dictionary<TKey, TValue> is reachable through the IReadOnly interfaces
        // and is not thread-safe, so a write from one request could corrupt what
        // every other request renders.
        itemsById = new ReadOnlyDictionary<int, MenuItem>(items.ToDictionary(item => item.Id));
        categoryCounts = new ReadOnlyDictionary<MenuCategory, int>(
            items
                .GroupBy(item => item.Category)
                .ToDictionary(group => group.Key, group => group.Count()));

        logger.LogInformation(
            "Loaded {ItemCount} menu items across {CategoryCount} categories from {MenuDataPath}",
            items.Count,

            // Distinct categories present in the data, not the enum's length: a
            // menu with one empty section must not log a section it cannot show.
            categoryCounts.Count,
            SourcePath);

        VerifyImagesExist(environment);
    }

    /// <summary>
    /// Fails the host when an item names an image that is not on disk.
    /// </summary>
    /// <param name="environment">Supplies the web root that holds <c>images/menu</c>.</param>
    /// <exception cref="MenuDataException">
    /// An item's <see cref="MenuItem.ImageFile"/> does not resolve to a file under
    /// <c>wwwroot/images/menu</c>, or the web root is unknown.
    /// </exception>
    /// <remarks>
    /// The same reasoning as failing on a missing data file: a broken photograph is a
    /// deploy defect, and discovering it at startup names the file and the item. Left
    /// to the browser it would surface as a silent blank tile in production only.
    /// Items without an <c>imageFile</c> are fine and fall back to the category glyph.
    /// </remarks>
    private void VerifyImagesExist(IWebHostEnvironment environment)
    {
        string? webRoot = environment.WebRootPath;

        // Empty is as unusable as null here: Path.Combine("") would resolve
        // against the process working directory, which is not the web root.
        bool hasWebRoot = !string.IsNullOrEmpty(webRoot);
        string imageFolder = hasWebRoot ? Path.Combine(webRoot!, ImageDirectory) : $"(unknown web root)/{ImageDirectory}";

        List<string> missing = [];

        foreach (MenuItem item in items)
        {
            if (item.ImageFile is null)
            {
                continue;
            }

            if (!hasWebRoot || !File.Exists(Path.Combine(imageFolder, item.ImageFile)))
            {
                // Item id and name too: a wrong file name is usually a copy-paste
                // between neighbouring rows, and naming the row is what fixes it.
                missing.Add($"{item.ImageFile} (item {item.Id} '{item.Name}')");
            }
        }

        if (missing.Count > 0)
        {
            throw new MenuDataException(
                $"Menu data '{SourcePath}' references {missing.Count} image file(s) that do not exist under " +
                $"'{imageFolder}': {string.Join(", ", missing)}. " +
                "Add the files or remove their 'imageFile' values.");
        }
    }

    /// <summary>
    /// Absolute path the menu was loaded from. Surfaced for diagnostics: it is
    /// the single most useful value when a deployment cannot find its data.
    /// </summary>
    public string SourcePath { get; }

    /// <inheritdoc />
    public IReadOnlyList<MenuItem> GetAll() => items;

    /// <inheritdoc />
    public IReadOnlyList<MenuItem> GetByCategory(MenuCategory? category)
    {
        if (category is null)
        {
            return items;
        }

        return items.Where(item => item.Category == category.Value).ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public MenuItem? GetById(int id) => itemsById.GetValueOrDefault(id);

    /// <inheritdoc />
    public IReadOnlyList<MenuCategory> GetCategories() => Categories;

    /// <inheritdoc />
    public IReadOnlyDictionary<MenuCategory, int> GetCategoryCounts() => categoryCounts;

    /// <inheritdoc />
    /// <remarks>
    /// The pool of eligible dishes is rebuilt per call. That is deliberate at this
    /// size (16 items): it keeps the method pure and obvious. If the menu grew by
    /// orders of magnitude the pool would move to a field built in the constructor.
    /// </remarks>
    public IReadOnlyList<MenuItem> GetSpecials(DateOnly date, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        // Only well-rated dishes are ever "on special", so the rotation always
        // advertises something the kitchen is proud of.
        List<MenuItem> specialsPool = items
            .Where(item => item.Rating >= 4)
            .OrderBy(item => item.Id)
            .ToList();

        if (specialsPool.Count == 0)
        {
            return [];
        }

        int takeCount = Math.Min(count, specialsPool.Count);
        int dayOffset = date.DayNumber % specialsPool.Count;

        return Enumerable.Range(0, takeCount)
            .Select(index => specialsPool[(dayOffset + index) % specialsPool.Count])
            .ToList()
            .AsReadOnly();
    }
}