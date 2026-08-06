using System.Diagnostics.CodeAnalysis;

namespace InventoryReservation;

public sealed class InventoryReservationEngine
{
    public InventoryResult Process(IEnumerable<InventoryCommand>? commands)
    {
        var inventoryBySku = new Dictionary<string, SkuInventoryState>(StringComparer.Ordinal);
        var seenCommandIds = new HashSet<string>(StringComparer.Ordinal);
        var failedCommands = new List<string?>();

        foreach (var command in commands ?? Enumerable.Empty<InventoryCommand>())
        {
            var duplicateCommandId = IsDuplicate(command, seenCommandIds);

            if (!TryGetValidCommand(command, duplicateCommandId, out var validCommand))
            {
                failedCommands.Add(command?.CommandId);
                continue;
            }

            if (!TryApply(validCommand, inventoryBySku))
            {
                failedCommands.Add(validCommand.CommandId);
            }
        }

        var summaries = inventoryBySku
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => new InventorySummary
            {
                Sku = item.Key,
                OnHand = item.Value.OnHand,
                Reserved = item.Value.Reserved,
                Shipped = item.Value.Shipped
            })
            .ToArray();

        return new InventoryResult(summaries, failedCommands.ToArray());
    }

    private static bool IsDuplicate(InventoryCommand? command, HashSet<string> seenCommandIds)
    {
        var commandId = command?.CommandId;

        if (IsBlank(commandId))
        {
            return false;
        }

        return !seenCommandIds.Add(commandId);
    }

    private static bool TryGetValidCommand(
        InventoryCommand? command,
        bool duplicateCommandId,
        out ValidInventoryCommand validCommand)
    {
        validCommand = default;

        if (command is null)
        {
            return false;
        }

        var commandId = command.CommandId;
        var sku = command.Sku;

        if (duplicateCommandId ||
            IsBlank(commandId) ||
            IsBlank(sku) ||
            command.Quantity <= 0 ||
            !TryParseCommandType(command.Type, out var type))
        {
            return false;
        }

        if (RequiresOrderId(type) && IsBlank(command.OrderId))
        {
            return false;
        }

        validCommand = new ValidInventoryCommand(
            commandId,
            type,
            sku,
            command.Quantity,
            command.OrderId);

        return true;
    }

    private static bool TryApply(
        ValidInventoryCommand command,
        Dictionary<string, SkuInventoryState> inventoryBySku)
    {
        return command.Type switch
        {
            CommandType.AddStock => ApplyAddStock(command, inventoryBySku),
            CommandType.Reserve => ApplyReserve(command, inventoryBySku),
            CommandType.Release => ApplyRelease(command, inventoryBySku),
            CommandType.Ship => ApplyShip(command, inventoryBySku),
            _ => false
        };
    }

    private static bool ApplyAddStock(
        ValidInventoryCommand command,
        Dictionary<string, SkuInventoryState> inventoryBySku)
    {
        var inventory = GetOrCreateInventory(command.Sku, inventoryBySku);
        inventory.AddStock(command.Quantity);
        return true;
    }

    private static bool ApplyReserve(
        ValidInventoryCommand command,
        Dictionary<string, SkuInventoryState> inventoryBySku)
    {
        if (!inventoryBySku.TryGetValue(command.Sku, out var inventory) ||
            !inventory.CanReserve(command.Quantity))
        {
            return false;
        }

        inventory.Reserve(command.OrderId!, command.Quantity);
        return true;
    }

    private static bool ApplyRelease(
        ValidInventoryCommand command,
        Dictionary<string, SkuInventoryState> inventoryBySku)
    {
        if (!inventoryBySku.TryGetValue(command.Sku, out var inventory) ||
            !inventory.HasReservation(command.OrderId!, command.Quantity))
        {
            return false;
        }

        inventory.Release(command.OrderId!, command.Quantity);
        return true;
    }

    private static bool ApplyShip(
        ValidInventoryCommand command,
        Dictionary<string, SkuInventoryState> inventoryBySku)
    {
        if (!inventoryBySku.TryGetValue(command.Sku, out var inventory) ||
            !inventory.HasReservation(command.OrderId!, command.Quantity))
        {
            return false;
        }

        inventory.Ship(command.OrderId!, command.Quantity);
        return true;
    }

    private static SkuInventoryState GetOrCreateInventory(
        string sku,
        Dictionary<string, SkuInventoryState> inventoryBySku)
    {
        if (!inventoryBySku.TryGetValue(sku, out var inventory))
        {
            inventory = new SkuInventoryState();
            inventoryBySku.Add(sku, inventory);
        }

        return inventory;
    }

    private static bool TryParseCommandType(string? type, out CommandType commandType)
    {
        if (string.Equals(type, "add_stock", StringComparison.OrdinalIgnoreCase))
        {
            commandType = CommandType.AddStock;
            return true;
        }

        if (string.Equals(type, "reserve", StringComparison.OrdinalIgnoreCase))
        {
            commandType = CommandType.Reserve;
            return true;
        }

        if (string.Equals(type, "release", StringComparison.OrdinalIgnoreCase))
        {
            commandType = CommandType.Release;
            return true;
        }

        if (string.Equals(type, "ship", StringComparison.OrdinalIgnoreCase))
        {
            commandType = CommandType.Ship;
            return true;
        }

        commandType = default;
        return false;
    }

    private static bool RequiresOrderId(CommandType type)
    {
        return type is CommandType.Reserve or CommandType.Release or CommandType.Ship;
    }

    private static bool IsBlank([NotNullWhen(false)] string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    private readonly record struct ValidInventoryCommand(
        string CommandId,
        CommandType Type,
        string Sku,
        int Quantity,
        string? OrderId);

    private enum CommandType
    {
        AddStock,
        Reserve,
        Release,
        Ship
    }
}
