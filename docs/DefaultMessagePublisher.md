# DefaultMessagePublisher

`DefaultMessagePublisher` is a concrete implementation of `IMessagePublisher` that provides asynchronous message publishing capabilities with support for logging and integration with the outbox pattern. It serves as the default mechanism for dispatching messages in applications leveraging the outbox pattern to ensure reliable message delivery.

## API

### `DefaultMessagePublisher`

The default constructor initializes a new instance of the `DefaultMessagePublisher` class. This instance requires an `IOutboxRepository` to manage the outbox storage and an `IMessageSerializer` to serialize messages before persistence.

### `Task PublishAsync`
