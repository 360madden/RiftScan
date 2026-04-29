using System.Text.Json;
using RiftScan.Analysis.Clusters;
using RiftScan.Analysis.Deltas;
using RiftScan.Analysis.Structures;
using RiftScan.Analysis.Triage;
using RiftScan.Analysis.Values;
using RiftScan.Analysis.Vectors;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class AnalyzerOutputContractTests
{
    [Fact]
    public void Region_triage_entry_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RegionTriageEntry(),
            "analyzer_id",
            "analyzer_version",
            "session_id",
            "region_id",
            "base_address_hex",
            "snapshot_count",
            "unique_checksum_count",
            "total_bytes",
            "byte_entropy",
            "zero_byte_ratio",
            "rank_score",
            "recommendation",
            "diagnostics");

    [Fact]
    public void Region_delta_entry_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new RegionDeltaEntry(),
            "analyzer_id",
            "analyzer_version",
            "session_id",
            "region_id",
            "base_address_hex",
            "snapshot_count",
            "compared_pair_count",
            "changed_byte_count",
            "changed_byte_ratio",
            "pair_change_ratio",
            "changed_range_count",
            "rank_score",
            "recommendation",
            "changed_ranges");

    [Fact]
    public void Typed_value_candidate_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new TypedValueCandidate(),
            "analyzer_id",
            "analyzer_version",
            "candidate_id",
            "session_id",
            "region_id",
            "base_address_hex",
            "offset_hex",
            "absolute_address_hex",
            "data_type",
            "sample_count",
            "distinct_value_count",
            "changed_sample_count",
            "rank_score",
            "score_breakdown",
            "feature_vector",
            "validation_status",
            "confidence_level",
            "explanation_short",
            "recommendation",
            "value_preview",
            "diagnostics");

    [Fact]
    public void Structure_candidate_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new StructureCandidate(),
            "analyzer_id",
            "analyzer_version",
            "session_id",
            "candidate_id",
            "region_id",
            "base_address_hex",
            "offset_hex",
            "absolute_address_hex",
            "structure_kind",
            "snapshot_support",
            "score",
            "score_breakdown",
            "feature_vector",
            "validation_status",
            "confidence_level",
            "explanation_short",
            "value_preview",
            "diagnostics");

    [Fact]
    public void Vec3_candidate_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new Vec3Candidate(),
            "analyzer_id",
            "analyzer_version",
            "candidate_id",
            "session_id",
            "region_id",
            "base_address_hex",
            "offset_hex",
            "absolute_address_hex",
            "data_type",
            "source_structure_kind",
            "snapshot_support",
            "stimulus_label",
            "sample_value_count",
            "value_delta_magnitude",
            "behavior_score",
            "rank_score",
            "score_breakdown",
            "feature_vector",
            "validation_status",
            "confidence_level",
            "explanation_short",
            "recommendation",
            "value_preview",
            "diagnostics");

    [Fact]
    public void Structure_cluster_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new StructureCluster(),
            "analyzer_id",
            "analyzer_version",
            "session_id",
            "cluster_id",
            "region_id",
            "base_address_hex",
            "start_offset_hex",
            "end_offset_hex",
            "candidate_count",
            "span_bytes",
            "average_score",
            "rank_score",
            "recommendation",
            "diagnostics");

    private static void AssertJsonPropertySet<T>(T value, params string[] expectedProperties)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, SessionJson.Options));
        var actualProperties = document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expected = expectedProperties
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actualProperties);
    }
}
