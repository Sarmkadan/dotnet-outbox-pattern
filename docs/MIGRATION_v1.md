# Migration Guide: v0.x to v1.0

This document outlines the breaking changes introduced in version 1.0 of the `dotnet-outbox-pattern` library and provides a step-by-step guide to upgrade your existing v0.x implementations.

Version 1.0 brought significant improvements, including enhanced message ordering capabilities, better retry mechanisms, and improved configurability. These advancements required certain breaking changes to the database schema, dependency injection registration, and the core message contract.

## Breaking Changes

### 1. Database Schema Changes

The underlying database schema for outbox messages has been updated to support new features like partitioned ordering and additional metadata. Key changes include:

*   **Addition of `PartitionKey` and `SequenceNumber` fields:** These new columns are crucial for ensuring ordered message delivery within specific logical partitions.
    *   `PartitionKey` (NVARCHAR or similar, allowing NULL initially)
    *   `SequenceNumber` (BIGINT, allowing NULL initially)
*   **Renamed Columns:** Several columns were renamed for clarity and consistency. (Specific renames would require comparison with v0.x schema; users should refer to their v0.x schema for exact column names they may have used and map them to the v1.x schema.)

**Example SQL for Schema Migration (SQL Server, adapt for other databases):**

```sql
-- Add new columns if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OutboxMessages') AND name = 'PartitionKey')
BEGIN
    ALTER TABLE OutboxMessages ADD PartitionKey NVARCHAR(256) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OutboxMessages') AND name = 'SequenceNumber')
BEGIN
    ALTER TABLE OutboxMessages ADD SequenceNumber BIGINT NULL;
END;

-- Example of column rename (replace OldColumnName with actual v0.x name)
-- If you had a column named 'OldColumnName' in v0.x that maps to 'NewColumnName' in v1.x:
-- EXEC sp_rename 'OutboxMessages.OldColumnName', 'NewColumnName', 'COLUMN';

-- Consider populating PartitionKey and SequenceNumber for existing messages if ordering is required for them.
-- For example, you might derive PartitionKey from an existing AggregateId.
-- UPDATE OutboxMessages SET PartitionKey = AggregateId WHERE PartitionKey IS NULL;
-- UPDATE OutboxMessages SET SequenceNumber = <some_sequential_value> WHERE SequenceNumber IS NULL;
```

**Note:** After migration, you may want to ensure that all existing messages have a `PartitionKey` if you intend to use ordered processing for them. `SequenceNumber` might be generated on new messages or populated based on `CreatedAt` for historical data if strict ordering isn't paramount for old entries.

### 2. Dependency Injection (DI) Registration API Changes

The method for registering the outbox pattern in your `IServiceCollection` has changed for improved discoverability and consistency with common .NET practices.

*   **`UseOutbox()` has been replaced by `AddOutbox()`:**

**Before (v0.x - example):**

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
});

services.UseOutbox(); // Or similar method
```

**After (v1.x):**

```csharp
services.AddOutbox(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
    // Configure other outbox options
});
```
The `AddOutbox` method now takes a configuration action where you can specify the `DbContext` provider and other outbox-specific settings.

### 3. `IOutboxMessage` Interface Contract Changes

The `OutboxMessage` class (and implicitly the `IOutboxMessage` contract, if explicitly used) has been updated to include properties supporting the new features.

*   **New properties added to `OutboxMessage`:**
    *   `PartitionKey`: `string?` - Used to group messages for ordered processing.
    *   `DeliveryGuarantee`: `DeliveryGuarantee` (enum) - Specifies the delivery guarantee semantics (e.g., AtLeastOnce, ExactlyOnce).
    *   Other potential additions include fields for retry tracking (`PublishAttempts`, `MaxPublishAttempts`), locking (`IsLocked`, `LockExpiresAt`), and additional metadata. Review the `OutboxMessage.cs` source code for a complete list of properties.

**Action Required:** If you have custom implementations that directly interact with `IOutboxMessage` or custom data transfer objects (DTOs) that map to `OutboxMessage`, you will need to update them to reflect these new properties.

## Step-by-step Upgrade

1.  **Update NuGet Packages:** Update `DotnetOutboxPattern` and related packages to version `1.x.x` in your project.
    ```bash
    dotnet add package DotnetOutboxPattern --version 1.*
    # Update other related packages as needed
    ```
2.  **Run Database Migrations:** Apply the necessary schema changes to your database. You can use the example SQL provided above, or if you are using Entity Framework Core Migrations, ensure your migrations are up-to-date and apply them.
    ```bash
    # If using EF Core Migrations
    dotnet ef migrations add AddOutboxV1SchemaChanges
    dotnet ef database update
    ```
3.  **Update DI Registration:** Modify your `Startup.cs` (or `Program.cs` in .NET 6+) to use the new `AddOutbox()` method for registering the outbox services.
    ```csharp
    // Before (example)
    // services.UseOutbox();

    // After
    services.AddOutbox(options =>
    {
        // Configure your DbContext provider
        options.UseSqlServer(Configuration.GetConnectionString("OutboxConnection"));
        // Example: Enable dead letter processing
        options.EnableDeadLetterProcessing();
        // Example: Configure retry policy
        options.WithExponentialBackoffRetry(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));
        // ... other configurations
    });
    ```
4.  **Review Custom `OutboxMessage` Implementations:** If you have custom classes implementing `IOutboxMessage` or DTOs mapping to it, update them to include the new properties (`PartitionKey`, `DeliveryGuarantee`, etc.).
5.  **Test Thoroughly:** After making these changes, thoroughly test your application, paying close attention to message publishing, ordering, and retry behavior.

## Configuration Reference (v1.x)

Below are some common configuration options available in v1.x via the `AddOutbox` method:

| Option | Type | Default | Description |
|---|---|---|---|
| `UseSqlServer` / `UsePostgreSql` / `UseSqlite` | Method | (None) | Configures the database provider for the outbox. |
| `EnableDeadLetterProcessing` | Method | `true` | Enables/disables the dead letter queue processing. |
| `WithBatchSize` | `int` | `100` | Number of messages processed in a single batch. |
| `WithDelayBetweenBatches` | `TimeSpan` | `5s` | Delay between processing batches. |
| `WithInitialRetryDelay` | `TimeSpan` | `5s` | Initial delay before the first retry attempt. |
| `WithMaxRetryDelay` | `TimeSpan` | `5m` | Maximum delay between retry attempts. |
| `WithExponentialBackoffRetry` | Method | (N/A) | Configures exponential backoff retry policy. |
| `WithLinearBackoffRetry` | Method | (N/A) | Configures linear backoff retry policy. |
| `WithFixedIntervalRetry` | Method | (N/A) | Configures fixed interval retry policy. |
| `WithJitter` | `bool` | `true` | Adds random jitter to retry delays. |
| `WithPublishTimeout` | `TimeSpan` | `30s` | Timeout for publishing a single message to the external broker. |
| `PreservePartitionOrdering` | `bool` | `true` | Enables/disables sequential processing for partitioned messages. |
| `WithLockDuration` | `TimeSpan` | `5m` | Duration for which a message is locked during processing. |
| `EnableMessageArchival` | Method | `false` | Enables archival of old published messages. |
| `WithMessageArchivalFrequency` | `TimeSpan` | `1h` | How often to run the archival service. |
| `WithArchiveOlderThan` | `TimeSpan` | `30d` | Messages older than this will be archived. |
| `EnableCleanupOfArchivedMessages` | Method | `false` | Enables deletion of archived messages. |
| `WithCleanupFrequency` | `TimeSpan` | `24h` | How often to run the cleanup service. |
| `WithDeleteArchivedOlderThan` | `TimeSpan` | `90d` | Archived messages older than this will be deleted. |

This guide should help you transition your application to `dotnet-outbox-pattern` v1.0. If you encounter any issues, please refer to the project's documentation or open an issue on GitHub.
