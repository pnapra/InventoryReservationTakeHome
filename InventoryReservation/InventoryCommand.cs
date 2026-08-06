namespace InventoryReservation;

public sealed record InventoryCommand
{
    public string? CommandId { get; init; }

    public string? Type { get; init; }

    public string? Sku { get; init; }

    public int Quantity { get; init; }

    public string? OrderId { get; init; }
}
