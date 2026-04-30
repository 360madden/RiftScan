using System.Buffers.Binary;
using System.Text.Json;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftSessionWaypointAnchorMatchServiceTests
{
    [Fact]
    public void Match_finds_player_and_waypoint_vec3_pair_from_anchor()
    {
        using var session = CreateWaypointFixtureSession();
        var anchorPath = WriteAnchorScan(session.Path);

        var result = new RiftSessionWaypointAnchorMatchService().Match(new RiftSessionWaypointAnchorMatchOptions
        {
            SessionPath = session.Path,
            AnchorPath = anchorPath,
            Tolerance = 0.01,
            Top = 10
        });

        Assert.True(result.Success);
        Assert.Equal("fixture-waypoint-anchor-session", result.SessionId);
        Assert.Equal(1, result.AnchorCount);
        Assert.Equal(1, result.AnchorsUsed);
        Assert.Equal(1, result.MatchCount);
        Assert.Equal(1, result.CandidateCount);

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal("rift-waypoint-anchor-candidate-000001", candidate.CandidateId);
        Assert.Equal("region-000001", candidate.PlayerSourceRegionId);
        Assert.Equal("0x20", candidate.PlayerSourceOffsetHex);
        Assert.Equal("0x40", candidate.WaypointSourceOffsetHex);
        Assert.Equal("xyz", candidate.PlayerAxisOrder);
        Assert.Equal("xyz", candidate.WaypointAxisOrder);
        Assert.Equal(20, candidate.BestMemoryDeltaX, precision: 6);
        Assert.Equal(0, candidate.BestMemoryDeltaZ, precision: 6);
        Assert.Equal(20, candidate.BestAnchorDeltaX, precision: 6);
        Assert.Equal(0, candidate.BestAnchorDeltaZ, precision: 6);

        var match = Assert.Single(result.Matches);
        Assert.Equal("rift-waypoint-anchor-match-000001", match.MatchId);
        Assert.Equal(candidate.CandidateId, match.CandidateId);
        Assert.Equal("snapshot-000001", match.SnapshotId);
        Assert.Equal("rift-addon-waypoint-anchor-000001", match.AnchorId);
        Assert.Equal(100, match.MemoryPlayerX, precision: 6);
        Assert.Equal(200, match.MemoryPlayerY, precision: 6);
        Assert.Equal(300, match.MemoryPlayerZ, precision: 6);
        Assert.Equal(120, match.MemoryWaypointX, precision: 6);
        Assert.Equal(200, match.MemoryWaypointY, precision: 6);
        Assert.Equal(300, match.MemoryWaypointZ, precision: 6);
        Assert.Equal(0, match.DeltaMaxAbsDistance, precision: 6);
    }

    [Fact]
    public void Match_derives_anchor_from_legacy_observation_scan()
    {
        using var session = CreateWaypointFixtureSession();
        var anchorPath = WriteLegacyObservationScan(session.Path);

        var result = new RiftSessionWaypointAnchorMatchService().Match(new RiftSessionWaypointAnchorMatchOptions
        {
            SessionPath = session.Path,
            AnchorPath = anchorPath,
            Tolerance = 0.01,
            Top = 10
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.AnchorCount);
        Assert.Equal(1, result.AnchorsUsed);
        Assert.Equal(1, result.MatchCount);
        Assert.Contains("waypoint_anchors_derived_from_observations_for_legacy_scan_result", result.Warnings);
        var match = Assert.Single(result.Matches);
        Assert.Equal("rift-addon-waypoint-anchor-000001", match.AnchorId);
        Assert.Equal(20, match.MemoryDeltaX, precision: 6);
        Assert.Equal(0, match.MemoryDeltaZ, precision: 6);
    }

    [Fact]
    public void Cli_match_waypoint_anchors_writes_json_output()
    {
        using var session = CreateWaypointFixtureSession();
        var anchorPath = WriteAnchorScan(session.Path);
        var outputPath = Path.Combine(session.Path, "waypoint-anchor-matches.json");
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
                "match-waypoint-anchors",
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
            Assert.Equal("riftscan.rift_session_waypoint_anchor_match_result.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("match_count").GetInt32());
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

    private static string WriteLegacyObservationScan(string directory)
    {
        var path = Path.Combine(directory, "legacy-addon-api-observation-scan.json");
        var result = new RiftAddonApiObservationScanResult
        {
            Success = true,
            ObservationCount = 3,
            Observations =
            [
                new RiftAddonApiObservation
                {
                    ObservationId = "rift-addon-api-obs-000001",
                    Kind = "current_player",
                    SourceAddon = "ReaderBridgeExport",
                    SourceFileName = "ReaderBridgeExport.lua",
                    SourcePathRedacted = "ReaderBridgeExport.lua",
                    FileLastWriteUtc = DateTimeOffset.Parse("2026-04-30T02:20:53Z"),
                    Realtime = 116070.96875,
                    CoordX = 100,
                    CoordY = 200,
                    CoordZ = 300,
                    ZoneId = "zFixture",
                    LocationName = "Fixture"
                },
                new RiftAddonApiObservation
                {
                    ObservationId = "rift-addon-api-obs-000002",
                    Kind = "waypoint",
                    SourceAddon = "ReaderBridgeExport",
                    SourceFileName = "ReaderBridgeExport.lua",
                    SourcePathRedacted = "ReaderBridgeExport.lua",
                    FileLastWriteUtc = DateTimeOffset.Parse("2026-04-30T02:20:53Z"),
                    Realtime = 116070.96875,
                    WaypointX = 120,
                    WaypointZ = 300
                },
                new RiftAddonApiObservation
                {
                    ObservationId = "rift-addon-api-obs-000003",
                    Kind = "waypoint_status",
                    SourceAddon = "ReaderBridgeExport",
                    SourceFileName = "ReaderBridgeExport.lua",
                    SourcePathRedacted = "ReaderBridgeExport.lua",
                    FileLastWriteUtc = DateTimeOffset.Parse("2026-04-30T02:20:53Z"),
                    Realtime = 116070.96875,
                    WaypointApiAvailable = true,
                    WaypointHasWaypoint = true,
                    WaypointX = 120,
                    WaypointZ = 300
                }
            ]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(result, SessionJson.Options));
        return path;
    }

    private static TempDirectory CreateWaypointFixtureSession()
    {
        var session = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(session.Path, "snapshots"));

        var snapshotPath = Path.Combine(session.Path, "snapshots", "region-000001-sample-000001.bin");
        var bytes = new byte[256];
        WriteVec3(bytes, 0x20, 100, 200, 300);
        WriteVec3(bytes, 0x40, 120, 200, 300);
        File.WriteAllBytes(snapshotPath, bytes);

        var manifest = new SessionManifest
        {
            SchemaVersion = "riftscan.session.v1",
            SessionId = "fixture-waypoint-anchor-session",
            ProjectVersion = "0.1.0",
            CreatedUtc = DateTimeOffset.Parse("2026-04-30T02:00:00Z"),
            MachineName = "fixture-machine",
            OsVersion = "fixture-os",
            ProcessName = "rift_x64",
            ProcessId = 1234,
            ProcessStartTimeUtc = DateTimeOffset.Parse("2026-04-30T01:59:00Z"),
            CaptureMode = "fixture",
            SnapshotCount = 1,
            RegionCount = 1,
            TotalBytesRaw = 256,
            TotalBytesStored = 256,
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
        var indexEntry = new SnapshotIndexEntry
        {
            SnapshotId = "snapshot-000001",
            RegionId = "region-000001",
            Path = "snapshots/region-000001-sample-000001.bin",
            BaseAddressHex = "0x60000000",
            SizeBytes = 256,
            ChecksumSha256Hex = SessionChecksum.ComputeSha256Hex(snapshotPath)
        };

        WriteJson(Path.Combine(session.Path, "manifest.json"), manifest);
        WriteJson(Path.Combine(session.Path, "regions.json"), regions);
        WriteJson(Path.Combine(session.Path, "modules.json"), new ModuleMap());
        File.WriteAllText(
            Path.Combine(session.Path, "snapshots", "index.jsonl"),
            JsonSerializer.Serialize(indexEntry, SessionJson.Options).ReplaceLineEndings(string.Empty) + Environment.NewLine);
        WriteChecksums(session.Path, "manifest.json", "regions.json", "modules.json", "snapshots/index.jsonl", "snapshots/region-000001-sample-000001.bin");

        var verification = new SessionVerifier().Verify(session.Path);
        Assert.True(verification.Success, string.Join(Environment.NewLine, verification.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        return session;
    }

    private static void WriteVec3(byte[] bytes, int offset, float x, float y, float z)
    {
        WriteFloat(bytes, offset, x);
        WriteFloat(bytes, offset + 4, y);
        WriteFloat(bytes, offset + 8, z);
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-waypoint-anchor-match-" + Guid.NewGuid().ToString("N"));
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
