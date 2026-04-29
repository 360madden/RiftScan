using System.Text;

namespace RiftScan.Analysis.Reports;

public sealed class CapabilityStatusReportGenerator
{
    public string Generate(CapabilityStatusResult result, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);

        var markdown = BuildMarkdown(result);
        File.WriteAllText(fullOutputPath, markdown);
        return fullOutputPath;
    }

    private static string BuildMarkdown(CapabilityStatusResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# RiftScan capability status");
        builder.AppendLine();
        builder.AppendLine($"- Generated UTC: `{result.GeneratedUtc:O}`");
        builder.AppendLine($"- Success: `{result.Success}`");
        builder.AppendLine($"- Capability count: `{result.CapabilityCount}`");
        builder.AppendLine("- Truth claim level: capability/readiness evidence only; not final recovered truth.");
        builder.AppendLine();

        AppendEvidenceInputs(builder, result);
        AppendTruthComponents(builder, result);
        AppendList(builder, "Evidence missing", result.EvidenceMissing);
        AppendList(builder, "Next recommended actions", result.NextRecommendedActions);
        AppendList(builder, "Warnings", result.Warnings);
        AppendCapabilities(builder, result);
        return builder.ToString();
    }

    private static void AppendEvidenceInputs(StringBuilder builder, CapabilityStatusResult result)
    {
        builder.AppendLine("## Evidence inputs");
        builder.AppendLine();
        AppendPathList(builder, "Truth readiness", result.TruthReadinessPaths);
        AppendPathList(builder, "Scalar evidence set", result.ScalarEvidenceSetPaths);
        AppendPathList(builder, "Scalar truth recovery", result.ScalarTruthRecoveryPaths);
        AppendPathList(builder, "Scalar truth promotion", result.ScalarTruthPromotionPaths);
        AppendPathList(builder, "Scalar promotion review", result.ScalarPromotionReviewPaths);
        AppendPathList(builder, "RIFT promoted coordinate live", result.RiftPromotedCoordinateLivePaths);
        builder.AppendLine();
    }

    private static void AppendPathList(StringBuilder builder, string label, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            builder.AppendLine($"- {label}: none");
            return;
        }

        foreach (var path in paths)
        {
            builder.AppendLine($"- {label}: `{path}`");
        }
    }

    private static void AppendTruthComponents(StringBuilder builder, CapabilityStatusResult result)
    {
        builder.AppendLine("## Truth components");
        builder.AppendLine();
        if (result.TruthComponents.Count == 0)
        {
            builder.AppendLine("_No truth component evidence supplied._");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Component | Code status | Evidence readiness | Evidence count | Next action |");
        builder.AppendLine("|---|---|---|---:|---|");
        foreach (var component in result.TruthComponents)
        {
            builder.AppendLine($"| {Escape(component.Component)} | {Escape(component.CodeStatus)} | {Escape(component.EvidenceReadiness)} | {component.EvidenceCount} | {Escape(component.NextAction)} |");
        }

        builder.AppendLine();
    }

    private static void AppendList(StringBuilder builder, string title, IReadOnlyList<string> values)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        if (values.Count == 0)
        {
            builder.AppendLine("_None._");
            builder.AppendLine();
            return;
        }

        foreach (var value in values)
        {
            builder.AppendLine($"- `{value}`");
        }

        builder.AppendLine();
    }

    private static void AppendCapabilities(StringBuilder builder, CapabilityStatusResult result)
    {
        builder.AppendLine("## Implemented capabilities");
        builder.AppendLine();
        if (result.Capabilities.Count == 0)
        {
            builder.AppendLine("_No capabilities reported._");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Capability | Command | Evidence surface | Remaining gap |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var capability in result.Capabilities)
        {
            builder.AppendLine($"| {Escape(capability.Name)} | `{Escape(capability.PrimaryCommand)}` | {Escape(capability.EvidenceSurface)} | {Escape(capability.RemainingGap)} |");
        }

        builder.AppendLine();
    }

    private static string Escape(string value) =>
        value
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
