using System.Security.Cryptography;

namespace RiftScan.Core.Sessions;

public static class SessionChecksum
{
    public static string ComputeSha256Hex(string path)
    {
        using var stream = File.OpenRead(path);
        return ComputeSha256Hex(stream);
    }

    public static string ComputeSha256Hex(Stream stream)
    {
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
