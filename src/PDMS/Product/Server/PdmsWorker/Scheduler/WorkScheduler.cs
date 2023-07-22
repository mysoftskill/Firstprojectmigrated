namespace Microsoft.PrivacyServices.DataManagement.Worker.Scheduler
{
    using global::Autofac;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Generic WorkScheduler class.
    /// </summary>
    public class WorkScheduler
    {
        private readonly ILifetimeScope currentScope;
        private readonly WorkerType workerType;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        /// <param name="scope">The LifetimeScope.</param>
        /// <param name="workerType">The specific worker to schedule.</param>
        public WorkScheduler(ILifetimeScope scope, WorkerType workerType)
        {
            this.currentScope = scope;
            this.workerType = workerType;
        }

        /// <summary>
        /// Main scheduler function to start the flow.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The Task object for asynchronous execution.</returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = this.currentScope.BeginLifetimeScope(this.workerType))
                    {
                        var worker = scope.ResolveKeyed<IWorker>(this.workerType);

                        var callbackReason = await worker.DoWorkAsync(cancellationToken).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(callbackReason))
                        {
                           await Task.Delay(worker.IdleTimeBetweenCallsInMilliseconds, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
    }
}
