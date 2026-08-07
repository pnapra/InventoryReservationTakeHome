using Xunit;

namespace InventoryReservation.Tests;

public sealed class InventoryReservationEngineTests
{
    [Fact]
    public void Process_ReturnsExpectedResultForProvidedExample()
    {
        var result = Process(
            AddStock("c1", "P1", 10),
            Reserve("c2", "P1", "O100", 4),
            Reserve("c3", "P1", "O200", 8),
            AddStock("c4", "P1", 5),
            Reserve("c5", "P1", "O200", 8),
            Ship("c6", "P1", "O100", 3),
            Release("c7", "P1", "O200", 2),
            Ship("c8", "P1", "O100", 2),
            AddStock("c9", "P2", 7),
            Reserve("c10", "P2", "O300", 5),
            Reserve("c10", "P2", "O300", 5));

        AssertSummary(result.Summaries[0], "P1", onHand: 12, reserved: 7, available: 5, shipped: 3);
        AssertSummary(result.Summaries[1], "P2", onHand: 7, reserved: 5, available: 2, shipped: 0);
        Assert.Equal(new string?[] { "c3", "c8", "c10" }, result.FailedCommands);
    }

    [Fact]
    public void Process_AddStock_IncreasesOnHand()
    {
        var result = Process(AddStock("c1", "P1", 10));

        AssertSummary(SingleSummary(result), "P1", onHand: 10, reserved: 0, available: 10, shipped: 0);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_ReserveAvailableInventory_IncreasesReserved()
    {
        var result = Process(AddStock("c1", "P1", 10), Reserve("c2", "P1", "O1", 4));

        AssertSummary(SingleSummary(result), "P1", onHand: 10, reserved: 4, available: 6, shipped: 0);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_ReserveExactlyAvailableInventory_Succeeds()
    {
        var result = Process(AddStock("c1", "P1", 10), Reserve("c2", "P1", "O1", 10));

        AssertSummary(SingleSummary(result), "P1", onHand: 10, reserved: 10, available: 0, shipped: 0);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_ReserveMoreThanAvailableInventory_Fails()
    {
        var result = Process(AddStock("c1", "P1", 5), Reserve("c2", "P1", "O1", 6));

        AssertSummary(SingleSummary(result), "P1", onHand: 5, reserved: 0, available: 5, shipped: 0);
        Assert.Equal(new string?[] { "c2" }, result.FailedCommands);
    }

    [Fact]
    public void Process_ReleaseValidReservation_DecreasesReserved()
    {
        var result = Process(
            AddStock("c1", "P1", 10),
            Reserve("c2", "P1", "O1", 6),
            Release("c3", "P1", "O1", 4));

        AssertSummary(SingleSummary(result), "P1", onHand: 10, reserved: 2, available: 8, shipped: 0);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_ReleaseWhenOrderLacksEnoughReservedInventory_Fails()
    {
        var result = Process(
            AddStock("c1", "P1", 10),
            Reserve("c2", "P1", "O1", 3),
            Release("c3", "P1", "O1", 4));

        AssertSummary(SingleSummary(result), "P1", onHand: 10, reserved: 3, available: 7, shipped: 0);
        Assert.Equal(new string?[] { "c3" }, result.FailedCommands);
    }

    [Fact]
    public void Process_ShipValidReservation_IncreasesShipped()
    {
        var result = Process(
            AddStock("c1", "P1", 10),
            Reserve("c2", "P1", "O1", 3),
            Ship("c3", "P1", "O1", 2));

        AssertSummary(SingleSummary(result), "P1", onHand: 8, reserved: 1, available: 7, shipped: 2);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_ShipWhenOrderLacksEnoughReservedInventory_Fails()
    {
        var result = Process(
            AddStock("c1", "P1", 10),
            Reserve("c2", "P1", "O1", 2),
            Ship("c3", "P1", "O1", 3));

        AssertSummary(SingleSummary(result), "P1", onHand: 10, reserved: 2, available: 8, shipped: 0);
        Assert.Equal(new string?[] { "c3" }, result.FailedCommands);
    }

    [Fact]
    public void Process_Shipping_DecreasesOnHandAndReserved()
    {
        var result = Process(
            AddStock("c1", "P1", 8),
            Reserve("c2", "P1", "O1", 5),
            Ship("c3", "P1", "O1", 5));

        AssertSummary(SingleSummary(result), "P1", onHand: 3, reserved: 0, available: 3, shipped: 5);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_Release_DoesNotDecreaseOnHand()
    {
        var result = Process(
            AddStock("c1", "P1", 8),
            Reserve("c2", "P1", "O1", 5),
            Release("c3", "P1", "O1", 5));

        AssertSummary(SingleSummary(result), "P1", onHand: 8, reserved: 0, available: 8, shipped: 0);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_CommandTypesAreCaseInsensitive()
    {
        var result = Process(
            new InventoryCommand { CommandId = "c1", Type = "ADD_STOCK", Sku = "P1", Quantity = 5 },
            new InventoryCommand { CommandId = "c2", Type = "Reserve", Sku = "P1", OrderId = "O1", Quantity = 3 },
            new InventoryCommand { CommandId = "c3", Type = "ReLeAsE", Sku = "P1", OrderId = "O1", Quantity = 1 },
            new InventoryCommand { CommandId = "c4", Type = "SHIP", Sku = "P1", OrderId = "O1", Quantity = 2 });

        AssertSummary(SingleSummary(result), "P1", onHand: 3, reserved: 0, available: 3, shipped: 2);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_ZeroQuantity_Fails()
    {
        var result = Process(AddStock("c1", "P1", 0));

        Assert.Empty(result.Summaries);
        Assert.Equal(new string?[] { "c1" }, result.FailedCommands);
    }

    [Fact]
    public void Process_NegativeQuantity_Fails()
    {
        var result = Process(AddStock("c1", "P1", -1));

        Assert.Empty(result.Summaries);
        Assert.Equal(new string?[] { "c1" }, result.FailedCommands);
    }

    [Fact]
    public void Process_MissingAndBlankCommandIds_FailAndPreserveSuppliedValue()
    {
        var result = Process(
            new InventoryCommand { Type = "add_stock", Sku = "P1", Quantity = 5 },
            new InventoryCommand { CommandId = " ", Type = "add_stock", Sku = "P1", Quantity = 5 });

        Assert.Empty(result.Summaries);
        Assert.Equal(new string?[] { null, " " }, result.FailedCommands);
    }

    [Fact]
    public void Process_MissingAndBlankSkus_Fail()
    {
        var result = Process(
            new InventoryCommand { CommandId = "c1", Type = "add_stock", Quantity = 5 },
            new InventoryCommand { CommandId = "c2", Type = "add_stock", Sku = " ", Quantity = 5 });

        Assert.Empty(result.Summaries);
        Assert.Equal(new string?[] { "c1", "c2" }, result.FailedCommands);
    }

    [Fact]
    public void Process_MissingAndBlankOrderIdsForReserveReleaseAndShip_Fail()
    {
        var result = Process(
            AddStock("c1", "P1", 10),
            new InventoryCommand { CommandId = "c2", Type = "reserve", Sku = "P1", Quantity = 1 },
            new InventoryCommand { CommandId = "c3", Type = "reserve", Sku = "P1", OrderId = " ", Quantity = 1 },
            new InventoryCommand { CommandId = "c4", Type = "release", Sku = "P1", Quantity = 1 },
            new InventoryCommand { CommandId = "c5", Type = "release", Sku = "P1", OrderId = " ", Quantity = 1 },
            new InventoryCommand { CommandId = "c6", Type = "ship", Sku = "P1", Quantity = 1 },
            new InventoryCommand { CommandId = "c7", Type = "ship", Sku = "P1", OrderId = " ", Quantity = 1 });

        AssertSummary(SingleSummary(result), "P1", onHand: 10, reserved: 0, available: 10, shipped: 0);
        Assert.Equal(new string?[] { "c2", "c3", "c4", "c5", "c6", "c7" }, result.FailedCommands);
    }

    [Fact]
    public void Process_UnknownCommandType_Fails()
    {
        var result = Process(new InventoryCommand { CommandId = "c1", Type = "adjust", Sku = "P1", Quantity = 5 });

        Assert.Empty(result.Summaries);
        Assert.Equal(new string?[] { "c1" }, result.FailedCommands);
    }

    [Fact]
    public void Process_DuplicateCommandIds_FailLaterOccurrences()
    {
        var result = Process(
            AddStock("c1", "P1", 5),
            AddStock("c1", "P1", 5),
            AddStock("c1", "P1", 5));

        AssertSummary(SingleSummary(result), "P1", onHand: 5, reserved: 0, available: 5, shipped: 0);
        Assert.Equal(new string?[] { "c1", "c1" }, result.FailedCommands);
    }

    [Fact]
    public void Process_FailedFirstOccurrenceStillConsumesNonblankCommandId()
    {
        var result = Process(
            new InventoryCommand { CommandId = "c1", Type = "add_stock", Sku = "P1", Quantity = 0 },
            AddStock("c1", "P1", 5));

        Assert.Empty(result.Summaries);
        Assert.Equal(new string?[] { "c1", "c1" }, result.FailedCommands);
    }

    [Fact]
    public void Process_FailedCommandsDoNotMutateState()
    {
        var result = Process(
            AddStock("c1", "P1", 5),
            Reserve("c2", "P1", "O1", 6),
            Reserve("c3", "P1", "O1", 5),
            Ship("c4", "P1", "O1", 6),
            Release("c5", "P1", "O1", 5));

        AssertSummary(SingleSummary(result), "P1", onHand: 5, reserved: 0, available: 5, shipped: 0);
        Assert.Equal(new string?[] { "c2", "c4" }, result.FailedCommands);
    }

    [Fact]
    public void Process_MultipleOrdersReserveTheSameSkuIndependently()
    {
        var result = Process(
            AddStock("c1", "P1", 10),
            Reserve("c2", "P1", "O1", 4),
            Reserve("c3", "P1", "O2", 3),
            Release("c4", "P1", "O1", 4),
            Ship("c5", "P1", "O2", 3));

        AssertSummary(SingleSummary(result), "P1", onHand: 7, reserved: 0, available: 7, shipped: 3);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_RepeatedReservationsForSameOrderAndSku_AreAccumulated()
    {
        var result = Process(
            AddStock("c1", "P1", 10),
            Reserve("c2", "P1", "O1", 3),
            Reserve("c3", "P1", "O1", 2),
            Release("c4", "P1", "O1", 4),
            Ship("c5", "P1", "O1", 1));

        AssertSummary(SingleSummary(result), "P1", onHand: 9, reserved: 0, available: 9, shipped: 1);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_SameOrderReservesMultipleSkusIndependently()
    {
        var result = Process(
            AddStock("c1", "P1", 10),
            AddStock("c2", "P2", 8),
            Reserve("c3", "P1", "O1", 4),
            Reserve("c4", "P2", "O1", 3),
            Ship("c5", "P1", "O1", 4),
            Release("c6", "P2", "O1", 3));

        AssertSummary(result.Summaries[0], "P1", onHand: 6, reserved: 0, available: 6, shipped: 4);
        AssertSummary(result.Summaries[1], "P2", onHand: 8, reserved: 0, available: 8, shipped: 0);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_ExcludesSkusWithNoSuccessfulCommands()
    {
        var result = Process(Reserve("c1", "P1", "O1", 1));

        Assert.Empty(result.Summaries);
        Assert.Equal(new string?[] { "c1" }, result.FailedCommands);
    }

    [Fact]
    public void Process_SortsSummariesBySkuUsingOrdinalOrdering()
    {
        var result = Process(
            AddStock("c1", "b", 1),
            AddStock("c2", "A", 1),
            AddStock("c3", "a", 1),
            AddStock("c4", "B", 1));

        Assert.Equal(new[] { "A", "B", "a", "b" }, result.Summaries.Select(summary => summary.Sku));
    }

    [Fact]
    public void Process_FailedCommandIdsRemainInProcessingOrder()
    {
        var result = Process(
            AddStock("c1", "P1", 1),
            Reserve("c2", "P1", "O1", 2),
            new InventoryCommand { CommandId = "c3", Type = "add_stock", Sku = "P1", Quantity = 0 },
            new InventoryCommand { CommandId = "c4", Type = "unknown", Sku = "P1", Quantity = 1 },
            AddStock("c1", "P1", 1));

        Assert.Equal(new string?[] { "c2", "c3", "c4", "c1" }, result.FailedCommands);
    }

    [Fact]
    public void Process_NullCommandCollection_ReturnsEmptyResult()
    {
        var result = new InventoryReservationEngine().Process(null);

        Assert.Empty(result.Summaries);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_EmptyCommandCollection_ReturnsEmptyResult()
    {
        var result = Process();

        Assert.Empty(result.Summaries);
        Assert.Empty(result.FailedCommands);
    }

    [Fact]
    public void Process_UsesCaseSensitiveOrdinalComparisonForIdentifiers()
    {
        var result = Process(
            AddStock("c1", "P1", 5),
            AddStock("C1", "p1", 5),
            Reserve("c2", "P1", "O1", 2),
            Reserve("c3", "P1", "o1", 2),
            Ship("c4", "P1", "O1", 2));

        AssertSummary(result.Summaries[0], "P1", onHand: 3, reserved: 2, available: 1, shipped: 2);
        AssertSummary(result.Summaries[1], "p1", onHand: 5, reserved: 0, available: 5, shipped: 0);
        Assert.Empty(result.FailedCommands);
    }

    private static InventoryResult Process(params InventoryCommand[] commands)
    {
        return new InventoryReservationEngine().Process(commands);
    }

    private static InventoryCommand AddStock(string commandId, string sku, int quantity)
    {
        return new InventoryCommand
        {
            CommandId = commandId,
            Type = "add_stock",
            Sku = sku,
            Quantity = quantity
        };
    }

    private static InventoryCommand Reserve(string commandId, string sku, string orderId, int quantity)
    {
        return new InventoryCommand
        {
            CommandId = commandId,
            Type = "reserve",
            Sku = sku,
            OrderId = orderId,
            Quantity = quantity
        };
    }

    private static InventoryCommand Release(string commandId, string sku, string orderId, int quantity)
    {
        return new InventoryCommand
        {
            CommandId = commandId,
            Type = "release",
            Sku = sku,
            OrderId = orderId,
            Quantity = quantity
        };
    }

    private static InventoryCommand Ship(string commandId, string sku, string orderId, int quantity)
    {
        return new InventoryCommand
        {
            CommandId = commandId,
            Type = "ship",
            Sku = sku,
            OrderId = orderId,
            Quantity = quantity
        };
    }

    private static InventorySummary SingleSummary(InventoryResult result)
    {
        return Assert.Single(result.Summaries);
    }

    private static void AssertSummary(
        InventorySummary summary,
        string sku,
        int onHand,
        int reserved,
        int available,
        int shipped)
    {
        Assert.Equal(sku, summary.Sku);
        Assert.Equal(onHand, summary.OnHand);
        Assert.Equal(reserved, summary.Reserved);
        Assert.Equal(available, summary.Available);
        Assert.Equal(shipped, summary.Shipped);
    }
}
