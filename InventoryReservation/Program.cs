using System.Text.Json;
using InventoryReservation;

var commands = new[]
{
    new InventoryCommand { CommandId = "c1", Type = "add_stock", Sku = "P1", Quantity = 10 },
    new InventoryCommand { CommandId = "c2", Type = "reserve", OrderId = "O100", Sku = "P1", Quantity = 4 },
    new InventoryCommand { CommandId = "c3", Type = "reserve", OrderId = "O200", Sku = "P1", Quantity = 8 },
    new InventoryCommand { CommandId = "c4", Type = "add_stock", Sku = "P1", Quantity = 5 },
    new InventoryCommand { CommandId = "c5", Type = "reserve", OrderId = "O200", Sku = "P1", Quantity = 8 },
    new InventoryCommand { CommandId = "c6", Type = "ship", OrderId = "O100", Sku = "P1", Quantity = 3 },
    new InventoryCommand { CommandId = "c7", Type = "release", OrderId = "O200", Sku = "P1", Quantity = 2 },
    new InventoryCommand { CommandId = "c8", Type = "ship", OrderId = "O100", Sku = "P1", Quantity = 2 },
    new InventoryCommand { CommandId = "c9", Type = "add_stock", Sku = "P2", Quantity = 7 },
    new InventoryCommand { CommandId = "c10", Type = "reserve", OrderId = "O300", Sku = "P2", Quantity = 5 },
    new InventoryCommand { CommandId = "c10", Type = "reserve", OrderId = "O300", Sku = "P2", Quantity = 5 }
};

var engine = new InventoryReservationEngine();
var result = engine.Process(commands);

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

Console.WriteLine(JsonSerializer.Serialize(result, options));
