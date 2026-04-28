using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class SessionVerifierTests
{
    [Fact]
    public void Verify_valid_fixture_session_succeeds()
    {
        var result = new SessionVerifier().Verify(ValidFixturePath);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Equal("fixture-valid-session", result.SessionId);
        Assert.Contains("manifest.json", result.ArtifactsVerified);
        Assert.Contains("snapshots/region-0001-snapshot-0001.bin", result.ArtifactsVerified);
    }

    [Fact]
    public void Verify_missing_manifest_fails_with_required_file_issue()
    {
        using var fixtureCopy = CopyFixtureToTemp();
        File.Delete(Path.Combine(fixtureCopy.Path, "manifest.json"));

        var result = new SessionVerifier().Verify(fixtureCopy.Path);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "required_file_missing" && issue.Path == "manifest.json");
    }

    [Fact]
    public void Verify_snapshot_checksum_mismatch_fails()
    {
        using var fixtureCopy = CopyFixtureToTemp();
        var snapshotPath = Path.Combine(fixtureCopy.Path, "snapshots", "region-0001-snapshot-0001.bin");
        File.WriteAllBytes(snapshotPath, [0xFF, 0xEE, 0xDD, 0xCC]);

        var result = new SessionVerifier().Verify(fixtureCopy.Path);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code is "snapshot_size_mismatch" or "snapshot_checksum_mismatch" or "checksum_mismatch");
    }

    [Fact]
    public void Verify_missing_required_manifest_field_fails_schema_validation()
    {
        using var fixtureCopy = CopyFixtureToTemp();
        var manifestPath = Path.Combine(fixtureCopy.Path, "manifest.json");
        var manifestJson = File.ReadAllText(manifestPath).Replace("\"session_id\": \"fixture-valid-session\",", string.Empty, StringComparison.Ordinal);
        File.WriteAllText(manifestPath, manifestJson);

        var result = new SessionVerifier().Verify(fixtureCopy.Path);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "manifest_session_id_missing");
    }


    [Fact]
    public void Cli_verify_session_returns_success_for_fixture_session()
    {
        var originalOut = Console.Out;
        using var output = new StringWriter();

        try
        {
            Console.SetOut(output);
            var exitCode = RiftScan.Cli.Program.Main(["verify", "session", ValidFixturePath]);

            Assert.Equal(0, exitCode);
            Assert.Contains("\"success\": true", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("fixture-valid-session", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string ValidFixturePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "valid-session");

    private static TempDirectory CopyFixtureToTemp()
    {
        var temp = new TempDirectory();
        CopyDirectory(ValidFixturePath, temp.Path);
        return temp;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
