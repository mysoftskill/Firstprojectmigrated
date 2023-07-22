using System;
using System.Threading.Tasks;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.Osgs.ServiceClient.Compass.Exceptions;
using Microsoft.Osgs.ServiceClient.Compass.Tracking;
using Microsoft.PrivacyServices.UX.Monitoring.Events;

namespace Microsoft.PrivacyServices.UX.Core.Cms
{
    /// <summary>
    /// Tracks operations for <see cref="Microsoft.Windows.Services.CompassService.Client.ICmsClient"/>.
    /// </summary>
    public class CmsClientOperationTracker : ICompassClientTracking
    {
        private readonly IInstrumentedRequestContextAccessor instrumentedRequestContextAccessor;

        public CmsClientOperationTracker(IInstrumentedRequestContextAccessor instrumentedRequestContextAccessor)
        {
            this.instrumentedRequestContextAccessor = instrumentedRequestContextAccessor ?? throw new ArgumentNullException(nameof(instrumentedRequestContextAccessor));
        }

        #region ICompassClientTracking Members

        public Task<T> GetContentAsync<T>(Func<OperationContext, Task<T>> action) where T : class
        {
            var instrumentedRequestContext = instrumentedRequestContextAccessor.GetInstrumentedRequestContext();

            return instrumentedRequestContext.TrackDependencyOperationAsync<CompassOutgoingServiceEvent, T>(
                dependencyName: "Compass",
                dependencyOperationName: nameof(GetContentAsync),
                prepareOperation: null,
                action: (dependencyOperation, operationResult) => TrackedGetContentAsync(dependencyOperation, operationResult, action));
        }

        #endregion

        private async Task<T> TrackedGetContentAsync<T>(ILogicalDependencyOperation<CompassOutgoingServiceEvent> dependencyOperation, TrackingOperationResult trackingResult, 
            Func<OperationContext, Task<T>> action) where T : class
        {
            T value = null;
            var context = new OperationContext();
            try
            {
                try
                {
                    value = await action(context).ConfigureAwait(false);
                }
                catch (ContentNotFoundException)
                {
                    trackingResult.Status = TrackingOperationResultStatus.Failed;
                }
            }
            finally
            {
                dependencyOperation.QosEvent.Endpoint = context.Endpoint;
                dependencyOperation.QosEvent.ContentPath = context.ContentPath;
                dependencyOperation.QosEvent.Locale = context.Locale;
                dependencyOperation.QosEvent.CacheKey = context.CacheKey;
                dependencyOperation.QosEvent.IsCacheMiss = context.IsCacheMiss;
            }

            return value;
        }
    }
}
