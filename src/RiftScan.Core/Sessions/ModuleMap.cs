using System.Text.Json.Serialization;

namespace RiftScan.Core.Sessions;

public sealed record ModuleMap
{
    [JsonPropertyName("modules")]
    public IReadOnlyList<ProcessModuleInfo> Modules { get; init; } = [];
}

public sealed record ProcessModuleInfo
{
    [JsonPropertyName("module_id")]
    public string ModuleId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("base_address_hex")]
    public string BaseAddressHex { get; init; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; init; }
}
