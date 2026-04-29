using System.Text.Json;
using RiftScan.Core.Sessions;

namespace RiftScan.Analysis.Comparison;

public sealed class ScalarTruthCorroborationVerifier
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "uncorroborated",
        "corroborated",
        "conflicted"
    };

    public ScalarTruthCorroborationVerificationResult Verify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var issues = new List<ScalarTruthCorroborationVerificationIssue>();
        var entryCount = 0;
        if (!File.Exists(fullPath))
        {
            issues.Add(Error("file_missing", "Scalar truth corroboration file does not exist.", null));
            return new ScalarTruthCorroborationVerificationResult { Path = fullPath, EntryCount = entryCount, Issues = issues };
        }

        var lineNumber = 0;
        foreach (var line in File.ReadLines(fullPath))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            ScalarTruthCorroborationEntry? entry;
            try
            {
                entry = JsonSerializer.Deserialize<ScalarTruthCorroborationEntry>(line, SessionJson.Options);
            }
            catch (JsonException ex)
            {
                issues.Add(Error("json_invalid", $"Invalid JSONL entry: {ex.Message}", lineNumber));
                continue;
            }

            if (entry is null)
            {
                issues.Add(Error("entry_empty", "Corroboration line did not contain an object.", lineNumber));
                continue;
            }

            entryCount++;
            ValidateEntry(entry, lineNumber, issues);
        }

        if (entryCount == 0)
        {
            issues.Add(Error("file_empty", "Scalar truth corroboration file must contain at least one entry.", null));
        }

        return new ScalarTruthCorroborationVerificationResult { Path = fullPath, EntryCount = entryCount, Issues = issues };
    }

    private static void ValidateEntry(
        ScalarTruthCorroborationEntry entry,
        int lineNumber,
        ICollection<ScalarTruthCorroborationVerificationIssue> issues)
    {
        if (!string.Equals(entry.SchemaVersion, "riftscan.scalar_truth_corroboration.v1", StringComparison.Ordinal))
        {
            issues.Add(Error("schema_version_invalid", "schema_version must be riftscan.scalar_truth_corroboration.v1.", lineNumber));
        }

        Require(entry.BaseAddressHex, "base_address_missing", "base_address_hex is required.", lineNumber, issues);
        Require(entry.OffsetHex, "offset_missing", "offset_hex is required.", lineNumber, issues);
        Require(entry.DataType, "data_type_missing", "data_type is required.", lineNumber, issues);
        Require(entry.CorroborationStatus, "corroboration_status_missing", "corroboration_status is required.", lineNumber, issues);
        Require(entry.Source, "source_missing", "source is required.", lineNumber, issues);
        if (!string.IsNullOrWhiteSpace(entry.CorroborationStatus) && !AllowedStatuses.Contains(entry.CorroborationStatus))
        {
            issues.Add(Error("corroboration_status_invalid", "corroboration_status must be uncorroborated, corroborated, or conflicted.", lineNumber));
        }

        if (!LooksLikeHex(entry.BaseAddressHex))
        {
            issues.Add(Error("base_address_invalid", "base_address_hex must be hexadecimal, for example 0x1000.", lineNumber));
        }

        if (!LooksLikeHex(entry.OffsetHex))
        {
            issues.Add(Error("offset_invalid", "offset_hex must be hexadecimal, for example 0x4.", lineNumber));
        }
    }

    private static void Require(
        string value,
        string code,
        string message,
        int lineNumber,
        ICollection<ScalarTruthCorroborationVerificationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Error(code, message, lineNumber));
        }
    }

    private static bool LooksLikeHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return text.Length > 0 && text.All(Uri.IsHexDigit);
    }

    private static ScalarTruthCorroborationVerificationIssue Error(string code, string message, int? lineNumber) =>
        new()
        {
            Code = code,
            Message = message,
            LineNumber = lineNumber
        };
}
