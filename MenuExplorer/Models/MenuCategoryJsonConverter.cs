/* ==========================================================================
 *  Menu Explorer — MenuCategoryJsonConverter
 * ==========================================================================
 *  Role    Read and write MenuCategory in menu.json as a declared name.
 *  Layer   Models. Used only by MenuJsonReader.
 *  Debug   On an unknown value it throws MenuCategoryJsonException, a distinct
 *          type so the reader can report "invalid category" instead of blaming
 *          the JSON syntax. Both cases are valid JSON; only one is a data error.
 * ========================================================================== */

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MenuExplorer.Models;

/// <summary>
/// Reads and writes <see cref="MenuCategory"/> as its member name, case-insensitively.
/// </summary>
/// <remarks>
/// <para>
/// Used instead of the built-in <c>JsonStringEnumConverter</c> so that an
/// unknown category produces a message naming the offending value and the
/// allowed set. The default converter's message ("could not be converted") tells
/// whoever is editing the JSON nothing about how to fix it.
/// </para>
/// <para>
/// Name matching is delegated to <see cref="MenuCategoryParser"/> rather than
/// reimplemented, so the data file and the query string can never accept
/// different spellings of the same category.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// { "id": 5, "name": "Dry-Aged Sirloin", "category": "Mains", ... }   // accepted
/// { "id": 5, "name": "Dry-Aged Sirloin", "category": "main"  }        // rejected
/// { "id": 5, "name": "Dry-Aged Sirloin", "category": "1"     }        // rejected
/// </code>
/// </example>
public sealed class MenuCategoryJsonConverter : JsonConverter<MenuCategory>
{
    /// <summary>
    /// Prefix shared by every message this converter throws. Kept public so the
    /// reader's error-classification tests can assert on it; the reader itself
    /// branches on <see cref="MenuCategoryJsonException"/>, never on this text.
    /// </summary>
    public const string UnknownCategoryMessagePrefix = "Unknown menu category";

    /// <inheritdoc />
    /// <exception cref="MenuCategoryJsonException">
    /// The value is not a JSON string, or is a string that names no declared
    /// <see cref="MenuCategory"/>. The message names the bad value and lists the
    /// allowed set.
    /// </exception>
    public override MenuCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new MenuCategoryJsonException(
                $"{UnknownCategoryMessagePrefix}: expected a string but found {reader.TokenType}.");
        }

        string? rawValue = reader.GetString();

        // Same rule as the query string: declared names only, so "1" and
        // "Starters,Mains" are rejected rather than silently meaning Mains.
        if (MenuCategoryParser.TryParse(rawValue, out MenuCategory category))
        {
            return category;
        }

        throw new MenuCategoryJsonException(
            $"{UnknownCategoryMessagePrefix} '{rawValue}'. Allowed categories: {string.Join(", ", Enum.GetNames<MenuCategory>())}.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, MenuCategory value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

/// <summary>
/// A JSON error caused by an invalid <see cref="MenuCategory"/> value rather than
/// by malformed JSON. Exists as its own type so <see cref="Services.MenuJsonReader"/>
/// can classify the failure by type instead of by matching message text.
/// </summary>
public sealed class MenuCategoryJsonException : JsonException
{
    /// <param name="message">Actionable message naming the value and allowed set.</param>
    public MenuCategoryJsonException(string message) : base(message)
    {
    }
}