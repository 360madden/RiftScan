using System.Text.Json;
using RiftScan.Analysis.Comparison;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class ScalarTruthCorroborationVerifierTests
{
    [Fact]
    public void Verify_accepts_valid_corroboration_jsonl()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "corroboration.jsonl");
        File.WriteAllText(path, JsonSerializer.Serialize(new ScalarTruthCorroborationEntry
        {
            BaseAddressHex = "0x1000",
            OffsetHex = "0x4",
            DataType = "float32",
            Classification = "actor_yaw_angle_scalar_candidate",
            CorroborationStatus = "corroborated",
            Source = "fixture_addon",
            EvidenceSummary = "fixture evidence"
        }, SessionJson.Options).ReplaceLineEndings(string.Empty));

        var result = new ScalarTruthCorroborationVerifier().Verify(path);

        Assert.True(result.Success);
        Assert.Equal(1, result.EntryCount);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Verify_rejects_invalid_corroboration_status()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "corroboration.jsonl");
        File.WriteAllText(path, """
{"schema_version":"riftscan.scalar_truth_corroboration.v1","base_address_hex":"0x1000","offset_hex":"0x4","data_type":"float32","classification":"","corroboration_status":"maybe","source":"fixture","evidence_summary":"bad"}
""");

        var result = new ScalarTruthCorroborationVerifier().Verify(path);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "corroboration_status_invalid" && issue.LineNumber == 1);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-corroboration-tests", Guid.NewGuid().ToString("N"));
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
