using System;
using System.Threading.Tasks;

namespace dotnet_outbox_pattern.Benchmarks.Benchmarks
{
    public static class MessagePublishingServiceBenchmarksExtensions
    {
        public static async Task WarmUp(this MessagePublishingServiceBenchmarks benchmarks)
        {
            await benchmarks.PublishAsync();
        }

        public static async Task ProcessMultiplePartitions(this MessagePublishingServiceBenchmarks benchmarks, int partitionCount)
        {
            for (int i = 0; i < partitionCount; i++)
            {
                await benchmarks.ProcessPartition_Batch100();
            }
        }

        public static async Task ProcessLargeBatch(this MessagePublishingServiceBenchmarks benchmarks, int messageCount)
        {
            for (int i = 0; i < messageCount; i++)
            {
                await benchmarks.ProcessPendingMessages_Batch100();
            }
        }
    }
}
