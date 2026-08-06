namespace InventoryReservation;

public sealed record InventoryResult(
    IReadOnlyList<InventorySummary> Summaries,
    IReadOnlyList<string?> FailedCommands);
