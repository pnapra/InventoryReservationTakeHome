namespace InventoryReservation;

internal sealed class SkuInventoryState
{
    private readonly Dictionary<string, int> _reservationsByOrderId = new(StringComparer.Ordinal);

    public int OnHand { get; private set; }

    public int Reserved { get; private set; }

    public int Shipped { get; private set; }

    public int Available => OnHand - Reserved;

    public void AddStock(int quantity)
    {
        OnHand += quantity;
    }

    public bool CanReserve(int quantity)
    {
        return Available >= quantity;
    }

    public void Reserve(string orderId, int quantity)
    {
        Reserved += quantity;
        _reservationsByOrderId[orderId] = GetReservedForOrder(orderId) + quantity;
    }

    public bool HasReservation(string orderId, int quantity)
    {
        return GetReservedForOrder(orderId) >= quantity;
    }

    public void Release(string orderId, int quantity)
    {
        Reserved -= quantity;
        DecreaseReservation(orderId, quantity);
    }

    public void Ship(string orderId, int quantity)
    {
        OnHand -= quantity;
        Reserved -= quantity;
        Shipped += quantity;
        DecreaseReservation(orderId, quantity);
    }

    private int GetReservedForOrder(string orderId)
    {
        return _reservationsByOrderId.TryGetValue(orderId, out var quantity)
            ? quantity
            : 0;
    }

    private void DecreaseReservation(string orderId, int quantity)
    {
        var remaining = _reservationsByOrderId[orderId] - quantity;

        if (remaining == 0)
        {
            _reservationsByOrderId.Remove(orderId);
            return;
        }

        _reservationsByOrderId[orderId] = remaining;
    }
}
