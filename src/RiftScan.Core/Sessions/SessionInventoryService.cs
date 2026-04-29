using System.Text.Json;

namespace RiftScan.Core.Sessions;

public sealed class SessionInventoryService
{
    public SessionInventoryResult Inventory(string sessionPath, string? inventoryOutputPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionPath);
        var fullSessionPath = Path.GetFullPath(sessionPath);
        var summary = new SessionSummaryService().Summarize(fullSessionPath);
        var pruneInventory = new SessionPruneService().Prune(fullSessionPath);
        var issues = summary.Issues.Concat(pruneInventory.Issues).ToArray();

        var result = new SessionInventoryResult
        {
            SessionPath = fullSessionPath,
            Summary = summary,
            PruneInventory = pruneInventory,
            Issues = issues
        };

        return WriteInventoryIfRequested(result, inventoryOutputPath);
    }

    private static SessionInventoryResult WriteInventoryIfRequested(SessionInventoryResult result, string? inventoryOutputPath)
    {
        if (string.IsNullOrWhiteSpace(inventoryOutputPath))
        {
            return result;
        }

        var fullInventoryPath = Path.GetFullPath(inventoryOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullInventoryPath) ?? ".");
        var resultWithInventoryPath = result with { InventoryPath = fullInventoryPath };
        File.WriteAllText(fullInventoryPath, JsonSerializer.Serialize(resultWithInventoryPath, SessionJson.Options));
        return resultWithInventoryPath;
    }
}
