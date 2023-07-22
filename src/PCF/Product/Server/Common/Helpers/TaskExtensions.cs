namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines some handy dandy Task extensions.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Throws a TimeoutException if the task's execution exceeds the given timeout.
        /// </summary>
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            CancellationTokenSource timeoutCancellationToken = new CancellationTokenSource();
            await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationToken.Token));

            if (task.IsCompleted)
            {
                // Ensure exceptions bubble as they should by awaiting. Since the task has completed already,
                // this will be a synchronous call.
                timeoutCancellationToken.Cancel();
                return await task;
            }
            else
            {
                throw new TimeoutException("Timeout while waiting on task.");
            }
        }

        /// <summary>
        /// Throws a TimeoutException if the task's execution exceeds the given timeout.
        /// </summary>
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            CancellationTokenSource timeoutCancellationToken = new CancellationTokenSource();
            await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationToken.Token));

            if (task.IsCompleted)
            {
                // Ensure exceptions bubble as they should by awaiting. Since the task has completed already,
                // this will be a synchronous call.
                timeoutCancellationToken.Cancel();
                await task;
            }
            else
            {
                throw new TimeoutException("Timeout while waiting on task.");
            }
        }
    }
}
