/* ==========================================================================
 *  Menu Explorer — ErrorViewModel
 * ==========================================================================
 *  Role    Content for the terminal error page (production exception handler).
 *  Layer   Models. Constructed by HomeController.Error.
 *  Debug   RequestId is the whole point of this type: it is the value a user
 *          quotes so the matching stack trace can be found in the logs.
 * ========================================================================== */

namespace MenuExplorer.Models;

/// <summary>View model for the shared error page.</summary>
/// <example>
/// <code>
/// new ErrorViewModel
/// {
///     RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
/// };
/// // The view renders "Reference: 0HN7...:00000001" for support.
/// </code>
/// </example>
public sealed class ErrorViewModel
{
    /// <summary>
    /// Correlates the rendered page with a server log entry. May be <c>null</c>
    /// outside a request (for example in a test), in which case the reference
    /// block is omitted rather than rendered empty.
    /// </summary>
    public string? RequestId { get; init; }

    /// <summary>Whether a reference can be shown to the user.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}