namespace PCF.UnitTests.Pdms
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;

    using Xunit;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Tests PrivacyCommand 
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class PrioritySemaphoreTests : INeedDataBuilders
    {
        [Fact]
        public async Task GlobalLimitsEnforced()
        {
            // Grab all of the tokens from the semaphore.
            int maxTokens = PrioritySemaphore.Instance.TotalTokenCount;

            // Leases are acquired from low priority pools to high priority pools in that order;
            // we use a stack to release them in the reverse order.
            Stack<IDisposable> leases = new Stack<IDisposable>();

            while (maxTokens > 0)
            {
                leases.Push(await PrioritySemaphore.Instance.WaitAsync(SemaphorePriority.RealTime));
                maxTokens--;
            }

            Assert.Equal(0, PrioritySemaphore.Instance.GetAvailableTokenCount(SemaphorePriority.Background));
            Assert.Equal(0, PrioritySemaphore.Instance.GetAvailableTokenCount(SemaphorePriority.Low));
            Assert.Equal(0, PrioritySemaphore.Instance.GetAvailableTokenCount(SemaphorePriority.Normal));
            Assert.Equal(0, PrioritySemaphore.Instance.GetAvailableTokenCount(SemaphorePriority.High));
            Assert.Equal(0, PrioritySemaphore.Instance.GetAvailableTokenCount(SemaphorePriority.RealTime));

            // Good news, we own all the tokens! Let's try to grab another one.
            Task<IDisposable> realTimeWaitTask = PrioritySemaphore.Instance.WaitAsync(SemaphorePriority.RealTime);
            Task<IDisposable> backgroundWaitTask = PrioritySemaphore.Instance.WaitAsync(SemaphorePriority.Background);

            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.False(realTimeWaitTask.IsCompleted);
            Assert.False(backgroundWaitTask.IsCompleted);

            // Release a lock. This will free up the above task to pull from the "realtime" pool. 
            leases.Pop().Dispose();
            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.True(realTimeWaitTask.IsCompleted);
            Assert.False(backgroundWaitTask.IsCompleted);

            while (leases.Count > 0)
            {
                leases.Pop().Dispose();
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.True(backgroundWaitTask.IsCompleted);

            realTimeWaitTask.Result.Dispose();
            backgroundWaitTask.Result.Dispose();
        }
    }
}
