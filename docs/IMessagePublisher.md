# IMessagePublisher

The `IMessagePublisher` interface defines a contract for services responsible for processing and publishing outbox messages in the `dotnet-outbox-pattern` project. It provides methods to handle pending, scheduled, and partitioned messages, as well as releasing locks acquired during message processing. This interface is typically used in conjunction with a message broker or event-driven architecture to ensure reliable message delivery.

## API

### `MessagePublishingService`
