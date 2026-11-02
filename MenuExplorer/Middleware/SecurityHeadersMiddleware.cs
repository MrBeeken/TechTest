/* ==========================================================================
 *  Menu Explorer — SecurityHeadersMiddleware
 * ==========================================================================
 *  Role    Adds the baseline response headers every page should carry:
 *          no-sniff, no-framing, a referrer policy, and a same-origin CSP.
 *  Layer   Middleware, registered once in Program.cs before routing.
 *  Debug   If a page suddenly loses its styling or the search silently stops,
 *          check this CSP first: 'self' is the whole allow-list, so any future
 *          inline <style>/<script>, CDN font or third-party image will be blocked
 *          on purpose. Serving it from wwwroot (as everything here is) just works.
 * ========================================================================== */

namespace MenuExplorer.Middleware;

/// <summary>
/// Adds baseline security headers to every response.
/// </summary>
/// <remarks>
/// <para>
/// The Content Security Policy is the meaningful part. It is strict enough to be
/// worth having (<c>'self'</c> only, no inline script or style) and affordable
/// here because the app ships zero inline <c>&lt;style&gt;</c> blocks, zero
/// <c>style=""</c> attributes, zero inline scripts and no third-party assets.
/// </para>
/// <para>
/// <c>frame-ancestors 'none'</c> supersedes <c>X-Frame-Options</c> in modern
/// browsers, and the older header is sent alongside it for the ones that ignore
/// CSP entirely.
/// </para>
/// <para>
/// HSTS is deliberately absent: it is only meaningful over HTTPS and is already
/// added by <c>UseHsts()</c> in non-development environments.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// app.UseMiddleware&lt;SecurityHeadersMiddleware&gt;();
/// </code>
/// produces, on every response:
/// <code>
/// X-Content-Type-Options: nosniff
/// X-Frame-Options: DENY
/// Referrer-Policy: same-origin
/// Content-Security-Policy: default-src 'self'; ...; frame-ancestors 'none'
/// </code>
/// </example>
public sealed class SecurityHeadersMiddleware
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self'; " +
        "script-src 'self'; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    private readonly RequestDelegate next;

    /// <param name="next">The rest of the pipeline.</param>
    public SecurityHeadersMiddleware(RequestDelegate next) => this.next = next;

    /// <summary>
    /// Stamps the headers on the response and continues.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <returns>A task that completes when the rest of the pipeline has run.</returns>
    /// <remarks>
    /// Headers are set before <c>next</c> so they are present even when a later
    /// component short-circuits with a 404 or a 500.
    /// </remarks>
    public Task InvokeAsync(HttpContext context)
    {
        IHeaderDictionary headers = context.Response.Headers;

        // Set rather than Append: a re-executed error page must not end up with
        // the header twice.
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "same-origin";
        headers.ContentSecurityPolicy = ContentSecurityPolicy;

        return next(context);
    }
}