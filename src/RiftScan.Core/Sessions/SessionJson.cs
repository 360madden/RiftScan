using System.Text.Json;

namespace RiftScan.Core.Sessions;

public static class SessionJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
