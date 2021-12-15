# DeadLetterHandlingExample
The `DeadLetterHandlingExample` type is designed to demonstrate and facilitate the handling of dead letters in a message processing system. It provides a set of methods for monitoring, investigating, reviewing, and requeueing dead letters, as well as attempting automatic recovery and performing health checks.

## API
* `public DeadLetterMonitor`: A property that provides access to a `DeadLetterMonitor` instance, allowing for monitoring of dead letters.
* `public async Task MonitorDeadLettersAsync`: An asynchronous method that monitors dead letters. It does not take any parameters and does not return a value. It may throw exceptions if errors occur during monitoring.
* `public async Task<string> InvestigateDeadLetterAsync`: An asynchronous method that investigates a dead letter. It does not take any parameters and returns a string containing the investigation result. It may throw exceptions if errors occur during investigation.
* `public async Task ReviewDeadLetterAsync`: An asynchronous method that reviews a dead letter. It does not take any parameters and does not return a value. It may throw exceptions if errors occur during review.
* `public async Task RequeueDeadLetterAsync`: An asynchronous method that requeues a dead letter. It does not take any parameters and does not return a value. It may throw exceptions if errors occur during requeueing.
* `public AutomatedDeadLetterRecovery`: A property that provides access to an `AutomatedDeadLetterRecovery` instance, allowing for automated recovery of dead letters.
* `public async Task AttemptAutomaticRecoveryAsync`: An asynchronous method that attempts to automatically recover a dead letter. It does not take any parameters and does not return a value. It may throw exceptions if errors occur during recovery.
* `public DeadLetterHealthCheck`: A property that provides access to a `DeadLetterHealthCheck` instance, allowing for health checks of dead letters.
* `public async Task<(bool isHealthy, string message)> CheckHealthAsync`: An asynchronous method that checks the health of a dead letter. It does not take any parameters and returns a tuple containing a boolean indicating whether the dead letter is healthy and a string containing a health message. It may throw exceptions if errors occur during the health check.
* `public static async Task Main`: A static asynchronous method that serves as the entry point for the `DeadLetterHandlingExample` type. It does not take any parameters and does not return a value.

## Usage
The following example demonstrates how to use the `DeadLetterHandlingExample` type to monitor and investigate dead letters:
```csharp
var deadLetterHandlingExample = new DeadLetterHandlingExample();
await deadLetterHandlingExample.MonitorDeadLettersAsync();
var investigationResult = await deadLetterHandlingExample.InvestigateDeadLetterAsync();
Console.WriteLine(investigationResult);
```
The following example demonstrates how to use the `DeadLetterHandlingExample` type to attempt automatic recovery of a dead letter:
```csharp
var deadLetterHandlingExample = new DeadLetterHandlingExample();
await deadLetterHandlingExample.AttemptAutomaticRecoveryAsync();
var healthCheckResult = await deadLetterHandlingExample.CheckHealthAsync();
Console.WriteLine($"Is healthy: {healthCheckResult.isHealthy}, Message: {healthCheckResult.message}");
```

## Notes
When using the `DeadLetterHandlingExample` type, it is essential to consider the following edge cases and thread-safety remarks:
* The `MonitorDeadLettersAsync` method may throw exceptions if errors occur during monitoring. It is recommended to handle these exceptions accordingly.
* The `InvestigateDeadLetterAsync` method may return a null or empty string if the investigation result is not available. It is recommended to check for these cases before processing the result.
* The `AttemptAutomaticRecoveryAsync` method may throw exceptions if errors occur during recovery. It is recommended to handle these exceptions accordingly.
* The `CheckHealthAsync` method may return a tuple with a false `isHealthy` value and an empty `message` string if the health check fails. It is recommended to check for these cases before processing the result.
* The `DeadLetterHandlingExample` type is designed to be thread-safe, allowing for concurrent access to its methods and properties. However, it is still essential to ensure that the underlying message processing system is also thread-safe to avoid any potential issues.
