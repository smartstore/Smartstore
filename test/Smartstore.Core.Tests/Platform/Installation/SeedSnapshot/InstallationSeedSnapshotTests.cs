using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.Installation;

namespace Smartstore.Core.Tests.Platform.Installation.SeedSnapshot;

/// <summary>
/// Guards the installation seed data against unintended changes. The real <see cref="InstallationDataSeeder"/>
/// runs per culture, the resulting database content is normalized and compared with the committed baseline
/// in "Baseline/{culture}".
/// </summary>
/// <remarks>
/// To accept intended changes, run the tests with the environment variable
/// <c>SMARTSTORE_UPDATE_SEED_SNAPSHOT=1</c> and review the baseline diff before committing it.
/// </remarks>
[TestFixture]
[NonParallelizable]
[Category("SeedSnapshot")]
public class InstallationSeedSnapshotTests
{
    private const string UpdateVariable = "SMARTSTORE_UPDATE_SEED_SNAPSHOT";

    private static IEnumerable<TestCaseData> Cultures()
    {
        yield return new TestCaseData("en-US", "English", "en", "us.png").SetArgDisplayNames("en-US");
        yield return new TestCaseData("de-DE", "Deutsch", "de", "de.png").SetArgDisplayNames("de-DE");
    }

    [TestCaseSource(nameof(Cultures))]
    public async Task Seeded_data_matches_baseline(string culture, string languageName, string uniqueSeoCode, string flagImageFileName)
    {
        var repoRoot = FindRepositoryRoot();
        var seedDir = Path.Combine(repoRoot, "src", "Smartstore.Web", "App_Data", "Seed");
        var baselineDir = Path.Combine(repoRoot, "test", "Smartstore.Core.Tests", "Platform", "Installation", "SeedSnapshot", "Baseline", culture);

        var snapshot = await CreateSnapshotAsync(seedDir, culture, languageName, uniqueSeoCode, flagImageFileName);

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            WriteSnapshot(baselineDir, snapshot);
            Assert.Pass($"Baseline for '{culture}' updated ({snapshot.Count} files). Review the diff before committing.");
        }

        Assert.That(Directory.Exists(baselineDir), Is.True,
            $"No baseline found at '{baselineDir}'. Run the tests with {UpdateVariable}=1 to create it.");

        var differences = Compare(baselineDir, snapshot);
        if (differences.Count > 0)
        {
            var actualDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "SeedSnapshot", "Actual", culture);
            WriteSnapshot(actualDir, snapshot);

            var message = new StringBuilder()
                .AppendLine($"Seed data for '{culture}' differs from the baseline in {differences.Count} file(s).")
                .AppendLine($"Actual snapshot: {actualDir}")
                .AppendLine($"Baseline:        {baselineDir}")
                .AppendLine()
                .AppendJoin(Environment.NewLine, differences.Take(30));

            Assert.Fail(message.ToString());
        }
    }

    [Test]
    public async Task Snapshot_is_deterministic()
    {
        var seedDir = Path.Combine(FindRepositoryRoot(), "src", "Smartstore.Web", "App_Data", "Seed");

        var first = await CreateSnapshotAsync(seedDir, "de-DE", "Deutsch", "de", "de.png");
        var second = await CreateSnapshotAsync(seedDir, "de-DE", "Deutsch", "de", "de.png");

        Assert.That(second.Keys, Is.EqualTo(first.Keys));

        var changed = first.Keys.Where(x => first[x] != second[x]).ToList();
        Assert.That(changed, Is.Empty, "Snapshot files differ between two identical runs: " + string.Join(", ", changed));
    }

    private static async Task<SortedDictionary<string, string>> CreateSnapshotAsync(
        string seedDir,
        string culture,
        string languageName,
        string uniqueSeoCode,
        string flagImageFileName)
    {
        var seededAtUtc = DateTime.UtcNow;

        using var harness = await SeedSnapshotHarness.SeedAsync(seedDir, culture, languageName, uniqueSeoCode, flagImageFileName);
        return SeedSnapshotWriter.CreateSnapshot(harness.Db, seededAtUtc);
    }

    private static List<string> Compare(string baselineDir, SortedDictionary<string, string> snapshot)
    {
        var differences = new List<string>();
        var baselineFiles = Directory.EnumerateFiles(baselineDir, "*.json")
            .ToDictionary(Path.GetFileName, x => x, StringComparer.Ordinal);

        foreach (var name in baselineFiles.Keys.Except(snapshot.Keys).OrderBy(x => x, StringComparer.Ordinal))
        {
            differences.Add($"- {name}: missing in actual snapshot");
        }

        foreach (var (name, content) in snapshot)
        {
            if (!baselineFiles.TryGetValue(name, out var path))
            {
                differences.Add($"- {name}: not in baseline");
                continue;
            }

            // Line endings depend on the checkout (CRLF with core.autocrlf on Windows, LF on Linux CI).
            var expected = File.ReadAllText(path).ReplaceLineEndings("\n");
            var actual = content.ReplaceLineEndings("\n");
            if (expected != actual)
            {
                differences.Add($"- {name}: {DescribeFirstDifference(expected, actual)}");
            }
        }

        return differences;
    }

    private static string DescribeFirstDifference(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');

        for (var i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var e = i < expectedLines.Length ? expectedLines[i].Trim() : "<eof>";
            var a = i < actualLines.Length ? actualLines[i].Trim() : "<eof>";

            if (e != a)
            {
                return $"line {i + 1}: expected `{Truncate(e)}` but was `{Truncate(a)}`";
            }
        }

        return "content differs";

        static string Truncate(string value)
            => value.Length > 120 ? value[..120] + "…" : value;
    }

    private static void WriteSnapshot(string dir, SortedDictionary<string, string> snapshot)
    {
        Directory.CreateDirectory(dir);

        foreach (var obsolete in Directory.EnumerateFiles(dir, "*.json").Where(x => !snapshot.ContainsKey(Path.GetFileName(x))))
        {
            File.Delete(obsolete);
        }

        foreach (var (name, content) in snapshot)
        {
            File.WriteAllText(Path.Combine(dir, name), content, new UTF8Encoding(false));
        }
    }

    internal static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Smartstore.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root (Smartstore.sln).");
    }
}
