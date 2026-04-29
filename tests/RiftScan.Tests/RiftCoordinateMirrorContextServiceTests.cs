using System.Buffers.Binary;
using System.Text.Json;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftCoordinateMirrorContextServiceTests
{
    [Fact]
    public void Analyze_emits_member_spacing_and_pointer_context_from_stored_snapshots()
    {
        using var session = CreateMirrorFixtureSession();
        var motionPath = WriteMirrorMotionComparison(session.Path);

        var result = new RiftCoordinateMirrorContextService().Analyze(new RiftCoordinateMirrorContextOptions
        {
            MotionComparisonPath = motionPath,
            SessionPath = session.Path,
            WindowBytes = 64,
            MaxPointerHits = 10,
            Top = 10
        });

        Assert.True(result.Success);
        Assert.Equal("riftscan.rift_coordinate_mirror_context_result.v1", result.ResultSchemaVersion);
        Assert.Equal("fixture-mirror-context-session", result.SessionId);
        Assert.Equal(1, result.MotionClusterCount);
        Assert.Equal(1, result.ContextCount);

        var context = Assert.Single(result.Contexts);
        Assert.Equal("motion-cluster-000001", context.MotionClusterId);
        Assert.Equal("0x50000000", context.RepresentativeSourceBaseAddressHex);
        Assert.Equal("0x20", context.RepresentativeSourceOffsetHex);
        Assert.Equal(2, context.CandidateCount);
        Assert.Equal(new long[] { 0, 32 }, context.MemberRelativeOffsetsBytes);
        Assert.Equal(new long[] { 32 }, context.MemberGapBytes);
        Assert.Equal(44, context.MemberSpanBytes);
        Assert.Equal("0x0", context.LocalWindowStartOffsetHex);
        Assert.Equal("0x6C", context.LocalWindowEndOffsetHex);
        Assert.Equal(108, context.LocalWindowSizeBytes);
        Assert.Equal(2, context.SnapshotsInspected);
        Assert.Equal("snapshot-000001", context.FirstSnapshotId);
        Assert.Equal("snapshot-000002", context.LastSnapshotId);
        Assert.Equal(2, context.FirstSnapshotReadableMemberCount);
        Assert.Equal(1, context.FirstSnapshotUniqueMemberValueCount);
        Assert.Equal(1, context.LastSnapshotUniqueMemberValueCount);
        Assert.Equal("owner_container_trace_ready_pointer_context_present", context.CanonicalDiscriminatorStatus);

        var firstValue = Assert.Single(context.FirstSnapshotMemberValues, value => value.SourceOffsetHex == "0x20");
        Assert.True(firstValue.Readable);
        Assert.Equal(100, firstValue.X);
        Assert.Equal(200, firstValue.Y);
        Assert.Equal(300, firstValue.Z);

        Assert.Equal(1, context.PointerLikeValueCount);
        var pointerHit = Assert.Single(context.PointerLikeHits);
        Assert.Equal("0x18", pointerHit.SourceOffsetHex);
        Assert.Equal("0x50000018", pointerHit.SourceAbsoluteAddressHex);
        Assert.Equal("0x50000080", pointerHit.PointerValueHex);
        Assert.Equal("region-000001", pointerHit.TargetRegionId);
        Assert.Equal("0x80", pointerHit.TargetOffsetHex);
        Assert.True(pointerHit.SourceIsRepresentativeRegion);
    }

    [Fact]
    public void Cli_coordinate_mirror_context_writes_json_and_markdown_report()
    {
        using var session = CreateMirrorFixtureSession();
        var motionPath = WriteMirrorMotionComparison(session.Path);
        var jsonPath = Path.Combine(session.Path, "coordinate-mirror-context.json");
        var markdownPath = Path.Combine(session.Path, "coordinate-mirror-context.md");
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
                "coordinate-mirror-context",
                motionPath,
                "--session",
                session.Path,
                "--window-bytes",
                "64",
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
            Assert.Equal("riftscan.rift_coordinate_mirror_context_result.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("context_count").GetInt32());
            using var fileJson = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.Equal(Path.GetFullPath(markdownPath), fileJson.RootElement.GetProperty("markdown_report_path").GetString());
            var markdown = File.ReadAllText(markdownPath);
            Assert.Contains("RiftScan Coordinate Mirror Context", markdown, StringComparison.Ordinal);
            Assert.Contains("owner_container_trace_ready_pointer_context_present", markdown, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string WriteMirrorMotionComparison(string directory)
    {
        var path = Path.Combine(directory, "motion-comparison.json");
        WriteJson(path, new RiftAddonCoordinateMotionComparisonResult
        {
            Success = true,
            MotionClusterCount = 1,
            MotionClusters =
            [
                new RiftAddonCoordinateMotionCluster
                {
                    ClusterId = "motion-cluster-000001",
                    RepresentativeSourceRegionId = "region-000001",
                    RepresentativeSourceBaseAddressHex = "0x50000000",
                    RepresentativeSourceOffsetHex = "0x20",
                    RepresentativeSourceAbsoluteAddressHex = "0x50000020",
                    AxisOrder = "xyz",
                    CandidateCount = 2,
                    SourceOffsets = ["0x20", "0x40"],
                    SourceAbsoluteAddresses = ["0x50000020", "0x50000040"],
                    Classification = "synchronized_coordinate_mirror_cluster",
                    PromotionStatus = "blocked_by_synchronized_mirror_cluster"
                }
            ]
        });
        return path;
    }

    private static TempDirectory CreateMirrorFixtureSession()
    {
        var session = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(session.Path, "snapshots"));

        var firstPath = Path.Combine(session.Path, "snapshots", "region-000001-sample-000001.bin");
        var secondPath = Path.Combine(session.Path, "snapshots", "region-000001-sample-000002.bin");
        var firstBytes = new byte[512];
        var secondBytes = new byte[512];
        WritePointer(firstBytes, 0x18, 0x5000_0080);
        WritePointer(secondBytes, 0x18, 0x5000_0080);
        WriteVec3(firstBytes, 0x20, 100, 200, 300);
        WriteVec3(firstBytes, 0x40, 100, 200, 300);
        WriteVec3(secondBytes, 0x20, 105, 201, 303);
        WriteVec3(secondBytes, 0x40, 105, 201, 303);
        File.WriteAllBytes(firstPath, firstBytes);
        File.WriteAllBytes(secondPath, secondBytes);

        var manifest = new SessionManifest
        {
            SchemaVersion = "riftscan.session.v1",
            SessionId = "fixture-mirror-context-session",
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
            TotalBytesRaw = 1024,
            TotalBytesStored = 1024,
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
                    SizeBytes = 512,
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
                SizeBytes = 512,
                ChecksumSha256Hex = SessionChecksum.ComputeSha256Hex(firstPath)
            },
            new SnapshotIndexEntry
            {
                SnapshotId = "snapshot-000002",
                RegionId = "region-000001",
                Path = "snapshots/region-000001-sample-000002.bin",
                BaseAddressHex = "0x50000000",
                SizeBytes = 512,
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

    private static void WritePointer(byte[] bytes, int offset, ulong value) =>
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(offset, 8), value);

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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-coordinate-mirror-context-tests", Guid.NewGuid().ToString("N"));
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
