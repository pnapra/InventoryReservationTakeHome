# Inventory Reservation

## Overview

This solution is a focused .NET 8 inventory reservation engine. It processes inventory commands sequentially in memory and returns final SKU summaries along with command IDs that failed.

The project intentionally avoids a database, API, UI, and unnecessary infrastructure so the business rules are easy to read, test, and discuss.

## Project Structure

- `InventoryReservation`: console application that demonstrates the provided example input and prints formatted JSON.
- `InventoryReservationEngine`: service class containing the command validation and inventory business logic.
- `SkuInventoryState`: internal mutable state model for one SKU, including per-order reservation quantities.
- `InventoryReservation.Tests`: xUnit test project covering behavior through the public `Process` method.

## How To Run

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project InventoryReservation
```

The console application runs the exercise's example input and prints the final result as formatted JSON.

## Assumptions

- Shipping decreases `onHand`.
- Shipping decreases `reserved`.
- Shipping increases `shipped`.
- Releasing inventory decreases `reserved` but does not change `onHand`.
- `available` is calculated as `onHand - reserved`.
- Command types are treated case-insensitively.
- Command IDs, SKUs, and order IDs are treated as case-sensitive using ordinal comparison.
- The first nonblank occurrence of a command ID consumes that ID even if the command fails another validation rule.
- A later occurrence of that command ID is therefore treated as a duplicate.
- Null or blank command IDs are not added to the duplicate-tracking set.
- An invalid command with a missing command ID preserves its supplied null or blank value in `failedCommands`.
- Each call to `Process` begins with empty inventory state.
- Identifiers are checked for null, empty, or whitespace values but are otherwise not trimmed or normalized.
- Commands are processed strictly in the order received.

## Design Choices And Tradeoffs

- Reservations are tracked by both SKU and order ID because release and ship must validate the reservation belonging to the specific order.
- An aggregate reserved quantity alone would not be sufficient.
- `available` is calculated instead of independently stored so it cannot become inconsistent with `onHand` and `reserved`.
- Mutable inventory state is kept internal to command processing.
- Failed commands are validated before state changes so they do not partially mutate inventory.
- A `HashSet` is used for processed command IDs.
- Dictionaries are used for efficient SKU and order reservation lookups.
- A console application was chosen because the exercise does not require a UI, API, or database.
- The solution intentionally favors clarity and correctness over production infrastructure.

## Production Considerations

### Concurrent Commands

The current implementation assumes sequential processing. In production, possible approaches include optimistic concurrency with a version or row-version field, transactional inventory updates, or serializing commands by SKU through a partitioned queue. A single process-wide lock should be avoided where possible because it can limit throughput as command volume grows.

### Persistence

Inventory, reservations, and processed command IDs should be stored transactionally. A unique database constraint on `commandId` should enforce idempotency so duplicate commands cannot be applied twice.

### API Exposure

The engine could be wrapped by a thin authenticated API endpoint while keeping the business rules inside the engine or an application service.

### Audit History And Replay

Received commands and their outcomes could be stored as immutable audit records. Current inventory could be rebuilt or verified by replaying the ordered command history.

## Test Coverage

The unit tests cover:

- Provided example
- Valid add, reserve, release, and ship operations
- Insufficient inventory
- Order-specific reservation validation
- Invalid quantities and identifiers
- Unknown command types
- Case-insensitive command types
- Duplicate command IDs
- Failed commands not mutating state
- Multiple SKUs and orders
- Summary sorting
- Failed-command processing order
- Null and empty input
