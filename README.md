# Inventory Reservation

A focused .NET 8 console solution for an in-memory inventory reservation engine.

## Projects

- `InventoryReservation`: console application and reservation engine
- `InventoryReservation.Tests`: xUnit behavioral tests for the public `Process` method

## Run the example

```bash
dotnet run --project InventoryReservation
```

The console app runs the provided sample commands and prints the final result as formatted JSON.

## Run tests

```bash
dotnet test
```

## Design Notes

- `InventoryReservationEngine.Process` starts from an empty state on every call.
- Commands are processed sequentially.
- Duplicate nonblank command IDs are tracked from their first occurrence, even if that first command fails.
- Identifier comparisons use ordinal, case-sensitive comparison.
- Command `type` matching is case-insensitive.
- Reservations are tracked per SKU and order ID, not only as an aggregate count.
- Failed commands do not mutate state.
