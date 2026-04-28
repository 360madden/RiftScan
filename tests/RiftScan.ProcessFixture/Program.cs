using System.Buffers.Binary;

var readyFile = ReadOption(args, "--ready-file");
var exitAfterMilliseconds = ReadIntOption(args, "--exit-after-ms");
var payload = new byte[1024 * 1024];
for (var offset = 0; offset + sizeof(long) <= payload.Length; offset += sizeof(long))
{
    BinaryPrimitives.WriteInt64LittleEndian(payload.AsSpan(offset), Environment.ProcessId + offset);
}

if (!string.IsNullOrWhiteSpace(readyFile))
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(readyFile))!);
    File.WriteAllText(readyFile, Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
}

var exitAfterUtc = exitAfterMilliseconds is { } milliseconds
    ? DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(milliseconds)
    : (DateTimeOffset?)null;

while (exitAfterUtc is null || DateTimeOffset.UtcNow < exitAfterUtc)
{
    GC.KeepAlive(payload);
    Thread.Sleep(250);
}

static string? ReadOption(string[] args, string name)
{
    for (var index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[index + 1];
        }
    }

    return null;
}

static int? ReadIntOption(string[] args, string name)
{
    var value = ReadOption(args, name);
    return string.IsNullOrWhiteSpace(value)
        ? null
        : int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
