namespace RiftScan.Analysis.Comparison;

internal sealed record ScalarCandidateComparison
{
    public string BaseAddressHex { get; init; } = string.Empty;

    public string OffsetHex { get; init; } = string.Empty;

    public string DataType { get; init; } = string.Empty;

    public string SessionACandidateId { get; init; } = string.Empty;

    public string SessionBCandidateId { get; init; } = string.Empty;

    public string SessionARegionId { get; init; } = string.Empty;

    public string SessionBRegionId { get; init; } = string.Empty;

    public double SessionARankScore { get; init; }

    public double SessionBRankScore { get; init; }

    public int SessionAChangedSampleCount { get; init; }

    public int SessionBChangedSampleCount { get; init; }

    public double SessionAValueDeltaMagnitude { get; init; }

    public double SessionBValueDeltaMagnitude { get; init; }

    public double SessionACircularDeltaMagnitude { get; init; }

    public double SessionBCircularDeltaMagnitude { get; init; }

    public double SessionASignedCircularDelta { get; init; }

    public double SessionBSignedCircularDelta { get; init; }

    public string SessionADominantDirection { get; init; } = string.Empty;

    public string SessionBDominantDirection { get; init; } = string.Empty;

    public string TurnPolarityRelationship { get; init; } = string.Empty;

    public string CameraTurnSeparationRelationship { get; init; } = string.Empty;

    public string ValueFamily { get; init; } = string.Empty;

    public double SessionADirectionConsistencyRatio { get; init; }

    public double SessionBDirectionConsistencyRatio { get; init; }

    public string SessionAStimulusLabel { get; init; } = string.Empty;

    public string SessionBStimulusLabel { get; init; } = string.Empty;

    public string SessionAValueSequenceSummary { get; init; } = string.Empty;

    public string SessionBValueSequenceSummary { get; init; } = string.Empty;

    public IReadOnlyList<string> SessionAAnalyzerSources { get; init; } = [];

    public IReadOnlyList<string> SessionBAnalyzerSources { get; init; } = [];
}
