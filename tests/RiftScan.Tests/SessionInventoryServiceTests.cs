using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class SessionInventoryServiceTests
{
    [Fact]
    public void Inventory_combines_summary_and_prune_candidates_without_mutating_session()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-generated-artifacts");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "report.md"), "# report\n");
            File.WriteAllText(Path.Combine(sessionPath, "clusters.jsonl"), "{\"schema_version\":\"riftscan.structure_cluster.v1\"}\n");

            var beforeFiles = RelativeFilePaths(sessionPath);
            var result = new SessionInventoryService().Inventory(sessionPath);
            var afterFiles = RelativeFilePaths(sessionPath);

            Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
            Assert.Equal(Path.GetFullPath(sessionPath), result.SessionPath);
            Assert.Equal("preserve_raw_artifacts_no_mutation", result.RawDataPolicy);
            Assert.Equal("fixture-valid-session", result.Summary.SessionId);
            Assert.Equal(2, result.Summary.ArtifactCount);
            Assert.Equal(2, result.PruneInventory.CandidateCount);
            Assert.Equal(["clusters.jsonl", "report.md"], result.Summary.GeneratedArtifacts.Select(artifact => artifact.Path).ToArray());
            Assert.Equal(["clusters.jsonl", "report.md"], result.PruneInventory.Candidates.Select(candidate => candidate.Path).ToArray());
            Assert.Equal(beforeFiles, afterFiles);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Inventory_json_out_writes_inventory_file_and_reports_path()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-generated-artifacts");
        var inventoryPath = Path.Combine(tempDirectory, "reports", "session-inventory.json");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "next_capture_plan.json"), "{\"schema_version\":\"riftscan.next_capture_plan.v1\"}\n");

            var result = new SessionInventoryService().Inventory(sessionPath, inventoryPath);

            var fullInventoryPath = Path.GetFullPath(inventoryPath);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
            Assert.Equal(fullInventoryPath, result.InventoryPath);
            Assert.True(File.Exists(fullInventoryPath));

            using var document = JsonDocument.Parse(File.ReadAllText(fullInventoryPath));
            Assert.Equal("riftscan.session_inventory_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(fullInventoryPath, document.RootElement.GetProperty("inventory_path").GetString());
            Assert.Equal("fixture-valid-session", document.RootElement.GetProperty("summary").GetProperty("session_id").GetString());
            Assert.Equal(1, document.RootElement.GetProperty("prune_inventory").GetProperty("candidate_count").GetInt32());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Cli_session_inventory_json_out_writes_inventory_file()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-generated-artifacts");
        var inventoryPath = Path.Combine(tempDirectory, "reports", "cli-session-inventory.json");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "triage.jsonl"), "{\"schema_version\":\"riftscan.region_triage_entry.v1\"}\n");

            var result = RunCli("session", "inventory", sessionPath, "--json-out", inventoryPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            var fullInventoryPath = Path.GetFullPath(inventoryPath);
            using var document = JsonDocument.Parse(result.Stdout);
            Assert.Equal(fullInventoryPath, document.RootElement.GetProperty("inventory_path").GetString());
            Assert.Equal(1, document.RootElement.GetProperty("summary").GetProperty("artifact_count").GetInt32());
            Assert.True(File.Exists(fullInventoryPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Inventory_missing_session_root_returns_machine_readable_error()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "missing-session");
        var inventoryPath = Path.Combine(tempDirectory, "reports", "missing-session-inventory.json");

        try
        {
            var result = new SessionInventoryService().Inventory(sessionPath, inventoryPath);

            Assert.False(result.Success);
            Assert.Equal(Path.GetFullPath(sessionPath), result.SessionPath);
            Assert.Contains(result.Issues, issue => issue.Code == "session_root_missing");
            Assert.Contains(result.Summary.Issues, issue => issue.Code == "session_root_missing");
            Assert.Contains(result.PruneInventory.Issues, issue => issue.Code == "session_root_missing");
            Assert.True(File.Exists(Path.GetFullPath(inventoryPath)));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Inventory_malformed_manifest_returns_error_without_throwing()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-bad-manifest");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "manifest.json"), "{ definitely-not-json");
            File.WriteAllText(Path.Combine(sessionPath, "report.md"), "# report\n");

            var result = new SessionInventoryService().Inventory(sessionPath);

            Assert.False(result.Success);
            Assert.Null(result.Summary.SessionId);
            Assert.Equal(1, result.Summary.ArtifactCount);
            Assert.Equal(1, result.PruneInventory.CandidateCount);
            Assert.Contains(result.Issues, issue => issue.Code == "json_invalid" && issue.Path == "manifest.json");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Inventory_malformed_regions_returns_error_without_throwing()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-bad-regions");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "regions.json"), "{ definitely-not-json");
            File.WriteAllText(Path.Combine(sessionPath, "clusters.jsonl"), "{\"schema_version\":\"riftscan.structure_cluster.v1\"}\n");

            var result = new SessionInventoryService().Inventory(sessionPath);

            Assert.False(result.Success);
            Assert.Equal("fixture-valid-session", result.Summary.SessionId);
            Assert.Equal(1, result.Summary.ArtifactCount);
            Assert.Equal(1, result.PruneInventory.CandidateCount);
            Assert.Contains(result.Issues, issue => issue.Code == "json_invalid" && issue.Path == "regions.json");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Inventory_malformed_snapshot_index_returns_error_without_throwing()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-bad-snapshot-index");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "snapshots", "index.jsonl"), "{ definitely-not-json\n");
            File.WriteAllText(Path.Combine(sessionPath, "report.json"), "{\"schema_version\":\"riftscan.session_report.v1\"}\n");

            var result = new SessionInventoryService().Inventory(sessionPath);

            Assert.False(result.Success);
            Assert.Equal("fixture-valid-session", result.Summary.SessionId);
            Assert.Equal(1, result.Summary.ArtifactCount);
            Assert.Equal(1, result.PruneInventory.CandidateCount);
            Assert.Contains(result.Issues, issue => issue.Code == "snapshot_index_json_invalid" && issue.Path == "snapshots/index.jsonl");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Cli_session_inventory_malformed_snapshot_index_returns_nonzero_json_result()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "session-with-bad-snapshot-index");

        try
        {
            CopyDirectory(ValidFixturePath, sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, "snapshots", "index.jsonl"), "{ definitely-not-json\n");

            var result = RunCli("session", "inventory", sessionPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            using var document = JsonDocument.Parse(result.Stdout);
            Assert.False(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("fixture-valid-session", document.RootElement.GetProperty("summary").GetProperty("session_id").GetString());
            Assert.Contains(
                document.RootElement.GetProperty("issues").EnumerateArray(),
                issue => issue.GetProperty("code").GetString() == "snapshot_index_json_invalid");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Cli_session_inventory_missing_root_returns_nonzero_json_result()
    {
        var tempDirectory = CreateTempDirectory();
        var sessionPath = Path.Combine(tempDirectory, "missing-session");

        try
        {
            var result = RunCli("session", "inventory", sessionPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.Stderr);
            using var document = JsonDocument.Parse(result.Stdout);
            Assert.False(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Contains(
                document.RootElement.GetProperty("issues").EnumerateArray(),
                issue => issue.GetProperty("code").GetString() == "session_root_missing");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string ValidFixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session");

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "riftscan-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(sourceFile, targetFile);
        }
    }

    private static string[] RelativeFilePaths(string directory) =>
        Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(directory, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(args);
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
