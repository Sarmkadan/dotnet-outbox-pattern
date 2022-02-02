// existing content ...

## DefaultMessagePublisher

The `DefaultMessagePublisher` class provides a basic implementation of `IMessagePublisher` for testing and demo purposes. It logs messages instead of publishing them to an actual message broker. You can also create a logging publisher using the `CreateLoggingPublisher` method.

### Usage Example

```csharp
using DotnetOutboxPattern.Infrastructure;
using Microsoft.Extensions.Logging;

public class DefaultMessagePublisherExample
{
    public void RunExample()
    {
        // Create a logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<DefaultMessagePublisher>();

        // Create a default message publisher
        var publisher = new DefaultMessagePublisher(logger);

        // Create a logging publisher
        var loggingPublisher = MessagePublisherFactory.CreateLoggingPublisher(logger);

        // Publish a message using default publisher
        var message = new OutboxMessage { /* initialize message properties */ };
        _ = publisher.PublishAsync(message);

        // Publish a message using logging publisher
        _ = loggingPublisher.PublishAsync(message);
    }
}
```

## OutboxProcessorOptions

The `OutboxProcessorOptions` class provides configuration for the background outbox message processor. It controls batch processing behavior, locking mechanics, and health monitoring thresholds.


### Usage Example

```csharp
using DotnetOutboxPattern.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class ConfigureOutboxProcessorExample
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHostedService<OutboxProcessor>();

        services.Configure<OutboxProcessorOptions>(options =>
        {
            options.Enabled = true;
            options.BatchSize = 200;
            options.DelayBetweenBatches = 10000;
            options.CheckExpiredLocksInterval = 300000;
            options.LockDurationSeconds = 600;
            options.PreservePartitionOrdering = true;
            options.OldestMessageAgeThresholdMinutes = 10;
        });

        // Register options interface
        services.AddSingleton<IOutboxProcessorOptions>(sp =>
            sp.GetRequiredService<IOptions<OutboxProcessorOptions>>().Value);
    }

    public void UseProcessor(IHost host)
    {
        var processor = host.Services.GetRequiredService<OutboxProcessor>();
        var health = processor.GetHealth();

        Console.WriteLine($"IsHealthy: {health.IsHealthy}");
        Console.WriteLine($"LastSuccessfulPublish: {health.LastSuccessfulPublish}");
    }
}
```