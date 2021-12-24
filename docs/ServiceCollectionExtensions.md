# ServiceCollectionExtensions
The `ServiceCollectionExtensions` class provides a set of extension methods for the `IServiceCollection` interface, enabling the integration of the Outbox pattern into ASP.NET Core applications. These extensions allow for the configuration of message publishing and database initialization, facilitating the implementation of event-driven architectures.

## API
* `AddOutboxPattern`: Adds the Outbox pattern to the service collection, enabling the publishing of messages. This method returns the `IServiceCollection` instance, allowing for method chaining. It does not throw any exceptions.
* `AddMessagePublisher<TPublisher>`: Adds a message publisher of type `TPublisher` to the service collection. This method returns the `IServiceCollection` instance, allowing for method chaining. It does not throw any exceptions.
* `AddMessagePublisher`: Adds a message publisher to the service collection. This method returns the `IServiceCollection` instance, allowing for method chaining. It does not throw any exceptions.
* `InitializeDatabaseAsync`: Initializes the database asynchronously. This method returns a `Task` representing the asynchronous operation. It may throw exceptions if the database initialization fails.

## Usage
The following examples demonstrate how to use the `ServiceCollectionExtensions` class:
```csharp
// Example 1: Adding the Outbox pattern and a message publisher
public void ConfigureServices(IServiceCollection services)
{
    services.AddOutboxPattern();
    services.AddMessagePublisher<MyMessagePublisher>();
}

// Example 2: Initializing the database
public async Task InitializeDatabase()
{
    var services = new ServiceCollection();
    services.AddOutboxPattern();
    await ServiceCollectionExtensions.InitializeDatabaseAsync(services.BuildServiceProvider());
}
```

## Notes
When using the `ServiceCollectionExtensions` class, consider the following edge cases and thread-safety remarks:
* The `AddOutboxPattern` and `AddMessagePublisher` methods are thread-safe, as they only modify the service collection.
* The `InitializeDatabaseAsync` method is asynchronous and may throw exceptions if the database initialization fails. It is recommended to handle these exceptions accordingly.
* The `ServiceCollectionExtensions` class does not provide any inherent thread-safety guarantees for the message publishing process. It is the responsibility of the message publisher implementation to ensure thread-safety.
