/* ==========================================================================
 *  Menu Explorer.Tests — TempDirectory
 * ==========================================================================
 *  Role    A unique directory under the system temp path, deleted on dispose.
 *  Layer   Test support.
 *  Debug   Cleanup is best-effort: a locked file must never turn a passing test
 *          into a dispose-time failure that hides the real result. Leaked
 *          directories live under menu-explorer-tests/ and are safe to delete.
 * ========================================================================== */

namespace MenuExplorer.Tests.TestSupport;

/// <summary>
/// Creates a unique directory under the system temp path and deletes it on
/// dispose, so file-system tests never leak state into each other.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "menu-explorer-tests",
            Guid.NewGuid().ToString("n"));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string WriteFile(string relativePath, string contents)
    {
        string fullPath = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup: a locked or read-only temp file must never
            // surface as a dispose-time test failure.
        }
    }
}