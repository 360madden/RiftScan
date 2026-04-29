using System.Buffers.Binary;
using System.Text.Json;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftSessionAddonCoordinateMatchServiceTests
{
    [Fact]
    public void Match_finds_stable_snapshot_vec3_from_addon_observations()
    {
        using var session = CreateCoordinateFixtureSession();
        var observationsPath = WriteObservationFixture(session.Path);

        var result = new RiftSessionAddonCoordinateMatchService().Match(new RiftSessionAddonCoordinateMatchOptions
        {
            SessionPath = session.Path,
            ObservationPath = observationsPath,
            RegionBaseAddresses = [0x5000_0000],
            Tolerance = 0.1,
            Top = 10
        });

        Assert.True(result.Success);
        Assert.Equal("fixture-addon-coordinate-session", result.SessionId);
        Assert.Equal("riftscan.rift_session_addon_coordinate_match_result.v1", result.ResultSchemaVersion);
        Assert.Equal(2, result.ObservationCount);
        Assert.Equal(2, result.ObservationsUsed);
        Assert.Equal(2, result.SnapshotCount);
        Assert.Equal(1, result.RegionsScanned);
        Assert.Equal(2, result.MatchCount);
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal("0x20", candidate.SourceOffsetHex);
        Assert.Equal("0x50000020", candidate.SourceAbsoluteAddressHex);
        Assert.Equal("xyz", candidate.AxisOrder);
        Assert.Equal(2, candidate.SupportCount);
        Assert.Equal(2, candidate.ObservationSupportCount);
        Assert.Equal(0, candidate.BestMaxAbsDistance);
        Assert.Equal("candidate_unverified", candidate.ValidationStatus);
        Assert.Contains("ReaderBridgeExport:coord_table_xyz", candidate.AddonSources);
        Assert.Contains("zFixture", candidate.ZoneIds);
    }

    [Fact]
    public void Cli_match_addon_coords_writes_json_and_markdown_report()
    {
        using var session = CreateCoordinateFixtureSession();
        var observationsPath = WriteObservationFixture(session.Path);
        var jsonPath = Path.Combine(session.Path, "addon-coordinate-matches.json");
        var markdownPath = Path.Combine(session.Path, "addon-coordinate-matches.md");
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
                "match-addon-coords",
                session.Path,
                "--observations",
                observationsPath,
                "--region-base",
                "0x50000000",
                "--tolerance",
                "0.1",
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
            Assert.Equal("riftscan.rift_session_addon_coordinate_match_result.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("candidate_count").GetInt32());
            using var fileJson = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.Equal(Path.GetFullPath(markdownPath), fileJson.RootElement.GetProperty("markdown_report_path").GetString());
            Assert.Contains("RiftScan Addon Coordinate Match Report", File.ReadAllText(markdownPath), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Motion_comparison_classifies_common_candidates_that_moved()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var prePath = Path.Combine(temp.Path, "pre-match.json");
        var postPath = Path.Combine(temp.Path, "post-match.json");
        WriteJson(prePath, new RiftSessionAddonCoordinateMatchResult
        {
            Success = true,
            SessionId = "pre-session",
            ObservationPath = "addon-before.jsonl",
            LatestObservationUtc = DateTimeOffset.Parse("2026-04-29T20:00:00Z"),
            CandidateCount = 1,
            Candidates =
            [
                new RiftSessionAddonCoordinateCandidate
                {
                    CandidateId = "pre-candidate",
                    SourceRegionId = "region-000001",
                    SourceBaseAddressHex = "0x50000000",
                    SourceOffsetHex = "0x20",
                    SourceAbsoluteAddressHex = "0x50000020",
                    AxisOrder = "xyz",
                    SupportCount = 6,
                    BestMemoryX = 100,
                    BestMemoryY = 200,
                    BestMemoryZ = 300,
                    BestMaxAbsDistance = 0.1
                }
            ]
        });
        WriteJson(postPath, new RiftSessionAddonCoordinateMatchResult
        {
            Success = true,
            SessionId = "post-session",
            ObservationPath = "addon-after.jsonl",
            LatestObservationUtc = DateTimeOffset.Parse("2026-04-29T20:01:00Z"),
            CandidateCount = 1,
            Candidates =
            [
                new RiftSessionAddonCoordinateCandidate
                {
                    CandidateId = "post-candidate",
                    SourceRegionId = "region-000001",
                    SourceBaseAddressHex = "0x50000000",
                    SourceOffsetHex = "0x20",
                    SourceAbsoluteAddressHex = "0x50000020",
                    AxisOrder = "xyz",
                    SupportCount = 6,
                    BestMemoryX = 102,
                    BestMemoryY = 200,
                    BestMemoryZ = 301,
                    BestMaxAbsDistance = 2
                }
            ]
        });

        var result = new RiftAddonCoordinateMotionComparisonService().Compare(new RiftAddonCoordinateMotionComparisonOptions
        {
            PreMatchPath = prePath,
            PostMatchPath = postPath,
            MinDeltaDistance = 1,
            Top = 10
        });

        Assert.True(result.Success);
        Assert.Equal("pre-session", result.PreSessionId);
        Assert.Equal("post-session", result.PostSessionId);
        Assert.Equal(1, result.CommonCandidateCount);
        Assert.Equal(1, result.MovedCandidateCount);
        Assert.Equal(1, result.MotionClusterCount);
        Assert.Equal(0, result.SynchronizedMirrorClusterCount);
        Assert.Equal("requires_cross_session_validation_not_final_truth", result.CanonicalPromotionStatus);
        var delta = Assert.Single(result.CandidateDeltas);
        Assert.Equal("0x20", delta.SourceOffsetHex);
        Assert.Equal("moved_with_player_candidate", delta.Classification);
        Assert.Equal(2, delta.DeltaX);
        Assert.Equal(0, delta.DeltaY);
        Assert.Equal(1, delta.DeltaZ);
        Assert.Equal(Math.Sqrt(5), delta.DeltaDistance, precision: 6);
        var cluster = Assert.Single(result.MotionClusters);
        Assert.Equal("single_moved_coordinate_candidate", cluster.Classification);
        Assert.Equal("requires_fresh_addon_observation_and_cross_session_validation", cluster.PromotionStatus);
    }

    [Fact]
    public void Motion_comparison_groups_synchronized_mirror_candidates()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var prePath = Path.Combine(temp.Path, "pre-match.json");
        var postPath = Path.Combine(temp.Path, "post-match.json");
        WriteJson(prePath, new RiftSessionAddonCoordinateMatchResult
        {
            Success = true,
            SessionId = "pre-session",
            ObservationPath = "addon-before.jsonl",
            LatestObservationUtc = DateTimeOffset.Parse("2026-04-29T20:00:00Z"),
            CandidateCount = 2,
            Candidates =
            [
                BuildCoordinateCandidate("0x20", 100, 200, 300),
                BuildCoordinateCandidate("0x40", 100, 200, 300)
            ]
        });
        WriteJson(postPath, new RiftSessionAddonCoordinateMatchResult
        {
            Success = true,
            SessionId = "post-session",
            ObservationPath = "addon-after.jsonl",
            LatestObservationUtc = DateTimeOffset.Parse("2026-04-29T20:01:00Z"),
            CandidateCount = 2,
            Candidates =
            [
                BuildCoordinateCandidate("0x20", 102, 200, 301),
                BuildCoordinateCandidate("0x40", 102, 200, 301)
            ]
        });

        var result = new RiftAddonCoordinateMotionComparisonService().Compare(new RiftAddonCoordinateMotionComparisonOptions
        {
            PreMatchPath = prePath,
            PostMatchPath = postPath,
            MinDeltaDistance = 1,
            Top = 10
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.MovedCandidateCount);
        Assert.Equal(1, result.MotionClusterCount);
        Assert.Equal(1, result.SynchronizedMirrorClusterCount);
        Assert.Equal("blocked_by_synchronized_mirror_clusters", result.CanonicalPromotionStatus);
        Assert.Contains("synchronized_coordinate_mirror_clusters_detected", result.Warnings);
        var cluster = Assert.Single(result.MotionClusters);
        Assert.Equal("synchronized_coordinate_mirror_cluster", cluster.Classification);
        Assert.Equal("blocked_by_synchronized_mirror_cluster", cluster.PromotionStatus);
        Assert.Equal(2, cluster.CandidateCount);
        Assert.Equal(new[] { "0x20", "0x40" }, cluster.SourceOffsets);
    }

    [Fact]
    public void Cli_compare_addon_coordinate_motion_writes_json_and_markdown_report()
    {
        using var temp = new TempDirectory();
        Directory.CreateDirectory(temp.Path);
        var prePath = Path.Combine(temp.Path, "pre-match.json");
        var postPath = Path.Combine(temp.Path, "post-match.json");
        var jsonPath = Path.Combine(temp.Path, "motion.json");
        var markdownPath = Path.Combine(temp.Path, "motion.md");
        WriteJson(prePath, new RiftSessionAddonCoordinateMatchResult
        {
            Success = true,
            SessionId = "pre-session",
            ObservationPath = "addon-before.jsonl",
            CandidateCount = 1,
            Candidates =
            [
                new RiftSessionAddonCoordinateCandidate
                {
                    SourceRegionId = "region-000001",
                    SourceBaseAddressHex = "0x50000000",
                    SourceOffsetHex = "0x20",
                    SourceAbsoluteAddressHex = "0x50000020",
                    AxisOrder = "xyz",
                    SupportCount = 6,
                    BestMemoryX = 100,
                    BestMemoryY = 200,
                    BestMemoryZ = 300
                }
            ]
        });
        WriteJson(postPath, new RiftSessionAddonCoordinateMatchResult
        {
            Success = true,
            SessionId = "post-session",
            ObservationPath = "addon-after.jsonl",
            CandidateCount = 1,
            Candidates =
            [
                new RiftSessionAddonCoordinateCandidate
                {
                    SourceRegionId = "region-000001",
                    SourceBaseAddressHex = "0x50000000",
                    SourceOffsetHex = "0x20",
                    SourceAbsoluteAddressHex = "0x50000020",
                    AxisOrder = "xyz",
                    SupportCount = 6,
                    BestMemoryX = 102,
                    BestMemoryY = 200,
                    BestMemoryZ = 301
                }
            ]
        });
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
                "compare-addon-coordinate-motion",
                prePath,
                postPath,
                "--min-delta-distance",
                "1",
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
            Assert.Equal("riftscan.rift_addon_coordinate_motion_comparison_result.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("moved_candidate_count").GetInt32());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("motion_cluster_count").GetInt32());
            using var fileJson = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.Equal(Path.GetFullPath(markdownPath), fileJson.RootElement.GetProperty("markdown_report_path").GetString());
            Assert.Contains("RiftScan Addon Coordinate Motion Comparison", File.ReadAllText(markdownPath), StringComparison.Ordinal);
            Assert.Contains("Motion clusters", File.ReadAllText(markdownPath), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string WriteObservationFixture(string directory)
    {
        var observationsPath = Path.Combine(directory, "addon-coordinate-observations.jsonl");
        File.WriteAllLines(observationsPath,
        [
            JsonSerializer.Serialize(new RiftAddonCoordinateObservation
            {
                ObservationId = "rift-addon-coord-000001",
                SourceFileName = "ReaderBridgeExport.lua",
                SourcePathRedacted = "ReaderBridgeExport.lua",
                AddonName = "ReaderBridgeExport",
                SourcePattern = "coord_table_xyz",
                FileLastWriteUtc = DateTimeOffset.Parse("2026-04-29T20:00:00Z"),
                CoordX = 100.25,
                CoordY = 200.5,
                CoordZ = 300.75,
                ZoneId = "zFixture"
            }, SessionJson.Options).ReplaceLineEndings(string.Empty),
            JsonSerializer.Serialize(new RiftAddonCoordinateObservation
            {
                ObservationId = "rift-addon-coord-000002",
                SourceFileName = "Leader.lua",
                SourcePathRedacted = "Leader.lua",
                AddonName = "Leader",
                SourcePattern = "coordX_coordY_coordZ",
                FileLastWriteUtc = DateTimeOffset.Parse("2026-04-29T20:01:00Z"),
                CoordX = 101.25,
                CoordY = 200.5,
                CoordZ = 301.75,
                ZoneId = "zFixture"
            }, SessionJson.Options).ReplaceLineEndings(string.Empty)
        ]);
        return observationsPath;
    }

    private static RiftSessionAddonCoordinateCandidate BuildCoordinateCandidate(
        string offsetHex,
        double x,
        double y,
        double z) =>
        new()
        {
            SourceRegionId = "region-000001",
            SourceBaseAddressHex = "0x50000000",
            SourceOffsetHex = offsetHex,
            SourceAbsoluteAddressHex = $"0x{(0x50000000UL + Convert.ToUInt64(offsetHex[2..], 16)):X}",
            AxisOrder = "xyz",
            SupportCount = 6,
            BestMemoryX = x,
            BestMemoryY = y,
            BestMemoryZ = z,
            BestMaxAbsDistance = 0.1
        };

    private static TempDirectory CreateCoordinateFixtureSession()
    {
        var session = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(session.Path, "snapshots"));

        var firstPath = Path.Combine(session.Path, "snapshots", "region-000001-sample-000001.bin");
        var secondPath = Path.Combine(session.Path, "snapshots", "region-000001-sample-000002.bin");
        var firstBytes = new byte[64];
        var secondBytes = new byte[64];
        WriteVec3(firstBytes, 0x20, 100.25f, 200.5f, 300.75f);
        WriteVec3(secondBytes, 0x20, 101.25f, 200.5f, 301.75f);
        WriteVec3(firstBytes, 0x30, -999f, -999f, -999f);
        WriteVec3(secondBytes, 0x30, -999f, -999f, -999f);
        File.WriteAllBytes(firstPath, firstBytes);
        File.WriteAllBytes(secondPath, secondBytes);

        var manifest = new SessionManifest
        {
            SchemaVersion = "riftscan.session.v1",
            SessionId = "fixture-addon-coordinate-session",
            ProjectVersion = "0.1.0",
            CreatedUtc = DateTimeOffset.Parse("2026-04-29T20:00:00Z"),
            MachineName = "fixture-machine",
            OsVersion = "fixture-os",
            ProcessName = "rift_x64",
            ProcessId = 1234,
            ProcessStartTimeUtc = DateTimeOffset.Parse("2026-04-29T19:59:00Z"),
            CaptureMode = "fixture",
            SnapshotCount = 2,
            RegionCount = 1,
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
                    RegionId = "region-000001",
                    BaseAddressHex = "0x50000000",
                    SizeBytes = 64,
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
                BaseAddressHex = "0x50000000",
                SizeBytes = 64,
                ChecksumSha256Hex = SessionChecksum.ComputeSha256Hex(firstPath)
            },
            new SnapshotIndexEntry
            {
                SnapshotId = "snapshot-000002",
                RegionId = "region-000001",
                Path = "snapshots/region-000001-sample-000002.bin",
                BaseAddressHex = "0x50000000",
                SizeBytes = 64,
                ChecksumSha256Hex = SessionChecksum.ComputeSha256Hex(secondPath)
            }
        };

        WriteJson(Path.Combine(session.Path, "manifest.json"), manifest);
        WriteJson(Path.Combine(session.Path, "regions.json"), regions);
        WriteJson(Path.Combine(session.Path, "modules.json"), new ModuleMap());
        File.WriteAllLines(
            Path.Combine(session.Path, "snapshots", "index.jsonl"),
            indexEntries.Select(entry => JsonSerializer.Serialize(entry, SessionJson.Options).ReplaceLineEndings(string.Empty)));
        WriteChecksums(session.Path, "manifest.json", "regions.json", "modules.json", "snapshots/index.jsonl", "snapshots/region-000001-sample-000001.bin", "snapshots/region-000001-sample-000002.bin");

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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-addon-coordinate-match-tests", Guid.NewGuid().ToString("N"));
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
