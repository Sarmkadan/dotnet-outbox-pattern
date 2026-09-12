# Domain enums

`Domain/Enums.cs` defines the enums used to describe outbox-message state, event type, delivery guarantee, and retry policy. Every member has an explicitly assigned integer value.

## `OutboxMessageState`

Represents the processing state of an outbox message.

| Member | Value | Meaning |
| --- | ---: | --- |
| `Pending` | `0` | The message is pending processing. |
| `Processing` | `1` | The message is currently being processed. |
| `Published` | `2` | The message has been published successfully. |
| `Failed` | `3` | Message processing failed and the message was moved to the dead-letter queue. |
| `Archived` | `4` | The message was archived manually. |

## `EventType`

Represents the type of event contained in an outbox message.

| Member | Value | Meaning |
| --- | ---: | --- |
| `Created` | `1` | An entity was created. |
| `Updated` | `2` | An entity was updated. |
| `Deleted` | `3` | An entity was deleted. |
| `Custom` | `4` | A custom business event. |
| `Notification` | `5` | A notification event. |

## `DeliveryGuarantee`

Represents a message delivery guarantee level.

| Member | Value | Meaning |
| --- | ---: | --- |
| `AtMostOnce` | `0` | At-most-once delivery: no retries are made, so the message may be lost. |
| `AtLeastOnce` | `1` | At-least-once delivery, with potential duplicate deliveries. |
| `ExactlyOnce` | `2` | Exactly-once delivery on a best-effort basis. |

## `RetryPolicyType`

Represents a retry policy.

| Member | Value | Meaning |
| --- | ---: | --- |
| `NoRetry` | `0` | No retry. |
| `FixedInterval` | `1` | Retries use a fixed interval. |
| `ExponentialBackoff` | `2` | Retries use exponential backoff. |
| `LinearBackoff` | `3` | Retries use linear backoff. |
