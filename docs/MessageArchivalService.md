# MessageArchivalService

Central service responsible for archiving processed messages from the outbox table once they exceed a configurable age threshold. It periodically scans the outbox for eligible messages, batches them for efficiency, and delegates the actual archival logic to an injected archiver component. The service ensures idempotent operation by tracking archival progress via the configured interval and batch size.

## API

### `MessageArchivalService`

Initializes a new instance of the archival service.
