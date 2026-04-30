using System.Text.Json;
using System.Text.Json.Nodes;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftReaderActorCoordinateScanPacketVerifierTests
{
    [Fact]
    public void Verifier_accepts_riftreader_actor_coordinate_scan_packet()
    {
        using var workspace = new TempDirectory();
        var packetPath = WritePacket(workspace.Path);

        var result = new RiftReaderActorCoordinateScanPacketVerifier().Verify(packetPath);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Equal("riftscan.riftreader_actor_coordinate_scan_packet_verification_result.v1", result.ResultSchemaVersion);
        Assert.Equal("riftscan-riftreader-delegate-actor-coordinate-scan-v1", result.PacketSchemaVersion);
        Assert.Equal(41220, result.ProcessId);
        Assert.Equal("rift_x64", result.ProcessName);
        Assert.Equal("0x216F2F26020", result.SourceObjectAddressHex);
        Assert.Equal("0x216F2F26068", result.CoordRegionAddressHex);
        Assert.Equal("0x48", result.SourceCoordRelativeOffsetHex);
        Assert.Equal("0x216BE6A0000", result.BridgeRegionBaseHex);
        Assert.Equal("0x21693FB9E48", result.TraceObjectAddressHex);
        Assert.Equal(3, result.RiftReaderArtifactCount);
    }

    [Fact]
    public void Verifier_rejects_packet_that_overclaims_trace_coord_match()
    {
        using var workspace = new TempDirectory();
        var packetPath = WritePacket(workspace.Path, traceCoordMatches: true);

        var result = new RiftReaderActorCoordinateScanPacketVerifier().Verify(packetPath);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "trace_coord_match_not_rejected");
    }

    [Fact]
    public void Verifier_rejects_packet_missing_bridge_target_from_next_capture()
    {
        using var workspace = new TempDirectory();
        var packetPath = WritePacket(workspace.Path, includeBridgeInNextCapture: false);

        var result = new RiftReaderActorCoordinateScanPacketVerifier().Verify(packetPath);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "next_capture_missing_bridge_region");
    }

    [Fact]
    public void Cli_verify_riftreader_actor_coordinate_scan_prints_machine_readable_result()
    {
        using var workspace = new TempDirectory();
        var packetPath = WritePacket(workspace.Path);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(["verify", "riftreader-actor-coordinate-scan", packetPath]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            using var document = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.riftreader_actor_coordinate_scan_packet_verification_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.True(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("0x216BE6A0000", document.RootElement.GetProperty("bridge_region_base_hex").GetString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string WritePacket(
        string workspace,
        bool traceCoordMatches = false,
        bool includeBridgeInNextCapture = true)
    {
        var riftReaderRepo = Path.Combine(workspace, "RiftReader");
        var riftScanRepo = Path.Combine(workspace, "Riftscan");
        var artifactDirectory = Path.Combine(riftReaderRepo, "scripts", "captures", "packet");
        Directory.CreateDirectory(artifactDirectory);
        Directory.CreateDirectory(riftScanRepo);

        var artifactFiles = new JsonArray();
        foreach (var artifactName in new[] { "read-player-coord-anchor.json", "resolved-proof-coord-anchor.json", "scan-pointer-object-base.json" })
        {
            var artifactPath = Path.Combine(artifactDirectory, artifactName);
            File.WriteAllText(artifactPath, "{}");
            artifactFiles.Add(artifactPath);
        }

        var packetPath = Path.Combine(workspace, "riftreader-packet.json");
        var targetAddresses = includeBridgeInNextCapture
            ? new JsonArray("0x216F2F26020", "0x216BE6A0000")
            : new JsonArray("0x216F2F26020");

        var packet = new JsonObject
        {
            ["schema_version"] = "riftscan-riftreader-delegate-actor-coordinate-scan-v1",
            ["generated_utc"] = "2026-04-30T13:46:39.4353956Z",
            ["purpose"] = "Fast actor-coordinate discovery packet bridging RiftReader custom-reader evidence into RiftScan.",
            ["process"] = new JsonObject
            {
                ["pid"] = 41220,
                ["name"] = "rift_x64"
            },
            ["riftreader"] = new JsonObject
            {
                ["repo"] = riftReaderRepo,
                ["artifact_directory"] = artifactDirectory,
                ["artifact_files"] = artifactFiles
            },
            ["riftscan"] = new JsonObject
            {
                ["repo"] = riftScanRepo,
                ["packet_path"] = packetPath
            },
            ["resolved_live_coord_anchor"] = new JsonObject
            {
                ["status"] = "validated",
                ["canonical_coord_source_kind"] = "coord-trace-source-object",
                ["match_source"] = "readerbridge-live",
                ["object_base_address"] = "0x216F2F26020",
                ["coord_region_address"] = "0x216F2F26068",
                ["source_object_address"] = "0x216F2F26020",
                ["source_coord_relative_offset_hex"] = "0x48",
                ["coord_offsets"] = new JsonObject
                {
                    ["x"] = "0x0",
                    ["y"] = "0x4",
                    ["z"] = "0x8"
                },
                ["memory_sample"] = new JsonObject
                {
                    ["AddressHex"] = "0x216F2F26068",
                    ["CoordX"] = 7259.063,
                    ["CoordY"] = 875.5653,
                    ["CoordZ"] = 3052.8816
                },
                ["expected"] = new JsonObject
                {
                    ["CoordX"] = 7259.0600585938,
                    ["CoordY"] = 875.57000732422,
                    ["CoordZ"] = 3052.8798828125
                },
                ["match"] = new JsonObject
                {
                    ["CoordMatchesWithinTolerance"] = true,
                    ["DeltaX"] = 0.0029296875,
                    ["DeltaY"] = -0.004699707,
                    ["DeltaZ"] = 0.0017089844
                }
            },
            ["rejected_trace_anchor"] = new JsonObject
            {
                ["status"] = "not_actor_coord_source",
                ["trace_object_base_address"] = "0x21693FB9E48",
                ["trace_target_address"] = "0x21693FB9FA0",
                ["reason"] = "Trace object fails coordinate comparison.",
                ["trace_match"] = new JsonObject
                {
                    ["CoordMatchesWithinTolerance"] = traceCoordMatches
                }
            },
            ["owner_pointer_evidence"] = new JsonObject
            {
                ["object_base_pointer_hit_count"] = 3,
                ["coord_region_pointer_hit_count"] = 0,
                ["trace_object_pointer_hit_count"] = 1,
                ["strongest_owner_table_candidate"] = new JsonObject
                {
                    ["region_base"] = "0x216BE6A0000",
                    ["object_pointer_address"] = "0x216BE6A00B0",
                    ["trace_object_pointer_address"] = "0x216BE6A00A8",
                    ["instruction_pointer_address"] = "0x216BE6A00F8",
                    ["instruction_address"] = "0x7FF7879B117E",
                    ["reasons"] = new JsonArray(
                        "Direct pointer to validated source object at 0x216F2F26020.",
                        "Only direct pointer to rejected trace object at 0x21693FB9E48.",
                        "Contains the coord-access instruction address."),
                    ["old_family_addresses_observed"] = new JsonArray("0x975E1ED9F0"),
                    ["confidence"] = "high_for_selector_source_chain_bridge; not_yet_promoted_as_stable_actor_owner_without_movement_capture"
                }
            },
            ["actor_coordinate_layout_evidence"] = new JsonObject
            {
                ["source_object_base"] = "0x216F2F26020",
                ["coord_triplet_offsets_hex"] = new JsonArray("0x48", "0x88", "0xD8"),
                ["primary_triplet_offset_hex"] = "0x48",
                ["neighbor_triplet_note"] = "Needs movement/turn capture to label roles."
            },
            ["next_capture_recommendation"] = new JsonObject
            {
                ["mode"] = "short_controlled_movement_or_user_moved_poll",
                ["use_riftreader_for"] = new JsonArray("preflight proof anchor"),
                ["use_riftscan_for"] = new JsonArray("targeted movement session artifact"),
                ["target_addresses"] = targetAddresses,
                ["avoid"] = new JsonArray("full exact-coordinate process scan unless bounded by region"),
                ["stop_condition"] = "Confirm which duplicated triplets change with true player movement."
            }
        };

        File.WriteAllText(packetPath, packet.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return packetPath;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-riftreader-packet-tests", Guid.NewGuid().ToString("N"));
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
