// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Example 3: Dead Letter Queue Handling
///
/// Demonstrates how to:
/// - Monitor the dead letter queue
/// - Retrieve and review failed messages
/// - Requeue messages for retry
/// - Implement automated recovery strategies
/// </summary>

namespace Examples
{
    public class DeadLetterHandlingExample
    {
        public class DeadLetterMonitor
        {
            private readonly IDeadLetterService _deadLetterService;
            private readonly ILogger<DeadLetterMonitor> _logger;

            public DeadLetterMonitor(IDeadLetterService deadLetterService, ILogger<DeadLetterMonitor> logger)
            {
                _deadLetterService = deadLetterService;
                _logger = logger;
            }

            /// <summary>
            /// Periodically checks for unreviewed dead letters and logs them.
            /// Can be called from a background job or health check endpoint.
            /// </summary>
            public async Task MonitorDeadLettersAsync()
            {
                var unreviewed = await _deadLetterService.GetUnreviewedAsync();

                if (unreviewed.Count == 0)
                {
                    _logger.LogInformation("No unreviewed dead letters");
                    return;
                }

                _logger.LogWarning("Found {Count} unreviewed dead letters", unreviewed.Count);

                foreach (var deadLetter in unreviewed)
                {
                    _logger.LogWarning(
                        "Dead Letter {Id}: Error={Error}, Failures={FailureCount}",
                        deadLetter.Id, deadLetter.ErrorMessage, deadLetter.TotalAttempts);
                }
            }

            /// <summary>
            /// Retrieves detailed information about a specific dead letter for investigation.
            /// </summary>
            public async Task<string> InvestigateDeadLetterAsync(Guid deadLetterId)
            {
                var deadLetter = await _deadLetterService.GetAsync(deadLetterId);

                if (deadLetter == null)
                    return "Dead letter not found";

                var details = $@"
Dead Letter Details:
  ID: {deadLetter.Id}
  Moved to DLQ: {deadLetter.MovedToDlqAt:O}
  Original Message ID: {deadLetter.OutboxMessageId}

Message Content:
  {deadLetter.EventData}

Error Information:
  Last Error: {deadLetter.ErrorMessage}
  Failure Count: {deadLetter.TotalAttempts}

Review Status:
  Reviewed: {(deadLetter.ReviewedAt.HasValue ? "Yes" : "No")}
  Reviewed At: {deadLetter.ReviewedAt:O}
  Reviewed By: {""}
  Notes: {deadLetter.ReviewNotes}
";
                return details;
            }

            /// <summary>
            /// Reviews a dead letter and documents the findings.
            /// This marks it as reviewed but doesn't retry automatically.
            /// </summary>
            public async Task ReviewDeadLetterAsync(
                Guid deadLetterId,
                string reason)
            {
                _logger.LogInformation(
                    "Reviewing dead letter {Id}: {Reason}",
                    deadLetterId, reason);

                await _deadLetterService.ReviewAsync(deadLetterId, reason);

                _logger.LogInformation("Dead letter {Id} marked as reviewed", deadLetterId);
            }

            /// <summary>
            /// Requeues a dead letter for retry after fixing the underlying issue.
            /// </summary>
            public async Task RequeueDeadLetterAsync(
                Guid deadLetterId,
                string fixDescription)
            {
                _logger.LogInformation(
                    "Requeuing dead letter {Id}: {Fix}",
                    deadLetterId, fixDescription);

                await _deadLetterService.RequeueAsync(
                    deadLetterId: deadLetterId,
                    reason: fixDescription);

                _logger.LogInformation(
                    "Dead letter {Id} requeued for retry",
                    deadLetterId);
            }
        }

        /// <summary>
        /// Automated recovery strategies for common failure patterns.
        /// </summary>
        public class AutomatedDeadLetterRecovery
        {
            private readonly IDeadLetterService _dlService;
            private readonly ILogger<AutomatedDeadLetterRecovery> _logger;

            public AutomatedDeadLetterRecovery(
                IDeadLetterService dlService,
                ILogger<AutomatedDeadLetterRecovery> logger)
            {
                _dlService = dlService;
                _logger = logger;
            }

            /// <summary>
            /// Automatically recovers messages with known transient errors.
            /// Should be called periodically (e.g., every 5 minutes).
            /// </summary>
            public async Task AttemptAutomaticRecoveryAsync()
            {
                var unreviewed = await _dlService.GetUnreviewedAsync();

                foreach (var deadLetter in unreviewed)
                {
                    // Example: Retry connection errors after 10 minutes
                    if (IsTransientError(deadLetter.ErrorMessage))
                    {
                        var timeSinceDlq = DateTime.UtcNow - deadLetter.OriginalCreatedAt;
                        if (timeSinceDlq.TotalMinutes > 10)
                        {
                            _logger.LogInformation(
                                "Auto-recovering transient error for {Id}",
                                deadLetter.Id);

                            await _dlService.RequeueAsync(
                                deadLetter.Id,
                                "Automatic recovery: Transient error resolved");
                        }
                    }

                    // Example: Skip rate-limited messages for later
                    if (IsRateLimitError(deadLetter.ErrorMessage))
                    {
                        _logger.LogInformation(
                            "Skipping rate-limited message {Id}, will retry later",
                            deadLetter.Id);
                        // Don't requeue yet - service is still rate limiting
                    }
                }
            }

            /// <summary>
            /// Checks if an error is likely transient (might succeed on retry).
            /// </summary>
            private bool IsTransientError(string? error)
            {
                if (string.IsNullOrEmpty(error))
                    return false;

                var transientPatterns = new[]
                {
                    "timeout",
                    "connection refused",
                    "connection reset",
                    "503",  // Service unavailable
                    "429",  // Too many requests
                };

                return transientPatterns.Any(p =>
                    error.Contains(p, StringComparison.OrdinalIgnoreCase));
            }

            /// <summary>
            /// Checks if an error is due to rate limiting.
            /// </summary>
            private bool IsRateLimitError(string? error)
            {
                if (string.IsNullOrEmpty(error))
                    return false;

                return error.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                       error.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                       error.Contains("too many requests", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Health check that can be used for monitoring/alerting.
        /// </summary>
        public class DeadLetterHealthCheck
        {
            private readonly IDeadLetterService _dlService;

            public DeadLetterHealthCheck(IDeadLetterService dlService)
            {
                _dlService = dlService;
            }

            public async Task<(bool isHealthy, string message)> CheckHealthAsync()
            {
                var unreviewed = await _dlService.GetUnreviewedAsync();

                // Alert if more than 100 unreviewed messages
                if (unreviewed.Count > 100)
                {
                    return (false, $"Too many unreviewed dead letters: {unreviewed.Count}");
                }

                // Alert if any message has been unreviewed for more than 24 hours
                var oldestUnreviewed = unreviewed
                    .OrderBy(dl => dl.MovedToDlqAt)
                    .FirstOrDefault();

                if (oldestUnreviewed != null)
                {
                    var age = DateTime.UtcNow - oldestUnreviewed.OriginalCreatedAt;
                    if (age.TotalHours > 24)
                    {
                        return (false, $"Dead letter unreviewed for {age.TotalHours:F1} hours");
                    }
                }

                return (true, $"DLQ healthy: {unreviewed.Count} unreviewed");
            }
        }

        public static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddLogging();

            // Setup:
            // services.AddOutboxPattern(connectionString);
            // services.AddScoped<DeadLetterMonitor>();
            // services.AddScoped<AutomatedDeadLetterRecovery>();
            // services.AddScoped<DeadLetterHealthCheck>();

            Console.WriteLine("Example: Dead Letter Queue Handling");
            Console.WriteLine("Features:");
            Console.WriteLine("  - Monitor unreviewed dead letters");
            Console.WriteLine("  - Investigate failed messages");
            Console.WriteLine("  - Manual review and requeue");
            Console.WriteLine("  - Automated recovery for transient errors");

            await Task.CompletedTask;
        }
    }
}
