/* ==========================================================================
 *  Menu Explorer — MenuDataOptions
 * ==========================================================================
 *  Role    Strongly typed view of the "MenuData" configuration section.
 *  Layer   Services. Bound once from configuration; never mutated at runtime.
 *  Debug   This is the setting named in the "file not found" message, so a
 *          deployment problem points straight at the key to override, e.g.
 *          MenuData__FilePath=/etc/menu/menu.json.
 * ========================================================================== */

namespace MenuExplorer.Services;

/// <summary>
/// Strongly typed view of the <c>MenuData</c> configuration section.
/// </summary>
/// <example>
/// <code>
/// {
///   "MenuData": {
///     "FilePath": "Data/menu.json"
///   }
/// }
/// </code>
/// </example>
public sealed class MenuDataOptions
{
    /// <summary>Configuration section this type binds to.</summary>
    public const string SectionName = "MenuData";

    /// <summary>
    /// Path to the menu JSON file. Relative paths are resolved against the
    /// application's content root; absolute paths are used as-is.
    /// </summary>
    public const string DefaultFilePath = "Data/menu.json";

    /// <summary>
    /// Configured path. Blank or whitespace falls back to
    /// <see cref="DefaultFilePath"/> rather than resolving to the content root
    /// itself, which would fail with a directory error instead of a data error.
    /// </summary>
    public string FilePath { get; set; } = DefaultFilePath;
}