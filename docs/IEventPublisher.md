# IEventPublisher

The `IEventPublisher` interface provides an abstraction for publishing and subscribing to domain events in a reliable, outbox-pattern-based messaging system. It ensures that events are persisted before being published, allowing for retry mechanisms in case of failures. The interface supports both publishing events asynchronously and subscribing to them with automatic retries and error handling.

## API

### `EventPublisher`
