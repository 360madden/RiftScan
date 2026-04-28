namespace RiftScan.Core.Processes;

public sealed record ProcessDescriptor(
    int ProcessId,
    string ProcessName,
    DateTimeOffset? StartTimeUtc,
    string? MainModulePath);
