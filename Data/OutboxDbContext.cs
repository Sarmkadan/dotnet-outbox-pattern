#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Data;

/// <summary>
/// Entity Framework Core DbContext for the outbox pattern
/// </summary>
public sealed class OutboxDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the DbSet for OutboxMessage entities.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    /// <summary>
    /// Gets or sets the DbSet for DeadLetter entities.
    /// </summary>
    public DbSet<DeadLetter> DeadLetters { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options)
    {
        ArgumentNullException.ThrowIfNull(options);
    }

    /// <summary>
    /// Configures the schema needed for the outbox pattern.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure OutboxMessage
        var outboxMessageBuilder = modelBuilder.Entity<OutboxMessage>();

        outboxMessageBuilder.HasKey(x => x.Id);
        outboxMessageBuilder.Property(x => x.Id).ValueGeneratedNever();

        outboxMessageBuilder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(256);

        outboxMessageBuilder.Property(x => x.AggregateId)
            .IsRequired()
            .HasMaxLength(256);

        outboxMessageBuilder.Property(x => x.AggregateType)
            .IsRequired()
            .HasMaxLength(128);

        outboxMessageBuilder.Property(x => x.EventData)
            .IsRequired();

        outboxMessageBuilder.Property(x => x.EventTypeName)
            .IsRequired()
            .HasMaxLength(256);

        outboxMessageBuilder.Property(x => x.Topic)
            .IsRequired()
            .HasMaxLength(128);

        outboxMessageBuilder.Property(x => x.State)
            .HasDefaultValue(OutboxMessageState.Pending);

        outboxMessageBuilder.Property(x => x.PartitionKey)
            .HasMaxLength(256);

        outboxMessageBuilder.Property(x => x.CorrelationId)
            .HasMaxLength(256);

        outboxMessageBuilder.Property(x => x.CausationId)
            .HasMaxLength(256);

        outboxMessageBuilder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        // Indexes for common queries
        outboxMessageBuilder.HasIndex(x => x.State)
            .HasDatabaseName("IX_OutboxMessage_State");

        outboxMessageBuilder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_OutboxMessage_IdempotencyKey");

        outboxMessageBuilder.HasIndex(x => x.AggregateId)
            .HasDatabaseName("IX_OutboxMessage_AggregateId");

        outboxMessageBuilder.HasIndex(x => x.Topic)
            .HasDatabaseName("IX_OutboxMessage_Topic");

        outboxMessageBuilder.HasIndex(x => new { x.State, x.CreatedAt })
            .HasDatabaseName("IX_OutboxMessage_State_CreatedAt");

        outboxMessageBuilder.HasIndex(x => new { x.State, x.ScheduledFor, x.IsLocked })
            .HasDatabaseName("IX_OutboxMessage_State_ScheduledFor_IsLocked");

        outboxMessageBuilder.HasIndex(x => x.PartitionKey)
            .HasDatabaseName("IX_OutboxMessage_PartitionKey");

        outboxMessageBuilder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_OutboxMessage_CorrelationId");

        // Configure DeadLetter
        var deadLetterBuilder = modelBuilder.Entity<DeadLetter>();

        deadLetterBuilder.HasKey(x => x.Id);
        deadLetterBuilder.Property(x => x.Id).ValueGeneratedNever();

        deadLetterBuilder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(256);

        deadLetterBuilder.Property(x => x.AggregateId)
            .IsRequired()
            .HasMaxLength(256);

        deadLetterBuilder.Property(x => x.AggregateType)
            .IsRequired()
            .HasMaxLength(128);

        deadLetterBuilder.Property(x => x.EventData)
            .IsRequired();

        deadLetterBuilder.Property(x => x.EventTypeName)
            .IsRequired()
            .HasMaxLength(256);

        deadLetterBuilder.Property(x => x.Topic)
            .IsRequired()
            .HasMaxLength(128);

        deadLetterBuilder.Property(x => x.ErrorMessage)
            .IsRequired()
            .HasMaxLength(2000);

        deadLetterBuilder.Property(x => x.CorrelationId)
            .HasMaxLength(256);

        deadLetterBuilder.Property(x => x.CausationId)
            .HasMaxLength(256);

        // Indexes for dead letter queries
        deadLetterBuilder.HasIndex(x => x.OutboxMessageId)
            .IsUnique()
            .HasDatabaseName("IX_DeadLetter_OutboxMessageId");

        deadLetterBuilder.HasIndex(x => x.IdempotencyKey)
            .HasDatabaseName("IX_DeadLetter_IdempotencyKey");

        deadLetterBuilder.HasIndex(x => x.IsReviewed)
            .HasDatabaseName("IX_DeadLetter_IsReviewed");

        deadLetterBuilder.HasIndex(x => x.MovedToDlqAt)
            .HasDatabaseName("IX_DeadLetter_MovedToDlqAt");

        deadLetterBuilder.HasIndex(x => x.AggregateId)
            .HasDatabaseName("IX_DeadLetter_AggregateId");

        deadLetterBuilder.HasIndex(x => x.Topic)
            .HasDatabaseName("IX_DeadLetter_Topic");
    }
}
