# OutboxRetryOptions

`Domain/OutboxRetryOptions.cs` defines the retry-with-backoff policy used after an outbox publish failure. `MessagePublishingService` uses it to decide whether a failed message is exhausted and, if not, when the message becomes eligible for another attempt. `DeadLetterService` also uses `MaxAttempts` when resetting a requeued message.

## Settable properties and defaults

| Property | Default | Accepted values | Effect |
| --- | --- | --- | --- |
| `MaxAttempts` | `OutboxConstants.DefaultMaxPublishAttempts` (`5`) | `1` through `20`, inclusive | Sets the policy attempt ceiling. On failure, `MessagePublishingService` uses the lower of this value and the message's `MaxPublishAttempts`. A message at that ceiling is dead-lettered instead of scheduled again. Requeuing a dead letter resets its `MaxPublishAttempts` to this value. |
| `BackoffStrategy` | `RetryStrategy.ExponentialBackoff` | Any `RetryStrategy` value; the setter performs no validation | Selects the delay calculation used by `ComputeNextDelay`. An unrecognized enum value falls back to `InitialDelay`. |
| `InitialDelay` | `TimeSpan.FromSeconds(1)` | Zero through 30 seconds, inclusive | Supplies the fixed delay, the starting value for linear backoff, and the base delay for exponential and jittered backoff. |
| `MaxDelay` | `TimeSpan.FromMinutes(5)` | Assignments accept zero through 30 seconds, inclusive | Caps the delay returned by `ComputeNextDelay`. The backing field initializes directly to five minutes; consequently, the default is five minutes even though assigning a value above 30 seconds throws. |
| `BackoffMultiplier` | `2.0` | `1.0` through `10.0`, inclusive | Controls growth for exponential and jittered backoff. It has no effect on fixed, linear, or no-retry calculations. |
| `LinearIncrement` | `TimeSpan.FromSeconds(1)` | Zero through 30 seconds, inclusive | Adds this amount for each attempt after the first when using linear backoff. It has no effect on the other strategies. |

Invalid assignments to the range-checked properties throw `ArgumentOutOfRangeException`. The type does not enforce a relationship between `InitialDelay` and `MaxDelay`; for exponential or jittered backoff, the shared exponential calculation throws `ArgumentOutOfRangeException` if the maximum is less than the initial delay.

## Delay calculation

`ComputeNextDelay(int attemptNumber)` accepts a 1-based number from `1` through `20`. In the publishing service this is the message's `PublishAttempts` value after the failure has been recorded. Values outside that range throw `ArgumentOutOfRangeException`.

For attempt number `n`, the selected strategy computes:

| Strategy | Computed delay before the final cap |
| --- | --- |
| `NoRetry` | Zero |
| `FixedDelay` | `InitialDelay` |
| `LinearBackoff` | `InitialDelay + LinearIncrement * (n - 1)` |
| `ExponentialBackoff` | `InitialDelay * BackoffMultiplier^n`, capped at `MaxDelay` by `BackoffMath` |
| `JitteredBackoff` | The capped exponential delay plus a random value from zero up to that base delay |
| Unrecognized enum value | `InitialDelay` |

The method applies `MaxDelay` once more to the selected result before returning it. Thus the first exponential delay uses the multiplier once: with the defaults, failed attempt 1 produces 2 seconds, attempt 2 produces 4 seconds, and so on, up to the five-minute default cap. A jittered result is also capped, so it never exceeds `MaxDelay`.

`NoRetry` controls only the delay calculation: it returns a zero delay. It does not bypass the attempt-ceiling logic, so the publishing service can schedule another attempt immediately until the effective attempt ceiling is reached.

## Example

```csharp
var retryOptions = new OutboxRetryOptions
{
    MaxAttempts = 4,
    BackoffStrategy = RetryStrategy.LinearBackoff,
    InitialDelay = TimeSpan.FromSeconds(2),
    LinearIncrement = TimeSpan.FromSeconds(3),
    MaxDelay = TimeSpan.FromSeconds(20)
};

var delayAfterFirstFailure = retryOptions.ComputeNextDelay(1); // 2 seconds
var delayAfterThirdFailure = retryOptions.ComputeNextDelay(3); // 8 seconds
```
