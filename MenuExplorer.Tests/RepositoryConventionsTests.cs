/* ==========================================================================
 *  Menu Explorer.Tests — RepositoryConventionsTests
 * ==========================================================================
 *  Role    Enforces the formatting rules that no compiler checks, starting with
 *          "no text file ends with a newline".
 *  Layer   Tests, file-system. Walks the repository from the test binary's
 *          location, so it runs anywhere the solution is built.
 *  Debug   A failure names the offending file. If this fires after an editor or a
 *          formatter saved a file, the fix is to delete the final empty line — the
 *          rule is deliberate (see .editorconfig) and this test is what keeps it
 *          from drifting back.
 * ========================================================================== */

using System.Text;

namespace MenuExplorer.Tests;

/// <summary>
/// Repository-wide formatting conventions, enforced as tests rather than
/// documented as intentions.
/// </summary>
/// <remarks>
/// The trailing-newline rule exists because every file in this repository ends on
/// its last line of content. A test is the only enforcement that survives a
/// different editor, a missing plugin, or <c>dotnet format</c> on a machine whose
/// <c>.editorconfig</c> is ignored.
/// </remarks>
public class RepositoryConventionsTests
{
    /// <summary>Extensions treated as text and therefore subject to the rule.</summary>
    private static readonly string[] TextExtensions =
    [
        ".cs", ".cshtml", ".css", ".js", ".json", ".csproj", ".sln", ".md", ".yml", ".yaml", ".props", ".editorconfig"
    ];

    /// <summary>Directories never walked: build output and version control internals.</summary>
    private static readonly string[] SkippedDirectories = ["bin", "obj", ".git", "node_modules", "artifacts"];

    /// <summary>
    /// No tracked text file may end with a newline, so the last line of every file
    /// is its last line of content.
    /// </summary>
    [Fact]
    public void NoSourceFileEndsWithANewline()
    {
        string repositoryRoot = FindRepositoryRoot();

        List<string> offenders = [];
        int inspected = 0;

        foreach (string filePath in EnumerateTextFiles(repositoryRoot))
        {
            inspected++;

            byte[] content = File.ReadAllBytes(filePath);

            if (content.Length > 0 && content[^1] == '\n')
            {
                offenders.Add(Path.GetRelativePath(repositoryRoot, filePath));
            }
        }

        // Guard against a silent no-op: if the walk found nothing, the rule would
        // pass without checking anything.
        Assert.True(inspected > 30, $"Only {inspected} text files were inspected; the walk is probably misconfigured.");

        Assert.True(
            offenders.Count == 0,
            "These files end with a newline, leaving a blank last line:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// Source files are UTF-8 without a byte-order mark, so a diff never shows a
    /// phantom change on the first line.
    /// </summary>
    [Fact]
    public void SourceFilesAreUtf8WithoutAByteOrderMark()
    {
        string repositoryRoot = FindRepositoryRoot();
        UTF8Encoding strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        List<string> offenders = [];

        foreach (string filePath in EnumerateTextFiles(repositoryRoot))
        {
            byte[] content = File.ReadAllBytes(filePath);

            if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
            {
                offenders.Add($"{Path.GetRelativePath(repositoryRoot, filePath)} (byte-order mark)");
                continue;
            }

            try
            {
                strictUtf8.GetString(content);
            }
            catch (DecoderFallbackException)
            {
                offenders.Add($"{Path.GetRelativePath(repositoryRoot, filePath)} (not valid UTF-8)");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "These files are not plain UTF-8:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// Locates the repository root by walking up from the test binary until the
    /// solution file is found.
    /// </summary>
    /// <returns>The absolute path of the directory containing the solution.</returns>
    /// <exception cref="InvalidOperationException">No solution file was found above the test binary.</exception>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MenuExplorer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find MenuExplorer.sln above '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Yields every non-skipped text file under <paramref name="root"/>.
    /// </summary>
    /// <param name="root">Repository root.</param>
    /// <returns>Absolute paths, ordered for a stable failure message.</returns>
    private static IEnumerable<string> EnumerateTextFiles(string root)
    {
        IEnumerable<string> paths = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !IsSkipped(path))
            .Where(path => TextExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal);

        return paths;
    }

    /// <summary>
    /// Whether a path lives inside a skipped directory.
    /// </summary>
    /// <param name="path">Absolute candidate path.</param>
    /// <returns><c>true</c> when any path segment is skipped.</returns>
    private static bool IsSkipped(string path)
    {
        string[] segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Any(segment => SkippedDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }
}