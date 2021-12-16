# IDeadLetterService
The `IDeadLetterService` interface provides a set of methods for managing dead letters, which are messages that cannot be processed by a message handler. It allows for moving messages to a dead letter queue, retrieving dead letters, reviewing and requeueing them, and monitoring the health of the dead letter service.

## API
* `DeadLetterService`: The constructor for the `DeadLetterService` class, which implements this interface.
* `MoveToDlqAsync`: Moves a message to the dead letter queue. This method is asynchronous and returns a `DeadLetter` object representing the moved message. It may throw exceptions if the move operation fails.
* `GetAsync`: Retrieves a dead letter by its identifier. This method is asynchronous and returns a `DeadLetter` object if the dead letter exists, or `null` if it does not. It may throw exceptions if the retrieval operation fails.
* `GetUnreviewedAsync`: Retrieves a list of unreviewed dead letters. This method is asynchronous and returns a list of `DeadLetter` objects. It may throw exceptions if the retrieval operation fails.
* `ReviewAsync`: Marks a dead letter as reviewed. This method is asynchronous and does not return a value. It may throw exceptions if the review operation fails.
* `RequeueAsync`: Requeues a dead letter, moving it back to the original queue. This method is asynchronous and does not return a value. It may throw exceptions if the requeue operation fails.
* `GetUnreviewedCountAsync`: Retrieves the number of unreviewed dead letters. This method is asynchronous and returns an integer count. It may throw exceptions if the retrieval operation fails.
* `GetByTopicAsync`: Retrieves a list of dead letters by topic. This method is asynchronous and returns a list of `DeadLetter` objects. It may throw exceptions if the retrieval operation fails.
* `DeleteAsync`: Deletes a dead letter. This method is asynchronous and does not return a value. It may throw exceptions if the deletion operation fails.
* `GetHealthAsync`: Retrieves health metrics for the dead letter service. This method is asynchronous and returns a `HealthMetrics` object. It may throw exceptions if the retrieval operation fails.

## Usage
The following example demonstrates how to use the `IDeadLetterService` to move a message to the dead letter queue and then retrieve it:
```csharp
var deadLetterService = new DeadLetterService();
var deadLetter = await deadLetterService.MoveToDlqAsync("message-id");
var retrievedDeadLetter = await deadLetterService.GetAsync(deadLetter.Id);
```
The following example demonstrates how to use the `IDeadLetterService` to review and requeue a dead letter:
```csharp
var deadLetterService = new DeadLetterService();
var unreviewedDeadLetters = await deadLetterService.GetUnreviewedAsync();
foreach (var deadLetter in unreviewedDeadLetters)
{
    await deadLetterService.ReviewAsync(deadLetter.Id);
    await deadLetterService.RequeueAsync(deadLetter.Id);
}
```

## Notes
The `IDeadLetterService` interface is designed to be thread-safe, allowing multiple threads to access and manipulate dead letters concurrently. However, it is still possible for concurrent modifications to result in inconsistent state, and care should be taken to ensure that dead letters are not modified simultaneously by multiple threads. Additionally, the `GetUnreviewedCountAsync` method may return a stale count if the number of unreviewed dead letters changes between the time the count is retrieved and the time it is used. It is recommended to use this method as a rough estimate rather than a precise count.
