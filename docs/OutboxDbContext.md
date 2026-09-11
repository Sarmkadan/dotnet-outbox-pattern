# OutboxDbContext

Entity Framework Core DbContext for the outbox pattern implementation.

## Entities

### OutboxMessage
Represents a message in the outbox table waiting to be processed.

### DeadLetter
Represents a message that failed processing and was moved to the dead letter queue.

## DbSets

| DbSet | Description |
|-------|-------------|
| `OutboxMessages` | Collection of outbox messages |
| `DeadLetters` | Collection of dead letter messages |

## Configuration Conventions

### OutboxMessage Configuration

#### Primary Key
- `Id`: Value generated never (application-assigned GUID)

#### Properties
| Property | Configuration |
|----------|---------------|
| `IdempotencyKey` | Required, max length 256 |
| `AggregateId` | Required, max length 256 |
| `AggregateType` | Required, max length 128 |
| `EventData` | Required |
| `EventTypeName` | Required, max length 256 |
| `Topic` | Required, max length 128 |
| `State` | Default value: `OutboxMessageState.Pending` |
| `PartitionKey` | Max length 256 |
| `CorrelationId` | Max length 256 |
| `CausationId` | Max length 256 |
| `ErrorMessage` | Max length 2000 |

#### Indexes
| Index Name | Columns | Unique |
|------------|---------|--------|
| `IX_OutboxMessage_State` | State | No |
| `IX_OutboxMessage_IdempotencyKey` | IdempotencyKey | Yes |
| `IX_OutboxMessage_AggregateId` | AggregateId | No |
| `IX_OutboxMessage_Topic` | Topic | No |
| `IX_OutboxMessage_State_CreatedAt` | State, CreatedAt | No |
| `IX_OutboxMessage_State_ScheduledFor_IsLocked` | State, ScheduledFor, IsLocked | No |
| `IX_OutboxMessage_PartitionKey` | PartitionKey | No |
| `IX_OutboxMessage_CorrelationId` | CorrelationId | No |

### DeadLetter Configuration

#### Primary Key
- `Id`: Value generated never (application-assigned GUID)

#### Properties
| Property | Configuration |
|----------|---------------|
| `IdempotencyKey` | Required, max length 256 |
| `AggregateId` | Required, max length 256 |
| `AggregateType` | Required, max length 128 |
| `EventData` | Required |
| `EventTypeName` | Required, max length 256 |
| `Topic` | Required, max length 128 |
| `ErrorMessage` | Required, max length 2000 |
| `CorrelationId` | Max length 256 |
| `CausationId` | Max length 256 |

#### Indexes
| Index Name | Columns | Unique |
|------------|---------|--------|
| `IX_DeadLetter_OutboxMessageId` | OutboxMessageId | Yes |
| `IX_DeadLetter_IdempotencyKey` | IdempotencyKey | No |
| `IX_DeadLetter_IsReviewed` | IsReviewed | No |
| `IX_DeadLetter_MovedToDlqAt` | MovedToDlqAt | No |
| `IX_DeadLetter_AggregateId` | AggregateId | No |
| `IX_DeadLetter_Topic` | Topic | No |

## Usage

The context is configured via dependency injection with `DbContextOptions<OutboxDbContext>`.

```csharp
services.AddDbContext<OutboxDbContext>(options =>
    options.UseSqlServer(connectionString));
```