using System.Buffers.Binary;
using System.Text.Json;
using RiftScan.Analysis.Xrefs;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class SessionXrefAnalysisServiceTests
{
    [Fact]
    public void Analyze_finds_pointer_and_pattern_xrefs_from_stored_snapshots_only()
    {
        using var session = CreateXrefFixtureSession();

        var result = new SessionXrefAnalysisService().Analyze(new SessionXrefAnalysisOptions
        {
            SessionPath = session.Path,
            TargetBaseAddress = 0x1000_0000,
            TargetOffsets = [0x10],
            PatternOffsets = [0x10],
            PatternLengthBytes = 12,
            MaxHits = 20
        });

        Assert.True(result.Success);
        Assert.Equal("fixture-xref-session", result.SessionId);
        Assert.Equal("0x10000000", result.TargetBaseAddressHex);
        Assert.Equal(64, result.TargetSizeBytes);
        Assert.Equal(2, result.PointerHitCount);
        Assert.Equal(1, result.ExactTargetPointerCount);
        Assert.Equal(2, result.OutsideTargetRegionPointerCount);
        Assert.Equal(1, result.OutsideExactTargetPointerCount);
        Assert.Contains(result.PointerHits, hit =>
            hit.SourceRegionId == "region-source" &&
            hit.SourceOffsetHex == "0x8" &&
            hit.PointerValueHex == "0x10000010" &&
            hit.TargetOffsetHex == "0x10" &&
            hit.MatchKind == "exact_target_offset_pointer");
        Assert.Contains(result.PointerHits, hit =>
            hit.SourceRegionId == "region-source" &&
            hit.SourceOffsetHex == "0x18" &&
            hit.PointerValueHex == "0x10000020" &&
            hit.TargetOffsetHex == "0x20" &&
            hit.MatchKind == "pointer_into_target_region");
        Assert.Equal(1, result.PatternDefinitionCount);
        Assert.Equal(2, result.PatternHitCount);
        Assert.Equal(1, result.OutsideTargetRegionPatternHitCount);
        Assert.Contains(result.PatternHits, hit =>
            hit.SourceRegionId == "region-source" &&
            hit.SourceOffsetHex == "0x20" &&
            !hit.SourceIsTargetRegion);
    }

    [Fact]
    public void Cli_analyze_xrefs_writes_json_and_markdown_report()
    {
        using var session = CreateXrefFixtureSession();
        var jsonPath = Path.Combine(session.Path, "xref-result.json");
        var markdownPath = Path.Combine(session.Path, "xref-result.md");
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(
            [
                "analyze",
                "xrefs",
                session.Path,
                "--target-base",
                "0x10000000",
                "--target-offsets",
                "0x10",
                "--pattern-offsets",
                "0x10",
                "--out",
                jsonPath,
                "--report-md",
                markdownPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(jsonPath));
            Assert.True(File.Exists(markdownPath));
            using var stdoutJson = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.session_xref_analysis_result.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(2, stdoutJson.RootElement.GetProperty("pointer_hit_count").GetInt32());
            using var resultJson = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.Equal(Path.GetFullPath(markdownPath), resultJson.RootElement.GetProperty("markdown_report_path").GetString());
            var report = File.ReadAllText(markdownPath);
            Assert.Contains("# RiftScan Session Xref Report - fixture-xref-session", report, StringComparison.Ordinal);
            Assert.Contains("exact_target_offset_pointer", report, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static TempDirectory CreateXrefFixtureSession()
    {
        var session = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(session.Path, "snapshots"));

        var targetPath = Path.Combine(session.Path, "snapshots", "target.bin");
        var sourcePath = Path.Combine(session.Path, "snapshots", "source.bin");
        var targetBytes = new byte[64];
        var sourceBytes = new byte[64];
        var patternBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x44, 0x33, 0x22, 0x11, 0xCA, 0xFE, 0xBA, 0xBE };

        patternBytes.CopyTo(targetBytes.AsSpan(0x10));
        BinaryPrimitives.WriteUInt64LittleEndian(sourceBytes.AsSpan(0x08), 0x1000_0010);
        BinaryPrimitives.WriteUInt64LittleEndian(sourceBytes.AsSpan(0x18), 0x1000_0020);
        patternBytes.CopyTo(sourceBytes.AsSpan(0x20));
        File.WriteAllBytes(targetPath, targetBytes);
        File.WriteAllBytes(sourcePath, sourceBytes);

        var targetHash = SessionChecksum.ComputeSha256Hex(targetPath);
        var sourceHash = SessionChecksum.ComputeSha256Hex(sourcePath);
        var manifest = new SessionManifest
        {
            SchemaVersion = "riftscan.session.v1",
            SessionId = "fixture-xref-session",
            ProjectVersion = "0.1.0",
            CreatedUtc = DateTimeOffset.Parse("2026-04-29T17:00:00Z"),
            MachineName = "fixture-machine",
            OsVersion = "fixture-os",
            ProcessName = "rift_x64",
            ProcessId = 1234,
            ProcessStartTimeUtc = DateTimeOffset.Parse("2026-04-29T16:59:00Z"),
            CaptureMode = "fixture",
            SnapshotCount = 2,
            RegionCount = 2,
            TotalBytesRaw = 128,
            TotalBytesStored = 128,
            Compression = "none",
            ChecksumAlgorithm = "SHA256",
            Status = "complete"
        };
        var regions = new RegionMap
        {
            Regions =
            [
                new MemoryRegion
                {
                    RegionId = "region-target",
                    BaseAddressHex = "0x10000000",
                    SizeBytes = 64,
                    Protection = "PAGE_READWRITE",
                    State = "MEM_COMMIT",
                    Type = "MEM_PRIVATE"
                },
                new MemoryRegion
                {
                    RegionId = "region-source",
                    BaseAddressHex = "0x20000000",
                    SizeBytes = 64,
                    Protection = "PAGE_READWRITE",
                    State = "MEM_COMMIT",
                    Type = "MEM_PRIVATE"
                }
            ]
        };
        var modules = new ModuleMap();
        var indexEntries = new[]
        {
            new SnapshotIndexEntry
            {
                SnapshotId = "snapshot-target",
                RegionId = "region-target",
                Path = "snapshots/target.bin",
                BaseAddressHex = "0x10000000",
                SizeBytes = 64,
                ChecksumSha256Hex = targetHash
            },
            new SnapshotIndexEntry
            {
                SnapshotId = "snapshot-source",
                RegionId = "region-source",
                Path = "snapshots/source.bin",
                BaseAddressHex = "0x20000000",
                SizeBytes = 64,
                ChecksumSha256Hex = sourceHash
            }
        };

        WriteJson(Path.Combine(session.Path, "manifest.json"), manifest);
        WriteJson(Path.Combine(session.Path, "regions.json"), regions);
        WriteJson(Path.Combine(session.Path, "modules.json"), modules);
        File.WriteAllLines(
            Path.Combine(session.Path, "snapshots", "index.jsonl"),
            indexEntries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        WriteChecksums(session.Path, "manifest.json", "regions.json", "modules.json", "snapshots/index.jsonl", "snapshots/target.bin", "snapshots/source.bin");

        var verification = new SessionVerifier().Verify(session.Path);
        Assert.True(verification.Success, string.Join(Environment.NewLine, verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        return session;
    }

    private static void WriteChecksums(string sessionPath, params string[] relativePaths)
    {
        var checksums = new ChecksumManifest
        {
            Algorithm = "SHA256",
            Entries = relativePaths
                .Select(relativePath =>
                {
                    var fullPath = Path.Combine(sessionPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    return new ChecksumEntry
                    {
                        Path = relativePath,
                        Sha256Hex = SessionChecksum.ComputeSha256Hex(fullPath),
                        Bytes = new FileInfo(fullPath).Length
                    };
                })
                .ToArray()
        };
        WriteJson(Path.Combine(sessionPath, "checksums.json"), checksums);
    }

    private static void WriteJson<T>(string path, T value) =>
        File.WriteAllText(path, JsonSerializer.Serialize(value, SessionJson.Options));

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-xref-tests", Guid.NewGuid().ToString("N"));
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
