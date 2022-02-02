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