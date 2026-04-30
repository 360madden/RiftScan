using System.Text.Json;
using System.Text.Json.Nodes;
using RiftScan.Core.Sessions;
using RiftScan.Rift.Addons;

namespace RiftScan.Tests;

public sealed class RiftActorCoordinateOwnerDiscoveryPacketVerifierTests
{
    [Fact]
    public void Verifier_accepts_cross_session_owner_discovery_packet()
    {
        using var workspace = CreatePacketWorkspace();
        var packetPath = WritePacket(workspace.Path);

        var result = new RiftActorCoordinateOwnerDiscoveryPacketVerifier().Verify(packetPath);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Equal("riftscan.rift_actor_coordinate_owner_discovery_packet_verification_result.v1", result.ResultSchemaVersion);
        Assert.Equal("riftscan.actor_coordinate_owner_discovery_packet.v2", result.PacketSchemaVersion);
        Assert.Equal("passive-session", result.PassiveSessionId);
        Assert.Equal("move-session", result.MoveSessionId);
        Assert.Equal(69, result.Context01StableEdgeCount);
        Assert.Equal(87, result.CrossSessionMin160StableEdgeCount);
        Assert.Equal(15, result.CrossSessionMin320StableEdgeCount);
    }

    [Fact]
    public void Verifier_rejects_canonical_promotion_claim()
    {
        using var workspace = CreatePacketWorkspace();
        var packetPath = WritePacket(workspace.Path, status: "canonical_actor_coordinate_owner_promoted");

        var result = new RiftActorCoordinateOwnerDiscoveryPacketVerifier().Verify(packetPath);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported_truth_promotion_claim");
    }

    [Fact]
    public void Cli_verify_actor_coordinate_owner_discovery_prints_machine_readable_result()
    {
        using var workspace = CreatePacketWorkspace();
        var packetPath = WritePacket(workspace.Path);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(["verify", "actor-coordinate-owner-discovery", packetPath]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            using var document = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_actor_coordinate_owner_discovery_packet_verification_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.True(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(15, document.RootElement.GetProperty("cross_session_min320_stable_edge_count").GetInt32());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Followup_plan_builds_capture_plan_from_verified_packet_offsets()
    {
        using var workspace = CreatePacketWorkspace();
        var packetPath = WritePacket(workspace.Path);
        var planPath = Path.Combine(workspace.Path, "followup-plan.json");

        var result = new RiftActorCoordinateOwnerFollowupPlanService().Plan(new RiftActorCoordinateOwnerFollowupPlanOptions
        {
            PacketPath = packetPath,
            TopOffsets = 2,
            Samples = 4,
            IntervalMilliseconds = 100,
            MaxBytesPerRegion = 4096,
            WindowsPerRegion = 2,
            OutputPath = planPath
        });

        Assert.True(result.Success);
        Assert.Equal("riftscan.rift_actor_coordinate_owner_followup_plan.v1", result.ResultSchemaVersion);
        Assert.Equal(2, result.SelectedOffsetCount);
        Assert.Equal(["0x975E1DBA50", "0x975E1DC7EC"], result.TargetAddresses);
        Assert.Equal(32768, result.MaxTotalBytes);
        Assert.Equal(Path.GetFullPath(planPath), result.CapturePlanPath);
        Assert.True(File.Exists(planPath));
        using var document = JsonDocument.Parse(File.ReadAllText(planPath));
        Assert.Equal("comparison_next_capture_plan.v1", document.RootElement.GetProperty("schema_version").GetString());
        Assert.Equal("capture_actor_coordinate_owner_discriminator_followup", document.RootElement.GetProperty("recommended_mode").GetString());
        Assert.Equal("0x975E1DBA50", document.RootElement.GetProperty("target_region_priorities")[0].GetProperty("base_address_hex").GetString());
        Assert.Equal("0x3A50", document.RootElement.GetProperty("target_region_priorities")[0].GetProperty("offset_hex").GetString());
    }

    [Fact]
    public void Cli_plan_actor_coordinate_owner_followup_writes_capture_plan()
    {
        using var workspace = CreatePacketWorkspace();
        var packetPath = WritePacket(workspace.Path);
        var planPath = Path.Combine(workspace.Path, "followup-plan.json");
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
                "plan-actor-coordinate-owner-followup",
                packetPath,
                "--top-offsets",
                "1",
                "--out",
                planPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(planPath));
            using var stdoutJson = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_actor_coordinate_owner_followup_plan.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(1, stdoutJson.RootElement.GetProperty("selected_offset_count").GetInt32());
            Assert.Equal(Path.GetFullPath(planPath), stdoutJson.RootElement.GetProperty("capture_plan_path").GetString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Followup_findings_verifier_accepts_machine_readable_artifact()
    {
        using var workspace = CreateFollowupFindingsWorkspace();
        var findingsPath = WriteFollowupFindings(workspace.Path);

        var result = new RiftActorCoordinateOwnerFollowupFindingsVerifier().Verify(findingsPath);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Equal("riftscan.rift_actor_coordinate_owner_followup_findings_verification_result.v1", result.ResultSchemaVersion);
        Assert.Equal("riftscan.actor_coordinate_owner_followup_findings.v1", result.FindingsSchemaVersion);
        Assert.Equal("followup-session", result.SessionId);
        Assert.Equal("candidate_evidence_strengthened_not_promoted", result.Status);
        Assert.Equal(15072, result.PointerHitCount);
        Assert.Equal(38, result.ExactTargetPointerCount);
        Assert.Equal(6, result.OutsideExactTargetPointerCount);
        Assert.Equal(1, result.StableExactTargetEdgeCount);
        Assert.Equal(6, result.SourceArtifactCount);
    }

    [Fact]
    public void Followup_findings_verifier_rejects_promotion_claim()
    {
        using var workspace = CreateFollowupFindingsWorkspace();
        var findingsPath = WriteFollowupFindings(workspace.Path, status: "canonical_actor_coordinate_owner_promoted");

        var result = new RiftActorCoordinateOwnerFollowupFindingsVerifier().Verify(findingsPath);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported_truth_promotion_claim");
    }

    [Fact]
    public void Cli_verify_actor_coordinate_owner_followup_findings_prints_machine_readable_result()
    {
        using var workspace = CreateFollowupFindingsWorkspace();
        var findingsPath = WriteFollowupFindings(workspace.Path);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(["verify", "actor-coordinate-owner-followup-findings", findingsPath]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            using var document = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_actor_coordinate_owner_followup_findings_verification_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.True(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(1, document.RootElement.GetProperty("stable_exact_target_edge_count").GetInt32());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Combined_passive_findings_verifier_accepts_machine_readable_artifact()
    {
        using var workspace = CreateCombinedPassiveFindingsWorkspace();
        var findingsPath = WriteCombinedPassiveFindings(workspace.Path);

        var result = new RiftActorCoordinateOwnerCombinedPassiveFindingsVerifier().Verify(findingsPath);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Equal("riftscan.rift_actor_coordinate_owner_combined_passive_findings_verification_result.v1", result.ResultSchemaVersion);
        Assert.Equal("riftscan.actor_coordinate_owner_combined_passive_findings.v1", result.FindingsSchemaVersion);
        Assert.Equal("combined-passive-session", result.SessionId);
        Assert.Equal("same_session_external_base_edge_confirmed_not_promoted", result.Status);
        Assert.Equal("0x975E1D8000", result.TargetRegionBaseHex);
        Assert.Equal("0x975E236000", result.OwnerCandidateRegionBaseHex);
        Assert.Equal(1, result.ExternalExactEdgeCount);
        Assert.Equal(8, result.ExternalExactEdgeSupportMax);
        Assert.Equal(11834, result.TargetRegionPointerHitCount);
        Assert.Equal(40, result.TargetRegionExactTargetPointerCount);
        Assert.Equal(8, result.TargetRegionOutsideExactTargetPointerCount);
        Assert.Equal(100, result.TargetRegionStableEdgeCount);
        Assert.Equal(56, result.OwnerRegionPointerHitCount);
        Assert.Equal(8, result.OwnerRegionExactTargetPointerCount);
        Assert.Equal(0, result.OwnerRegionOutsideTargetRegionPointerCount);
        Assert.Equal(0, result.OwnerRegionOutsideExactTargetPointerCount);
        Assert.Equal(7, result.OwnerRegionStableEdgeCount);
        Assert.Equal(0, result.ReciprocalPairCount);
        Assert.Equal(8, result.SourceArtifactCount);
    }

    [Fact]
    public void Combined_passive_findings_verifier_rejects_promotion_claim()
    {
        using var workspace = CreateCombinedPassiveFindingsWorkspace();
        var findingsPath = WriteCombinedPassiveFindings(workspace.Path, status: "canonical_actor_coordinate_owner_promoted");

        var result = new RiftActorCoordinateOwnerCombinedPassiveFindingsVerifier().Verify(findingsPath);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported_truth_promotion_claim");
    }

    [Fact]
    public void Cli_verify_actor_coordinate_owner_combined_passive_findings_prints_machine_readable_result()
    {
        using var workspace = CreateCombinedPassiveFindingsWorkspace();
        var findingsPath = WriteCombinedPassiveFindings(workspace.Path);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(["verify", "actor-coordinate-owner-combined-passive-findings", findingsPath]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            using var document = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_actor_coordinate_owner_combined_passive_findings_verification_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.True(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(1, document.RootElement.GetProperty("external_exact_edge_count").GetInt32());
            Assert.Equal(8, document.RootElement.GetProperty("external_exact_edge_support_max").GetInt32());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Owner_path_hypotheses_verifier_accepts_machine_readable_artifact()
    {
        using var workspace = CreateOwnerPathHypothesesWorkspace();
        var hypothesesPath = WriteOwnerPathHypotheses(workspace.Path);

        var result = new RiftActorCoordinateOwnerPathHypothesesVerifier().Verify(hypothesesPath);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.Equal("riftscan.rift_actor_coordinate_owner_path_hypotheses_verification_result.v1", result.ResultSchemaVersion);
        Assert.Equal("riftscan.actor_coordinate_owner_passive_owner_path_hypotheses.v1", result.HypothesesSchemaVersion);
        Assert.Equal("owner-path-session", result.SessionId);
        Assert.Equal("external_base_edge_confirmed_no_discriminating_offset_edge_in_combined_passive_not_promoted", result.Status);
        Assert.Equal("0x975E1D8000", result.TargetRegionBaseHex);
        Assert.Equal("0x975E236000", result.OwnerCandidateRegionBaseHex);
        Assert.Equal(40, result.ExactPointerHitCount);
        Assert.Equal(5, result.UniqueExactEdgeCount);
        Assert.Equal(3, result.MissingDiscriminatingExactOffsetCount);
        Assert.Equal(1, result.ExternalBaseEdgeCount);
        Assert.Equal(8, result.ExternalBaseEdgeSupportMax);
        Assert.Equal(4, result.InternalExactEdgeCount);
        Assert.True(result.TargetOutputIsCapped);
        Assert.Equal(0, result.OwnerReciprocalPairCount);
        Assert.Equal(7, result.SourceArtifactCount);
    }

    [Fact]
    public void Owner_path_hypotheses_verifier_rejects_promotion_claim()
    {
        using var workspace = CreateOwnerPathHypothesesWorkspace();
        var hypothesesPath = WriteOwnerPathHypotheses(workspace.Path, status: "canonical_actor_coordinate_owner_promoted");

        var result = new RiftActorCoordinateOwnerPathHypothesesVerifier().Verify(hypothesesPath);

        Assert.False(result.Success);
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported_truth_promotion_claim");
    }

    [Fact]
    public void Cli_verify_actor_coordinate_owner_path_hypotheses_prints_machine_readable_result()
    {
        using var workspace = CreateOwnerPathHypothesesWorkspace();
        var hypothesesPath = WriteOwnerPathHypotheses(workspace.Path);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = RiftScan.Cli.Program.Main(["verify", "actor-coordinate-owner-path-hypotheses", hypothesesPath]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            using var document = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_actor_coordinate_owner_path_hypotheses_verification_result.v1", document.RootElement.GetProperty("result_schema_version").GetString());
            Assert.True(document.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(3, document.RootElement.GetProperty("missing_discriminating_exact_offset_count").GetInt32());
            Assert.True(document.RootElement.GetProperty("target_output_is_capped").GetBoolean());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void Move_capture_plan_builds_capture_plan_from_verified_owner_path_hypotheses()
    {
        using var workspace = CreateOwnerPathHypothesesWorkspace();
        var hypothesesPath = WriteOwnerPathHypotheses(workspace.Path);
        var planPath = Path.Combine(workspace.Path, "move-capture-plan.json");

        var result = new RiftActorCoordinateOwnerMoveCapturePlanService().Plan(new RiftActorCoordinateOwnerMoveCapturePlanOptions
        {
            HypothesesPath = hypothesesPath,
            Samples = 4,
            IntervalMilliseconds = 100,
            MaxBytesPerRegion = 4096,
            InterventionWaitMilliseconds = 30000,
            InterventionPollMilliseconds = 1000,
            OutputPath = planPath
        });

        Assert.True(result.Success);
        Assert.Equal("riftscan.rift_actor_coordinate_owner_move_capture_plan.v1", result.ResultSchemaVersion);
        Assert.Equal("owner-path-session", result.SessionId);
        Assert.Equal(2, result.TargetRegionCount);
        Assert.Equal(["0x975E1D8000", "0x975E236000"], result.TargetRegionBases);
        Assert.Equal(32768, result.MaxTotalBytes);
        Assert.Equal(30000, result.InterventionWaitMilliseconds);
        Assert.Equal(Path.GetFullPath(planPath), result.CapturePlanPath);
        Assert.True(File.Exists(planPath));
        using var document = JsonDocument.Parse(File.ReadAllText(planPath));
        Assert.Equal("comparison_next_capture_plan.v1", document.RootElement.GetProperty("schema_version").GetString());
        Assert.Equal("controlled_move_forward_combined_target_owner_capture", document.RootElement.GetProperty("recommended_mode").GetString());
        Assert.Equal("0x975E1D8000", document.RootElement.GetProperty("target_region_priorities")[0].GetProperty("base_address_hex").GetString());
        Assert.Equal("0x975E236000", document.RootElement.GetProperty("target_region_priorities")[1].GetProperty("base_address_hex").GetString());
        Assert.Contains("--stimulus-note", result.RecommendedCaptureArgs);
    }

    [Fact]
    public void Cli_plan_actor_coordinate_owner_move_capture_writes_capture_plan()
    {
        using var workspace = CreateOwnerPathHypothesesWorkspace();
        var hypothesesPath = WriteOwnerPathHypotheses(workspace.Path);
        var planPath = Path.Combine(workspace.Path, "move-capture-plan.json");
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
                "plan-actor-coordinate-owner-move-capture",
                hypothesesPath,
                "--samples",
                "4",
                "--max-bytes-per-region",
                "4096",
                "--out",
                planPath
            ]);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.True(File.Exists(planPath));
            using var stdoutJson = JsonDocument.Parse(output.ToString());
            Assert.Equal("riftscan.rift_actor_coordinate_owner_move_capture_plan.v1", stdoutJson.RootElement.GetProperty("result_schema_version").GetString());
            Assert.Equal(2, stdoutJson.RootElement.GetProperty("target_region_count").GetInt32());
            Assert.Equal(Path.GetFullPath(planPath), stdoutJson.RootElement.GetProperty("capture_plan_path").GetString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static TempDirectory CreatePacketWorkspace()
    {
        var workspace = new TempDirectory();
        foreach (var name in RequiredArtifactNames)
        {
            File.WriteAllText(Path.Combine(workspace.Path, name), "{}");
        }

        return workspace;
    }

    private static TempDirectory CreateFollowupFindingsWorkspace()
    {
        var workspace = new TempDirectory();
        foreach (var name in RequiredFollowupArtifactNames)
        {
            File.WriteAllText(Path.Combine(workspace.Path, name), JsonSerializer.Serialize(new { success = true }, SessionJson.Options));
        }

        Directory.CreateDirectory(Path.Combine(workspace.Path, "followup-session"));
        return workspace;
    }

    private static TempDirectory CreateCombinedPassiveFindingsWorkspace()
    {
        var workspace = new TempDirectory();
        foreach (var name in RequiredCombinedPassiveArtifactNames)
        {
            File.WriteAllText(Path.Combine(workspace.Path, name), JsonSerializer.Serialize(new { success = true }, SessionJson.Options));
        }

        Directory.CreateDirectory(Path.Combine(workspace.Path, "combined-passive-session"));
        return workspace;
    }

    private static TempDirectory CreateOwnerPathHypothesesWorkspace()
    {
        var workspace = new TempDirectory();
        foreach (var name in RequiredOwnerPathArtifactNames)
        {
            File.WriteAllText(Path.Combine(workspace.Path, name), JsonSerializer.Serialize(new { success = true }, SessionJson.Options));
        }

        return workspace;
    }

    private static string WritePacket(
        string workspacePath,
        string status = "cross_session_owner_discriminator_evidence_strengthened; canonical_actor_coordinate_owner_not_promoted")
    {
        var packetPath = Path.Combine(workspacePath, "actor-coordinate-owner-discovery-packet.json");
        var packet = new JsonObject
        {
            ["schema_version"] = "riftscan.actor_coordinate_owner_discovery_packet.v2",
            ["sessions"] = new JsonObject
            {
                ["passive"] = "passive-session",
                ["move"] = "move-session"
            },
            ["target_base_address_hex"] = "0x975E1D8000",
            ["source_artifacts"] = new JsonObject
            {
                ["motion"] = Path.Combine(workspacePath, "motion.json"),
                ["move_context"] = Path.Combine(workspacePath, "move-context.json"),
                ["passive_context"] = Path.Combine(workspacePath, "passive-context.json"),
                ["context01_chain"] = Path.Combine(workspacePath, "context01-chain.json"),
                ["passive_move_chain160"] = Path.Combine(workspacePath, "chain160.json"),
                ["passive_move_chain320"] = Path.Combine(workspacePath, "chain320.json")
            },
            ["motion_summary"] = new JsonObject
            {
                ["motion_cluster_count"] = 8,
                ["synchronized_mirror_cluster_count"] = 5,
                ["canonical_promotion_status"] = "blocked_by_synchronized_mirror_clusters"
            },
            ["mirror_context_summary"] = new JsonObject
            {
                ["move"] = BuildContextSide(),
                ["passive"] = BuildContextSide()
            },
            ["cross_session_xref_chain_summary"] = new JsonObject
            {
                ["context01_min160"] = BuildXrefSummary(69),
                ["all_contexts_min160"] = BuildXrefSummary(87),
                ["all_contexts_min320"] = BuildXrefSummary(15)
            },
            ["current_assessment"] = new JsonObject
            {
                ["status"] = status,
                ["strongest_evidence"] = new JsonArray("stable exact xref support"),
                ["blocking_gaps"] = new JsonArray("synchronized mirror clusters remain")
            },
            ["next_recommended_action"] = new JsonObject
            {
                ["mode"] = "broaden_owner_region_capture",
                ["reason"] = "Need outside owner evidence.",
                ["expected_signal"] = "Unique owner/container discriminator.",
                ["stop_condition"] = "One family retains addon and xref support.",
                ["candidate_offsets_to_prioritize"] = new JsonArray("0x3A50", "0x47EC")
            }
        };
        File.WriteAllText(packetPath, packet.ToJsonString(SessionJson.Options));
        return packetPath;
    }

    private static string WriteFollowupFindings(
        string workspacePath,
        string status = "candidate_evidence_strengthened_not_promoted")
    {
        var findingsPath = Path.Combine(workspacePath, "actor-coordinate-owner-followup-findings.json");
        var findings = new JsonObject
        {
            ["schema_version"] = "riftscan.actor_coordinate_owner_followup_findings.v1",
            ["created_utc"] = "2026-04-30T08:23:06Z",
            ["status"] = status,
            ["session_id"] = "followup-session",
            ["session_path"] = Path.Combine(workspacePath, "followup-session"),
            ["source_artifacts"] = new JsonObject
            {
                ["preflight_inventory"] = Path.Combine(workspacePath, "preflight.json"),
                ["capture_result"] = Path.Combine(workspacePath, "capture.json"),
                ["verify_session"] = Path.Combine(workspacePath, "verify-session.json"),
                ["xrefs_complete"] = Path.Combine(workspacePath, "xrefs.json"),
                ["xref_chain_min2"] = Path.Combine(workspacePath, "xref-chain.json"),
                ["xref_chain_min2_verification"] = Path.Combine(workspacePath, "xref-chain-verify.json")
            },
            ["capture_summary"] = new JsonObject
            {
                ["capture_mode"] = "capture_plan_passive_idle_no_game_input",
                ["target_region_base_hex"] = "0x975E1D8000",
                ["windows_captured"] = 2,
                ["samples"] = 8,
                ["snapshots"] = 16,
                ["bytes_captured"] = 1048576,
                ["verified"] = true
            },
            ["xref_summary"] = new JsonObject
            {
                ["pointer_hit_count"] = 15072,
                ["exact_target_pointer_count"] = 38,
                ["outside_target_region_pointer_count"] = 8704,
                ["outside_exact_target_pointer_count"] = 6,
                ["stable_edge_count_min_support_2"] = 100,
                ["reciprocal_pair_count"] = 0
            },
            ["strongest_new_evidence"] = new JsonArray("0x177E0 exact pointer support"),
            ["exact_target_edges_from_followup_window"] = new JsonArray(
                BuildFollowupExactEdge("snapshot-000010", "0xF510"),
                BuildFollowupExactEdge("snapshot-000010", "0xF740"),
                BuildFollowupExactEdge("snapshot-000010", "0xF760"),
                BuildFollowupExactEdge("snapshot-000012", "0xF510"),
                BuildFollowupExactEdge("snapshot-000012", "0xF740"),
                BuildFollowupExactEdge("snapshot-000012", "0xF760")),
            ["stable_exact_target_edges_min_support_2"] = new JsonArray(new JsonObject
            {
                ["source_base_address_hex"] = "0x975E1E0000",
                ["source_offset_hex"] = "0xF510",
                ["pointer_value_hex"] = "0x975E1EF7E0",
                ["target_offset_hex"] = "0x177E0",
                ["support_count"] = 2,
                ["match_kind"] = "exact_target_offset_pointer",
                ["source_is_target_region"] = false
            }),
            ["blocking_gaps"] = new JsonArray("canonical actor-coordinate owner remains blocked"),
            ["next_recommended_action"] = new JsonObject
            {
                ["mode"] = "targeted_move_forward_followup_with_same_plan",
                ["reason"] = "Need behavior contrast.",
                ["expected_signal"] = "One family separates from mirrors.",
                ["stop_condition"] = "Unique owner/container discriminator."
            }
        };
        File.WriteAllText(findingsPath, findings.ToJsonString(SessionJson.Options));
        return findingsPath;
    }

    private static string WriteCombinedPassiveFindings(
        string workspacePath,
        string status = "same_session_external_base_edge_confirmed_not_promoted")
    {
        var findingsPath = Path.Combine(workspacePath, "actor-coordinate-owner-combined-passive-findings.json");
        var findings = new JsonObject
        {
            ["schema_version"] = "riftscan.actor_coordinate_owner_combined_passive_findings.v1",
            ["created_utc"] = "2026-04-30T08:40:21Z",
            ["status"] = status,
            ["session_id"] = "combined-passive-session",
            ["session_path"] = Path.Combine(workspacePath, "combined-passive-session"),
            ["source_artifacts"] = new JsonObject
            {
                ["preflight_inventory"] = Path.Combine(workspacePath, "combined-preflight.json"),
                ["capture_result"] = Path.Combine(workspacePath, "combined-capture.json"),
                ["verify_session"] = Path.Combine(workspacePath, "combined-verify-session.json"),
                ["target_region_xrefs"] = Path.Combine(workspacePath, "target-xrefs.json"),
                ["target_region_xref_chain_min8"] = Path.Combine(workspacePath, "target-xref-chain.json"),
                ["target_region_xref_chain_min8_verification"] = Path.Combine(workspacePath, "target-xref-chain-verify.json"),
                ["owner_region_xrefs"] = Path.Combine(workspacePath, "owner-xrefs.json"),
                ["owner_region_xref_chain_min8"] = Path.Combine(workspacePath, "owner-xref-chain.json")
            },
            ["capture_summary"] = new JsonObject
            {
                ["capture_mode"] = "passive_idle_combined_target_owner_no_game_input",
                ["regions_captured"] = 2,
                ["target_region_base_hex"] = "0x975E1D8000",
                ["owner_candidate_region_base_hex"] = "0x975E236000",
                ["samples"] = 8,
                ["snapshots"] = 16,
                ["bytes_captured"] = 884736,
                ["verified"] = true
            },
            ["target_region_xref_summary"] = new JsonObject
            {
                ["pointer_hit_count"] = 11834,
                ["exact_target_pointer_count"] = 40,
                ["outside_target_region_pointer_count"] = 8,
                ["outside_exact_target_pointer_count"] = 8,
                ["stable_edge_count_min_support_8"] = 100,
                ["reciprocal_pair_count"] = 0,
                ["external_exact_edges"] = new JsonArray(BuildCombinedExternalExactEdge())
            },
            ["owner_region_xref_summary"] = new JsonObject
            {
                ["pointer_hit_count"] = 56,
                ["exact_target_pointer_count"] = 8,
                ["outside_target_region_pointer_count"] = 0,
                ["outside_exact_target_pointer_count"] = 0,
                ["stable_edge_count_min_support_8"] = 7,
                ["reciprocal_pair_count"] = 0,
                ["interpretation"] = "owner_candidate_region_has_internal_self_pointers_but_no_target_region_reciprocal_pointer_in_combined_passive_capture"
            },
            ["strongest_new_evidence"] = new JsonArray("0x975E236000+0x1010 -> 0x975E1D8000 support 8"),
            ["blocking_gaps"] = new JsonArray("external edge points to coordinate region base rather than the discriminating 0x177E0 coordinate-family lead"),
            ["next_recommended_action"] = new JsonObject
            {
                ["mode"] = "controlled_move_forward_combined_target_owner_capture",
                ["reason"] = "Need behavior contrast.",
                ["expected_signal"] = "One coordinate-family path separates from mirror clusters.",
                ["stop_condition"] = "Unique owner/container discriminator."
            }
        };
        File.WriteAllText(findingsPath, findings.ToJsonString(SessionJson.Options));
        return findingsPath;
    }

    private static string WriteOwnerPathHypotheses(
        string workspacePath,
        string status = "external_base_edge_confirmed_no_discriminating_offset_edge_in_combined_passive_not_promoted")
    {
        var hypothesesPath = Path.Combine(workspacePath, "actor-coordinate-owner-path-hypotheses.json");
        var hypotheses = new JsonObject
        {
            ["schema_version"] = "riftscan.actor_coordinate_owner_passive_owner_path_hypotheses.v1",
            ["created_utc"] = "2026-04-30T08:54:01Z",
            ["status"] = status,
            ["session_id"] = "owner-path-session",
            ["source_artifacts"] = new JsonObject
            {
                ["combined_passive_findings"] = Path.Combine(workspacePath, "combined-findings.json"),
                ["combined_passive_findings_verification"] = Path.Combine(workspacePath, "combined-findings-verify.json"),
                ["target_region_xrefs"] = Path.Combine(workspacePath, "target-xrefs.json"),
                ["target_region_xref_chain_min8_top1000"] = Path.Combine(workspacePath, "target-chain-top1000.json"),
                ["target_region_xref_chain_min8_top1000_verification"] = Path.Combine(workspacePath, "target-chain-top1000-verify.json"),
                ["owner_region_xref_chain_min8_top1000"] = Path.Combine(workspacePath, "owner-chain-top1000.json"),
                ["owner_region_xref_chain_min8_top1000_verification"] = Path.Combine(workspacePath, "owner-chain-top1000-verify.json")
            },
            ["verification_summary"] = new JsonObject
            {
                ["combined_findings_success"] = true,
                ["target_chain_success"] = true,
                ["owner_chain_success"] = true,
                ["issues"] = new JsonArray()
            },
            ["target_region_base_hex"] = "0x975E1D8000",
            ["owner_candidate_region_base_hex"] = "0x975E236000",
            ["exact_target_edge_summary"] = new JsonObject
            {
                ["exact_pointer_hit_count"] = 40,
                ["unique_exact_edge_count"] = 5,
                ["support_sum"] = 40,
                ["exact_edge_set_is_complete_for_target_xrefs"] = true,
                ["outside_exact_edge_count"] = 1,
                ["internal_exact_edge_count"] = 4,
                ["exact_target_offsets"] = new JsonArray("0x0", "0x47EC", "0x485C", "0x48D0"),
                ["discriminating_offsets_checked"] = new JsonArray("0x176E0", "0x17730", "0x177E0"),
                ["missing_discriminating_exact_offsets"] = new JsonArray("0x176E0", "0x17730", "0x177E0")
            },
            ["external_base_edges"] = new JsonArray(BuildOwnerExternalBaseEdge()),
            ["internal_exact_edges"] = new JsonArray(
                BuildOwnerInternalExactEdge("0x4118", "0x975E1DC7EC", "0x47EC"),
                BuildOwnerInternalExactEdge("0x43B8", "0x975E1DC85C", "0x485C"),
                BuildOwnerInternalExactEdge("0x4578", "0x975E1DC8D0", "0x48D0"),
                BuildOwnerInternalExactEdge("0x4658", "0x975E1DC8D0", "0x48D0")),
            ["top1000_chain_summary"] = new JsonObject
            {
                ["target_stable_edge_count"] = 1000,
                ["target_top_limit"] = 1000,
                ["target_output_is_capped"] = true,
                ["target_reciprocal_pair_count"] = 0,
                ["owner_stable_edge_count"] = 7,
                ["owner_top_limit"] = 1000,
                ["owner_output_is_capped"] = false,
                ["owner_reciprocal_pair_count"] = 0
            },
            ["interpretation"] = new JsonArray("container/base-anchor hypothesis only"),
            ["blocking_gaps"] = new JsonArray("movement_addon_contrast_missing"),
            ["next_recommended_action"] = new JsonObject
            {
                ["mode"] = "controlled_move_forward_combined_target_owner_capture",
                ["reason"] = "Need behavior contrast.",
                ["expected_signal"] = "One coordinate family separates from mirrors.",
                ["stop_condition"] = "Unique owner/container discriminator."
            }
        };
        File.WriteAllText(hypothesesPath, hypotheses.ToJsonString(SessionJson.Options));
        return hypothesesPath;
    }

    private static JsonObject BuildFollowupExactEdge(string snapshotId, string sourceOffsetHex) =>
        new()
        {
            ["snapshot_id"] = snapshotId,
            ["source_region_id"] = "region-000039-window-0000000000008000",
            ["source_base_address_hex"] = "0x975E1E0000",
            ["source_offset_hex"] = sourceOffsetHex,
            ["source_absolute_address_hex"] = "0x975E1EF510",
            ["pointer_value_hex"] = "0x975E1EF7E0",
            ["target_offset_hex"] = "0x177E0"
        };

    private static JsonObject BuildCombinedExternalExactEdge() =>
        new()
        {
            ["source_base_address_hex"] = "0x975E236000",
            ["source_offset_hex"] = "0x1010",
            ["pointer_value_hex"] = "0x975E1D8000",
            ["target_offset_hex"] = "0x0",
            ["support_count"] = 8,
            ["match_kind"] = "exact_target_offset_pointer",
            ["source_is_target_region"] = false
        };

    private static JsonObject BuildOwnerExternalBaseEdge() =>
        new()
        {
            ["source_base_address_hex"] = "0x975E236000",
            ["source_offset_hex"] = "0x1010",
            ["source_absolute_address_hex"] = "0x975E237010",
            ["pointer_value_hex"] = "0x975E1D8000",
            ["target_offset_hex"] = "0x0",
            ["source_is_target_region"] = false,
            ["support_count"] = 8,
            ["supporting_snapshot_ids"] = BuildSnapshotIds(even: true)
        };

    private static JsonObject BuildOwnerInternalExactEdge(string sourceOffsetHex, string pointerValueHex, string targetOffsetHex) =>
        new()
        {
            ["source_base_address_hex"] = "0x975E1D8000",
            ["source_offset_hex"] = sourceOffsetHex,
            ["source_absolute_address_hex"] = "0x975E1DC118",
            ["pointer_value_hex"] = pointerValueHex,
            ["target_offset_hex"] = targetOffsetHex,
            ["source_is_target_region"] = true,
            ["support_count"] = 8,
            ["supporting_snapshot_ids"] = BuildSnapshotIds(even: false)
        };

    private static JsonArray BuildSnapshotIds(bool even)
    {
        var snapshotIds = new JsonArray();
        for (var index = even ? 2 : 1; index <= 16; index += 2)
        {
            snapshotIds.Add($"snapshot-{index:000000}");
        }

        return snapshotIds;
    }

    private static JsonObject BuildContextSide() =>
        new()
        {
            ["context_count"] = 1,
            ["contexts"] = new JsonArray(new JsonObject
            {
                ["context_id"] = "context-000001",
                ["motion_cluster_id"] = "cluster-000001"
            })
        };

    private static JsonObject BuildXrefSummary(int stableEdgeCount) =>
        new()
        {
            ["stable_edge_count"] = stableEdgeCount,
            ["reciprocal_pair_count"] = 0,
            ["stable_edges"] = new JsonArray(new JsonObject
            {
                ["edge_id"] = "xref-edge-000001"
            })
        };

    private static readonly string[] RequiredArtifactNames =
    [
        "motion.json",
        "move-context.json",
        "passive-context.json",
        "context01-chain.json",
        "chain160.json",
        "chain320.json"
    ];

    private static readonly string[] RequiredFollowupArtifactNames =
    [
        "preflight.json",
        "capture.json",
        "verify-session.json",
        "xrefs.json",
        "xref-chain.json",
        "xref-chain-verify.json"
    ];

    private static readonly string[] RequiredCombinedPassiveArtifactNames =
    [
        "combined-preflight.json",
        "combined-capture.json",
        "combined-verify-session.json",
        "target-xrefs.json",
        "target-xref-chain.json",
        "target-xref-chain-verify.json",
        "owner-xrefs.json",
        "owner-xref-chain.json"
    ];

    private static readonly string[] RequiredOwnerPathArtifactNames =
    [
        "combined-findings.json",
        "combined-findings-verify.json",
        "target-xrefs.json",
        "target-chain-top1000.json",
        "target-chain-top1000-verify.json",
        "owner-chain-top1000.json",
        "owner-chain-top1000-verify.json"
    ];

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "riftscan-actor-coordinate-packet-tests", Guid.NewGuid().ToString("N"));
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
