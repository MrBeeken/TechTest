/* ==========================================================================
 *  Menu Explorer — MenuJsonReader
 * ==========================================================================
 *  Role    Turn raw menu JSON into validated MenuItem instances.
 *  Layer   Services. Pure: a Stream in, a read-only list out. No file system,
 *          no host, no logging, so every rule here is unit-testable in memory.
 *  Debug   Every failure throws MenuDataException naming the source file and,
 *          where it exists, the offending item id. Three failure kinds are kept
 *          distinct: malformed JSON, an invalid category value, and a violated
 *          invariant.
 * ========================================================================== */

using System.Text.Json;
using MenuExplorer.Models;

namespace MenuExplorer.Services;

/// <summary>
/// Turns the raw menu JSON into validated <see cref="MenuItem"/> instances.
/// </summary>
/// <remarks>
/// Kept separate from <see cref="JsonMenuService"/> so the deserialization and
/// validation rules can be unit tested against an in-memory stream, with no
/// file system or hosting environment involved.
/// </remarks>
/// <example>
/// <code>
/// using FileStream stream = File.OpenRead(path);
/// IReadOnlyList&lt;MenuItem&gt; items = MenuJsonReader.Parse(stream, path);
/// </code>
/// </example>
public static class MenuJsonReader
{
    /// <summary>
    /// Lenient about formatting (comments, trailing commas, casing) and strict
    /// about meaning (ids, prices, ratings, required fields).
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new MenuCategoryJsonConverter() }
    };

    /// <summary>
    /// Deserialises and validates a menu document.
    /// </summary>
    /// <param name="json">Readable stream containing a JSON array of menu items.</param>
    /// <param name="source">
    /// File path or label used in error messages. Pass the resolved absolute path
    /// so a startup failure tells the operator exactly which file to edit.
    /// </param>
    /// <returns>Items ordered by category, then name, for a stable display order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="MenuDataException">
    /// The stream is not a valid menu document, contains no items, contains a
    /// <c>null</c> element, repeats an id, or contains an item that violates a
    /// <see cref="MenuItem"/> invariant.
    /// </exception>
    /// <example>
    /// A valid document — every property is required except <c>isVegetarian</c>:
    /// <code>
    /// [
    ///   { "id": 5, "name": "Dry-Aged Sirloin", "description": "300g sirloin.",
    ///     "price": 285.00, "category": "Mains", "isVegetarian": false, "rating": 5 }
    /// ]
    /// </code>
    /// </example>
    public static IReadOnlyList<MenuItem> Parse(Stream json, string source)
    {
        ArgumentNullException.ThrowIfNull(json);

        List<MenuItem?>? deserializedItems;
        try
        {
            deserializedItems = JsonSerializer.Deserialize<List<MenuItem?>>(json, SerializerOptions);
        }
        catch (MenuCategoryJsonException exception)
        {
            // Valid JSON, invalid value: the message must not blame the syntax.
            throw new MenuDataException($"Menu data '{source}' has an invalid category. {exception.Message}", exception);
        }
        catch (JsonException exception)
        {
            // Covers a syntax error and System.Text.Json's own "missing required
            // property" failure. Both messages are appended so the operator sees
            // which property or which token is at fault.
            throw new MenuDataException(
                $"Menu data '{source}' is not valid JSON (a syntax error, or a required property is missing): {exception.Message}",
                exception);
        }

        if (deserializedItems is null || deserializedItems.Count == 0)
        {
            throw new MenuDataException($"Menu data '{source}' does not contain any menu items.");
        }

        List<MenuItem> items = new(deserializedItems.Count);
        HashSet<int> seenIds = new(deserializedItems.Count);

        // One pass for three checks, so the index reported for a null element is
        // the index in the file the operator is looking at.
        for (int index = 0; index < deserializedItems.Count; index++)
        {
            // A JSON array may legally contain null ("[null]"), and
            // System.Text.Json happily materialises it. Checking here keeps the
            // failure an actionable MenuDataException rather than a NullReferenceException.
            if (deserializedItems[index] is not MenuItem item)
            {
                throw new MenuDataException(
                    $"Menu data '{source}' contains a null entry at index {index}; every array element must be a menu item object.");
            }

            Validate(item, source);

            if (!seenIds.Add(item.Id))
            {
                throw new MenuDataException($"Menu data '{source}' contains duplicate item id {item.Id}.");
            }

            items.Add(item);
        }

        return items
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Enforces every <see cref="MenuItem"/> invariant, throwing on the first
    /// violation found.
    /// </summary>
    /// <param name="item">The deserialised item to check.</param>
    /// <param name="source">File path or label used in the message.</param>
    /// <exception cref="MenuDataException">An invariant is violated.</exception>
    private static void Validate(MenuItem item, string source)
    {
        string label = $"Item {item.Id} ('{item.Name}')";

        if (item.Id <= 0)
        {
            throw new MenuDataException($"Menu data '{source}' contains an item with a non-positive id ({item.Id}).");
        }

        if (string.IsNullOrWhiteSpace(item.Name))
        {
            throw new MenuDataException($"Menu data '{source}' contains item {item.Id} with a blank name.");
        }

        if (string.IsNullOrWhiteSpace(item.Description))
        {
            throw new MenuDataException($"Menu data '{source}' contains {label} with a blank description.");
        }

        if (item.Price < 0m)
        {
            throw new MenuDataException($"Menu data '{source}' contains {label} with a negative price ({item.Price}).");
        }

        if (item.Rating is < MenuItemRatings.Min or > MenuItemRatings.Max)
        {
            throw new MenuDataException(
                $"Menu data '{source}' contains {label} with rating {item.Rating}; expected {MenuItemRatings.Min}–{MenuItemRatings.Max}.");
        }

        if (item.ImageFile is not null && !IsSafeImageFileName(item.ImageFile))
        {
            throw new MenuDataException(
                $"Menu data '{source}' contains {label} with imageFile '{item.ImageFile}'; " +
                "expected a bare file name inside images/menu (letters, digits, hyphens, dots), with no directory separators or '..'.");
        }
    }

    /// <summary>
    /// Whether an <c>imageFile</c> value is a single, safe file name.
    /// </summary>
    /// <param name="fileName">The value as written in the data file.</param>
    /// <returns><c>true</c> when the value names one file in the images folder.</returns>
    /// <remarks>
    /// The data file is not user input, but it is edited by hand and it is the one
    /// place a path could sneak into a URL. Rejecting separators and <c>..</c> here
    /// means the composed URL can never escape <c>/images/menu/</c>, even if someone
    /// later adds an item from a generated file.
    /// </remarks>
    private static bool IsSafeImageFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 128)
        {
            return false;
        }

        if (fileName.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        // Path.GetFileName returns the input unchanged only when there is no
        // directory component of any kind ('/', '\' or a drive qualifier).
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
        {
            return false;
        }

        return Path.GetExtension(fileName) is { Length: > 1 } extension
            && Path.GetFileNameWithoutExtension(fileName).Length > 0
            && extension[1..].All(char.IsAsciiLetterOrDigit);
    }
}