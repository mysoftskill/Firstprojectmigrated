using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Osgs.Core.Extensions;
using Microsoft.Osgs.Infra.Cache;
using Microsoft.Osgs.Infra.Cache.Tracking;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.Osgs.Infra.Cache.Utilities;
using Microsoft.PrivacyServices.UX.Monitoring.Events;

namespace Microsoft.PrivacyServices.UX.Core.Cache
{
    /// <summary>
    /// Cache tracking for PCD.
    /// </summary>
    public class SimpleCacheTracker : ICacheTracking
    {
        private static readonly TraceSource trace = new TraceSource(nameof(SimpleCacheTracker));

        private readonly IInstrumentedRequestContextAccessor instrumentedRequestContextAccessor;

        public SimpleCacheTracker(IInstrumentedRequestContextAccessor instrumentedRequestContextAccessor)
        {
            this.instrumentedRequestContextAccessor = instrumentedRequestContextAccessor ?? throw new ArgumentNullException(nameof(instrumentedRequestContextAccessor));
        }

        public void CacheMiss(string key)
        {
            trace.TraceVerbose($"Cache: Miss, Key = '{key}'");
        }

        public void CacheHit(string key)
        {
            trace.TraceVerbose($"Cache: Hit, Key = '{key}'");
        }

        public void LockTimeoutActivated(string key)
        {
            trace.TraceWarning($"Cache: LockTimeoutActivated, Key = '{key}'");
        }

        public void LockAcquired()
        {
            trace.TraceVerbose("Cache: LockAcquired");
        }

        public void LockReleased()
        {
            trace.TraceVerbose("Cache: LockReleased");
        }

        public void CacheHitAfterLock(string key)
        {
            trace.TraceVerbose($"Cache: HitAfterLock, Key = '{key}'");
        }

        public CacheLocker GetCacheLocker(string key, Func<CacheLocker> executer, LoggingContext context = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> SourceGetter<T>(string key, Func<Task<T>> executer, LoggingContext context = null) where T : class
        {
            return ExecuteCacheOperationAsync(
                nameof(SourceGetter),
                string.Empty,
                executer);
        }

        public Task<T> GetAsync<T>(string key, Func<GetOperationContext, Task<T>> executer, CacheSmartGetOperationContext operationContext = null, LoggingContext loggingContext = null) where T : class
        {
            return ExecuteCacheOperationAsync(
                nameof(GetAsync),
                key,
                executer);
        }

        public Task<T> SmartGetAsync<T>(string key, Func<Task<T>> executer, CacheSmartGetOperationContext operationContext, LoggingContext loggingContext = null) where T : class
        {
            return ExecuteCacheOperationAsync(
                nameof(SmartGetAsync),
                key,
                executer);
        }

        public Task SmartGetRefreshAsync(string key, Func<Task> executer, LoggingContext context = null)
        {
            var instrumentedRequestContext = instrumentedRequestContextAccessor.GetInstrumentedRequestContext();

            return instrumentedRequestContext.TrackDependencyOperationAsync<CacheOutgoingServiceEvent, bool>(
                dependencyName: nameof(SimpleCacheTracker),
                dependencyOperationName: nameof(SmartGetRefreshAsync),
                prepareOperation: null,
                action: (dependencyOperation, operationResult) => TrackedCacheOperationAsync(dependencyOperation, operationResult, () =>
                {
                    executer();
                    return Task.FromResult(true);
                }, key));
        }

        public Task<bool> PutAsync(string key, Func<Task<bool>> executer, LoggingContext context = null)
        {
            return ExecuteCacheOperationAsync(
                nameof(PutAsync),
                key,
                executer);
        }

        private Task<T> ExecuteCacheOperationAsync<T>(
            string id,
            string cacheKey,
            Func<Task<T>> action)
        {
            var instrumentedRequestContext = instrumentedRequestContextAccessor.GetInstrumentedRequestContext();

            return instrumentedRequestContext.TrackDependencyOperationAsync<CacheOutgoingServiceEvent, T>(
                dependencyName: nameof(SimpleCacheTracker),
                dependencyOperationName: id,
                prepareOperation: null,
                action: (dependencyOperation, operationResult) => TrackedCacheOperationAsync(dependencyOperation, operationResult, action, cacheKey));
        }

        private Task<T> ExecuteCacheOperationAsync<T>(
            string id,
            string cacheKey,
            Func<GetOperationContext, Task<T>> action)
        {
            var instrumentedRequestContext = instrumentedRequestContextAccessor.GetInstrumentedRequestContext();

            return instrumentedRequestContext.TrackDependencyOperationAsync<CacheOutgoingServiceEvent, T>(
                dependencyName: nameof(SimpleCacheTracker),
                dependencyOperationName: id,
                prepareOperation: null,
                action: (dependencyOperation, operationResult) => TrackedCacheOperationAsync(dependencyOperation, operationResult, action, cacheKey));
        }

        private async Task<T> TrackedCacheOperationAsync<T>(
            ILogicalDependencyOperation<CacheOutgoingServiceEvent> dependencyOperation,
            TrackingOperationResult trackingResult,
            Func<Task<T>> action, string cacheKey)
        {
            var cachedItem = default(T);

            try
            {
                cachedItem = await action().ConfigureAwait(false);
            }
            catch (CacheException)
            {
                // Don't log as this was already logged.
                // Don't throw as we don't want to go down if cache is down
                // Simply set the operation as failed
                trackingResult.Status = TrackingOperationResultStatus.Failed;
            }

            dependencyOperation.QosEvent.Key = cacheKey;

            return cachedItem;
        }

        private async Task<T> TrackedCacheOperationAsync<T>(
            ILogicalDependencyOperation<CacheOutgoingServiceEvent> dependencyOperation,
            TrackingOperationResult trackingResult,
            Func<GetOperationContext, Task<T>> action, string cacheKey)
        {
            var cachedItem = default(T);

            var context = new GetOperationContext();

            try
            {
                cachedItem = await action(context).ConfigureAwait(false);
            }
            catch (CacheException)
            {
                // Don't log as this was already logged.
                // Don't throw as we don't want to go down if cache is down
                // Simply set the operation as failed
                trackingResult.Status = TrackingOperationResultStatus.Failed;
            }

            dependencyOperation.QosEvent.Key = cacheKey;

            return cachedItem;
        }
    }
}
