using System.Text.Json.Serialization;

namespace RiftScan.Analysis.Comparison;

public sealed record ScalarEvidenceSetResult
{
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "riftscan.scalar_evidence_set.v1";

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("session_paths")]
    public IReadOnlyList<string> SessionPaths { get; init; } = [];

    [JsonPropertyName("output_path")]
    public string? OutputPath { get; init; }

    [JsonPropertyName("report_path")]
    public string? ReportPath { get; init; }

    [JsonPropertyName("truth_candidate_path")]
    public string? TruthCandidatePath { get; init; }

    [JsonPropertyName("session_count")]
    public int SessionCount { get; init; }

    [JsonPropertyName("scalar_candidate_key_count")]
    public int ScalarCandidateKeyCount { get; init; }

    [JsonPropertyName("ranked_candidate_count")]
    public int RankedCandidateCount { get; init; }

    [JsonPropertyName("session_summaries")]
    public IReadOnlyList<ScalarEvidenceSessionSummary> SessionSummaries { get; init; } = [];

    [JsonPropertyName("ranked_candidates")]
    public IReadOnlyList<ScalarEvidenceAggregateCandidate> RankedCandidates { get; init; } = [];

    [JsonPropertyName("rejected_candidate_summaries")]
    public IReadOnlyList<ScalarEvidenceRejectedSummary> RejectedCandidateSummaries { get; init; } = [];

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record ScalarEvidenceSessionSummary
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("session_path")]
    public string SessionPath { get; init; } = string.Empty;

    [JsonPropertyName("stimulus_label")]
    public string StimulusLabel { get; init; } = string.Empty;

    [JsonPropertyName("scalar_candidate_count")]
    public int ScalarCandidateCount { get; init; }
}

public sealed record ScalarEvidenceAggregateCandidate
{
    [JsonPropertyName("classification")]
    public string Classification { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("data_type")]
    public string DataType { get; init; } = string.Empty;

    [JsonPropertyName("value_family")]
    public string ValueFamily { get; init; } = string.Empty;

    [JsonPropertyName("score_total")]
    public double ScoreTotal { get; init; }

    [JsonPropertyName("confidence_level")]
    public string ConfidenceLevel { get; init; } = "weak_candidate";

    [JsonPropertyName("truth_readiness")]
    public string TruthReadiness { get; init; } = "insufficient";

    [JsonPropertyName("score_breakdown")]
    public IReadOnlyDictionary<string, double> ScoreBreakdown { get; init; } = new Dictionary<string, double>();

    [JsonPropertyName("labels_present")]
    public IReadOnlyList<string> LabelsPresent { get; init; } = [];

    [JsonPropertyName("sessions_present")]
    public int SessionsPresent { get; init; }

    [JsonPropertyName("passive_stable")]
    public bool PassiveStable { get; init; }

    [JsonPropertyName("turn_left_changed")]
    public bool TurnLeftChanged { get; init; }

    [JsonPropertyName("turn_right_changed")]
    public bool TurnRightChanged { get; init; }

    [JsonPropertyName("camera_only_changed")]
    public bool CameraOnlyChanged { get; init; }

    [JsonPropertyName("opposite_turn_polarity")]
    public bool OppositeTurnPolarity { get; init; }

    [JsonPropertyName("camera_turn_separation")]
    public string CameraTurnSeparation { get; init; } = string.Empty;

    [JsonPropertyName("turn_left_signed_delta")]
    public double TurnLeftSignedDelta { get; init; }

    [JsonPropertyName("turn_right_signed_delta")]
    public double TurnRightSignedDelta { get; init; }

    [JsonPropertyName("camera_only_signed_delta")]
    public double CameraOnlySignedDelta { get; init; }

    [JsonPropertyName("supporting_reasons")]
    public IReadOnlyList<string> SupportingReasons { get; init; } = [];

    [JsonPropertyName("rejection_reasons")]
    public IReadOnlyList<string> RejectionReasons { get; init; } = [];

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;

    [JsonPropertyName("next_validation_step")]
    public string NextValidationStep { get; init; } = string.Empty;
}

public sealed record ScalarEvidenceRejectedSummary
{
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("example_candidates")]
    public IReadOnlyList<ScalarEvidenceRejectedExample> ExampleCandidates { get; init; } = [];
}

public sealed record ScalarEvidenceRejectedExample
{
    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("offset_hex")]
    public string OffsetHex { get; init; } = string.Empty;

    [JsonPropertyName("value_family")]
    public string ValueFamily { get; init; } = string.Empty;

    [JsonPropertyName("score_total")]
    public double ScoreTotal { get; init; }

    [JsonPropertyName("evidence_summary")]
    public string EvidenceSummary { get; init; } = string.Empty;
}
