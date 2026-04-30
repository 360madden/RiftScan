using System.Buffers.Binary;
using System.Text.Json;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftSessionWaypointScalarMatchServiceTests
{
    [Fact]
    public void Match_finds_separate_waypoint_x_and_z_scalar_pair_from_anchor()
    {
        using var session = CreateWaypointScalarFixtureSession();
        var anchorPath = WriteAnchorScan(session.Path);

        var result = new RiftSessionWaypointScalarMatchService().Match(new RiftSessionWaypointScalarMatchOptions
        {
            SessionPath = session.Path,
            AnchorPath = anchorPath,
            Tolerance = 0.01,
            Top = 10
        });

        Assert.True(result.Success);
        Assert.Equal("fixture-waypoint-scalar-session", result.SessionId);
        Assert.Equal(1, result.AnchorCount);
        Assert.Equal(1, result.AnchorsUsed);
        Assert.Equal(4, result.ScalarHitCount);
        Assert.Equal(2, result.ScalarAxisHitCounts["waypoint_x"]);
        Assert.Equal(2, result.ScalarAxisHitCounts["waypoint_z"]);
        Assert.Equal(4, result.RetainedScalarHitCount);
        Assert.Equal(2, result.RetainedScalarAxisHitCounts["waypoint_x"]);
        Assert.Equal(2, result.RetainedScalarAxisHitCounts["waypoint_z"]);
        Assert.Equal(1, result.PairCandidateCount);

        var candidate = Assert.Single(result.PairCandidates);
        Assert.Equal("rift-waypoint-scalar-pair-candidate-000001", candidate.CandidateId);
        Assert.Equal("rift-addon-waypoint-anchor-000001", candidate.AnchorId);
        Assert.Equal("region-000001", candidate.XSourceRegionId);
        Assert.Equal("0x24", candidate.XSourceOffsetHex);
        Assert.Equal("0xA0", candidate.ZSourceOffsetHex);
        Assert.Equal(2, candidate.SupportCount);
        Assert.Equal(1, candidate.AnchorSupportCount);
        Assert.Equal(120, candidate.BestMemoryWaypointX, precision: 6);
        Assert.Equal(300, candidate.BestMemoryWaypointZ, precision: 6);
        Assert.Equal(120, candidate.AnchorWaypointX, precision: 6);
        Assert.Equal(300, candidate.AnchorWaypointZ, precision: 6);
        Assert.Equal("waypoint_scalar_pair_supported", candidate.ValidationStatus);
        Assert.Equal(["snapshot-000001", "snapshot-000002"], candidate.SupportingSnapshotIds);

        Assert.Contains(result.ScalarHits, hit => hit.Axis == "waypoint_x" && hit.SourceOffsetHex == "0x24");
        Assert.Contains(result.ScalarHits, hit => hit.Axis == "waypoint_z" && hit.SourceOffsetHex == "0xA0");
    }

    [Fact]
    public void Cli_match_waypoint_scalars_writes_json_output()
    {
        using var session = CreateWaypointScalarFixtureSession();
        var anchorPath = WriteAnchorScan(session.Path);
        var outputPath = Path.Combine(session.Path, "waypoint-scalar-matches.json");
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main([
                "rift",
                "match-waypoint-scalars",
                session.Path,
                "--anchors",
                anchorPath,
                "--tolerance",
                "0.01",
                "--out",
                outputPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(outputPath));
            using var stdoutJson = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_session_waypoint_scalar_match_result.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(4, stdoutJson.RootElement.GetProperty("scalar_hit_count").GetInt32());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("pair_candidate_count").GetInt32());
            using var fileJson = JsonDocument.Parse(File.ReadAllText(outputPath));
            Assert.Equal(Path.GetFullPath(outputPath), fileJson.RootElement.GetProperty("output_path").GetString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string WriteAnchorScan(string directory)
    {
        var path = Path.Combine(directory, "addon-api-observation-scan.json");
        var result = new RiftAddonApiObservationScanResult
        {
            Success = true,
            WaypointAnchorCount = 1,
            WaypointAnchors =
            [
                new RiftAddonWaypointAnchor
                {
                    AnchorId = "rift-addon-waypoint-anchor-000001",
                    SourceAddon = "ReaderBridgeExport",
                    SourceFileName = "ReaderBridgeExport.lua",
                    SourcePathRedacted = "ReaderBridgeExport.lua",
                    FileLastWriteUtc = DateTimeOffset.Parse("2026-04-30T02:20:53Z"),
                    Realtime = 116070.96875,
                    PlayerObservationId = "rift-addon-api-obs-000001",
                    WaypointObservationId = "rift-addon-api-obs-000002",
                    WaypointStatusObservationId = "rift-addon-api-obs-000003",
                    PlayerX = 100,
                    PlayerY = 200,
                    PlayerZ = 300,
                    WaypointX = 120,
                    WaypointZ = 300,
                    DeltaX = 20,
                    DeltaZ = 0,
                    HorizontalDistance = 20,
                    ZoneId = "zFixture",
                    LocationName = "Fixture"
                }
            ]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, SessionJson.Options));
        return path;
    }

    private static TempDirectory CreateWaypointScalarFixtureSession()
    {
        var session = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(session.Path, "snapshots"));

        var snapshotPath1 = Path.Combine(session.Path, "snapshots", "region-000001-sample-000001.bin");
        var snapshotPath2 = Path.Combine(session.Path, "snapshots", "region-000001-sample-000002.bin");
        var bytes1 = new byte[256];
        var bytes2 = new byte[256];
        WriteFloat(bytes1, 0x24, 120);
        WriteFloat(bytes1, 0xA0, 300);
        WriteFloat(bytes2, 0x24, 120);
        WriteFloat(bytes2, 0xA0, 300);
        File.WriteAllBytes(snapshotPath1, bytes1);
        File.WriteAllBytes(snapshotPath2, bytes2);

        var manifest = new SessionManifest
        {
            SchemaVersion = "riftscan.session.v1",
            SessionId = "fixture-waypoint-scalar-session",
            ProjectVersion = "0.1.0",
            CreatedUtc = DateTimeOffset.Parse("2026-04-30T02:00:00Z"),
            MachineName = "fixture-machine",
            OsVersion = "fixture-os",
            ProcessName = "rift_x64",
            ProcessId = 1234,
            ProcessStartTimeUtc = DateTimeOffset.Parse("2026-04-30T01:59:00Z"),
            CaptureMode = "fixture",
            SnapshotCount = 2,
            RegionCount = 1,
            TotalBytesRaw = 512,
            TotalBytesStored = 512,
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
                    RegionId = "region-000001",
                    BaseAddressHex = "0x60000000",
                    SizeBytes = 256,
                    Protection = "PAGE_READWRITE",
                    State = "MEM_COMMIT",
                    Type = "MEM_PRIVATE"
                }
            ]
        };
        var indexEntries = new[]
        {
            new SnapshotIndexEntry
            {
                SnapshotId = "snapshot-000001",
                RegionId = "region-000001",
                Path = "snapshots/region-000001-sample-000001.bin",
                BaseAddressHex = "0x60000000",
                SizeBytes = 256,
                ChecksumSha256Hex = SessionChecksum.ComputeSha256Hex(snapshotPath1)
            },
            new SnapshotIndexEntry
            {
                SnapshotId = "snapshot-000002",
                RegionId = "region-000001",
                Path = "snapshots/region-000001-sample-000002.bin",
                BaseAddressHex = "0x60000000",
                SizeBytes = 256,
                ChecksumSha256Hex = SessionChecksum.ComputeSha256Hex(snapshotPath2)
            }
        };

        WriteJson(Path.Combine(session.Path, "manifest.json"), manifest);
        WriteJson(Path.Combine(session.Path, "regions.json"), regions);
        WriteJson(Path.Combine(session.Path, "modules.json"), new ModuleMap());
        File.WriteAllText(
            Path.Combine(session.Path, "snapshots", "index.jsonl"),
            string.Join(
                Environment.NewLine,
                indexEntries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty))) + Environment.NewLine);
        WriteChecksums(
            session.Path,
            "manifest.json",
            "regions.json",
            "modules.json",
            "snapshots/index.jsonl",
            "snapshots/region-000001-sample-000001.bin",
            "snapshots/region-000001-sample-000002.bin");

        var verification = new SessionVerifier().Verify(session.Path);
        Assert.True(verification.Success, string.Join(Environment.NewLine, verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        return session;
    }

    private static void WriteFloat(byte[] bytes, int offset, float value) =>
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, 4), BitConverter.SingleToInt32Bits(value));

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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-waypoint-scalar-match-" + Guid.NewGuid().ToString("N"));
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
