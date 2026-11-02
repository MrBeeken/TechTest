/* ==========================================================================
 *  Menu Explorer — MenuDataException
 * ==========================================================================
 *  Role    The single failure type for menu data: missing file, unreadable
 *          file, malformed JSON, or a violated MenuItem invariant.
 *  Layer   Services. Thrown only during startup, while the singleton is built.
 *  Debug   The message always names the file (and the item id where one exists)
 *          and never wraps a NullReferenceException, so the first line of the
 *          log is the fix. The inner exception is preserved for the stack.
 * ========================================================================== */

namespace MenuExplorer.Services;

/// <summary>
/// Raised when the menu data file is missing, unreadable, or violates the
/// <see cref="Models.MenuItem"/> invariants. The message is written to be
/// actionable for whoever is running the app.
/// </summary>
/// <example>
/// <code>
/// Menu data 'C:\app\Data\menu.json' contains item 3 ('   ') with a blank name.
/// Menu data 'C:\app\Data\menu.json' has an invalid category. Unknown menu
///   category 'Burgers'. Allowed categories: Starters, Mains, Desserts, Drinks.
/// </code>
/// </example>
public sealed class MenuDataException : Exception
{
    /// <param name="message">Actionable message naming the file and the problem.</param>
    public MenuDataException(string message) : base(message)
    {
    }

    /// <param name="message">Actionable message naming the file and the problem.</param>
    /// <param name="innerException">
    /// The underlying <c>IOException</c> or <c>JsonException</c>, kept so a
    /// structured log still shows where in the document the failure happened.
    /// </param>
    public MenuDataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}