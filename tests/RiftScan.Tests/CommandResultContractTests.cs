using System.Text.Json;
using RiftScan.Analysis.Reports;
using RiftScan.Analysis.Triage;
using RiftScan.Core.Sessions;

namespace RiftScan.Tests;

public sealed class CommandResultContractTests
{
    [Fact]
    public void Session_analysis_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionAnalysisResult(),
            "result_schema_version",
            "success",
            "session_path",
            "session_id",
            "regions_analyzed",
            "artifacts_written");

    [Fact]
    public void Session_report_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionReportResult(),
            "result_schema_version",
            "success",
            "session_path",
            "report_path",
            "report_json_path");

    [Fact]
    public void Session_verification_result_pins_json_contract_fields() =>
        AssertJsonPropertySet(
            new SessionVerificationResult(),
            "result_schema_version",
            "success",
            "session_path",
            "session_id",
            "artifacts_verified",
            "issues");

    [Fact]
    public void Command_result_schema_versions_are_stable()
    {
        Assert.Equal("riftscan.session_analysis_result.v1", ReadSchema(new SessionAnalysisResult()));
        Assert.Equal("riftscan.session_report_result.v1", ReadSchema(new SessionReportResult()));
        Assert.Equal("riftscan.session_verification_result.v1", ReadSchema(new SessionVerificationResult()));
    }

    private static string? ReadSchema<T>(T value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, SessionJson.Options));
        return document.RootElement.GetProperty("result_schema_version").GetString();
    }

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
