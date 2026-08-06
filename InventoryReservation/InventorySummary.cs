namespace InventoryReservation;

public sealed record InventorySummary
{
    public required string Sku { get; init; }

    public int OnHand { get; init; }

    public int Reserved { get; init; }

    public int Available => OnHand - Reserved;

    public int Shipped { get; init; }
}
