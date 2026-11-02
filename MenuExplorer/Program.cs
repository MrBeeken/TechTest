/* ==========================================================================
 *  Menu Explorer — Program.cs (composition root)
 * ==========================================================================
 *  Role    Builds the host: content root, services, middleware order, routes.
 *  Layer   Composition root. The only place that wires concrete types together.
 *  Debug   Read top to bottom and you have the whole pipeline. Two deliberate
 *          decisions are easy to break:
 *            1. ContentRootPath is pinned to AppContext.BaseDirectory, so a
 *               published app finds its own Data/menu.json from any directory.
 *            2. IMenuService is resolved before Run(), so bad data fails at
 *               startup instead of on a visitor's first request.
 * ========================================================================== */

using MenuExplorer.Middleware;
using MenuExplorer.Services;

// Where wwwroot lives depends on how the app was started:
//   published    -> <app dir>/wwwroot, beside the assembly;
//   dotnet run   -> the project's wwwroot, because the build does not copy it
//                   into bin/ (static assets are served from the manifest);
//   tests        -> the test project copies the web project's wwwroot beside
//                   the test assembly.
// Resolve it explicitly: WebRootPath defaults to <ContentRootPath>/wwwroot and
// the content root is pinned below, which would leave a `dotnet run` app with
// no web root at all. Null here means no folder exists and the startup image
// check reports that rather than serving broken tiles.
string publishedWebRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
string workingDirectoryWebRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
string? webRoot = Directory.Exists(publishedWebRoot) ? publishedWebRoot
    : Directory.Exists(workingDirectoryWebRoot) ? workingDirectoryWebRoot
    : null;

WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,

    // Anchor the content root to the application's own directory. Without this,
    // a published app resolves Data/menu.json and appsettings.json against the
    // *current working directory*, so launching it from anywhere other than its
    // publish folder would fail to find its own data.
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = webRoot
});

// Options: strongly typed access to the "MenuData" configuration section.
builder.Services.Configure<MenuDataOptions>(
    builder.Configuration.GetSection(MenuDataOptions.SectionName));

// The menu is immutable at runtime, so a singleton (loaded once) is correct.
builder.Services.AddSingleton<IMenuService, JsonMenuService>();
builder.Services.AddSingleton(TimeProvider.System);

// One instance per request: records which menu section the page resolved so the
// shared category navigation never re-parses the query string. See the type's
// flowerbox for the bug that made this necessary.
builder.Services.AddScoped<MenuNavigationContext>();

builder.Services.AddControllersWithViews();

builder.Services
    .AddHealthChecks()
    .AddCheck<MenuHealthCheck>("menu-data");

WebApplication app = builder.Build();

// Force the menu to load now: invalid or missing JSON stops the app at startup
// instead of failing on a user's first request. The discard is deliberate — the
// call is for its side effect (construction + validation), not for its value.
_ = app.Services.GetRequiredService<IMenuService>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Baseline headers on every response, including the error and 404 pages below.
app.UseMiddleware<SecurityHeadersMiddleware>();

// Re-execute bodiless error responses (unknown routes, 403s) as friendly pages
// while preserving the original status code.
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();

// {id:int?} rejects non-numeric ids at the routing layer, so /Menu/Details/abc
// is a 404 rather than binding to 0 and rendering "menu item 0".
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id:int?}")
    .WithStaticAssets();

app.MapHealthChecks("/health");

app.Run();

/// <summary>Exposed for integration tests via <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program
{
}