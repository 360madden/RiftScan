using System.Globalization;

namespace RiftScan.Analysis.Regions;

public static class KnownSystemRegionClassifier
{
    public const string Diagnostic = "known_windows_kuser_shared_data_region";

    public const string TriageRecommendation = "exclude_known_system_region_from_game_truth_ranking";

    public const string ValueRecommendation = "known_system_region_value_lane_ignored";

    public const double TriageRankScoreCap = 1.0;

    public const double ValueRankScoreCap = 5.0;

    public static bool IsKnownSystemNoise(string baseAddressHex) =>
        TryParseHex(baseAddressHex, out var baseAddress) && baseAddress == 0x7FFE0000UL;

    private static bool TryParseHex(string value, out ulong parsed)
    {
        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed);
    }
}
