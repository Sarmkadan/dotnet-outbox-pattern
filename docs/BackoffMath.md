# BackoffMath

## Purpose

`Infrastructure/BackoffMath.cs` centralizes the exponential backoff calculation used by `RetryHelper`, `OutboxBackoffExtensions`, and `OutboxRetryOptions`. It returns a delay in milliseconds as a `double`, caps exponential growth, and limits the result to a caller-supplied maximum delay.

## Backoff Formula

`ComputeExponentialDelay` accepts a base delay `b`, maximum delay `dMax`, multiplier `m`, and attempt number `a`.

After validating its arguments, the method normalizes the inputs and caps the exponent at 32:

```text
b = max(0, baseDelayMs)
m = max(1.0, multiplier)
a = max(0, attempt)
e = min(a, 32)
```

It then calculates the scaled delay and clamps it to the configured maximum:

```text
scaledDelay = b * m^e
ceiling = max(maxDelayMs, b)
delay = min(scaledDelay, ceiling)
```

Because validation already requires non-negative values, a multiplier of at least `1.0`, and a maximum no smaller than the base delay, the normalization does not alter valid ordinary inputs; `ceiling` is their supplied `maxDelayMs`.

Attempt zero uses an exponent of zero and therefore returns the base delay. Retry callers normally pass attempts starting at one, so the first retry is `b * m^1`. Attempts greater than 32 use the same exponent as attempt 32. The returned value never exceeds `dMax`.

For example, with a 100 ms base delay, a multiplier of 2, and a 1,000 ms maximum, attempts 0 through 5 produce 100, 200, 400, 800, 1,000, and 1,000 ms.

The method rejects invalid arguments with `ArgumentOutOfRangeException`:

- `baseDelayMs` must be zero or greater.
- `maxDelayMs` must be greater than or equal to `baseDelayMs`.
- `multiplier` must be at least `1.0`.
- `attempt` must be zero or greater.

## Usage Example

```csharp
using DotnetOutboxPattern.Infrastructure;

const int baseDelayMs = 100;
const int maxDelayMs = 1_000;
const double multiplier = 2.0;

for (var attempt = 1; attempt <= 4; attempt++)
{
    var delayMs = BackoffMath.ComputeExponentialDelay(
        baseDelayMs,
        maxDelayMs,
        multiplier,
        attempt);

    await Task.Delay(TimeSpan.FromMilliseconds(delayMs));
}
```

For these inputs, the four calls calculate delays of 200, 400, 800, and 1,000 ms. Further attempts remain at the 1,000 ms maximum.
