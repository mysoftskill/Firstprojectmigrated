using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Azure.ComplianceServices.Common;
using Microsoft.Osgs.Infra.Monitoring;
using Microsoft.Osgs.Web.Core.Configuration;
using Microsoft.PrivacyServices.UX.Monitoring.Events;

namespace Microsoft.PrivacyServices.UX.Core.Flighting
{
    /// <summary>
    /// Implements <see cref="IGroundControl"/> using Carbon Flighting.
    /// </summary>
    public class GroundControl : IGroundControl
    {
        private readonly IAppConfiguration appConfiguration;
        private readonly IEnvironmentInfo environmentInfo;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IInstrumentedRequestContextAccessor instrumentedRequestContextAccessor;

        public GroundControl(
            IAppConfiguration appConfig,
            IEnvironmentInfo environmentInfo,
            IHttpContextAccessor httpContextAccessor,
            IInstrumentedRequestContextAccessor instrumentedRequestContextAccessor)
        {
            appConfiguration = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            this.environmentInfo = environmentInfo ?? throw new ArgumentNullException(nameof(environmentInfo));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            this.instrumentedRequestContextAccessor = instrumentedRequestContextAccessor ?? throw new ArgumentNullException(nameof(instrumentedRequestContextAccessor));
        }

        #region IGroundControl Members

        public Task<IEnumerable<string>> GetUserFlights(IReadOnlyDictionary<string, string> additionalProps = null)
        {
            return ExecuteMonitoredOperationAsync(
                nameof(GetUserFlights),
                () => appConfiguration.GetEnabledFeaturesAsync(GetFlightContext(additionalProps)).GetAwaiter().GetResult());
        }

        public Task<bool> IsUserInFlight(string flightName, IReadOnlyDictionary<string, string> additionalProps = null)
        {
            return ExecuteMonitoredOperationAsync(
                nameof(IsUserInFlight),
                () => appConfiguration.IsFeatureFlagEnabledAllAsync(flightName, GetFlightContext(additionalProps)).GetAwaiter().GetResult());
        }

        #endregion

        private ICustomOperatorContext AddDefaultCustomOperatorContextProperties(ICustomOperatorContext context)
        {

            context.EnvironmentName = environmentInfo.EnvironmentType.ToUpperInvariant();
            context.Market = httpContextAccessor.HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.UICulture.Name;

            return context;
        }

        private IEnumerable<ICustomOperatorContext> GetFlightContext(IReadOnlyDictionary<string, string> additionalProps = null)
        {
            List<ICustomOperatorContext> contextList = new List<ICustomOperatorContext>();

            ICustomOperatorContext context  = CustomOperatorContextFactory.CreateDefaultStringComparisonContextWithKeyValue("UserID",
                        httpContextAccessor.HttpContext.User.Identity.Name);

            // Add a operator context to check for userID
            contextList.Add(AddDefaultCustomOperatorContextProperties(context));

            // For additional properties, create new key value context.
            if (additionalProps != null)
            {
                foreach (var kvp in additionalProps)
                {
                    contextList.Add(AddDefaultCustomOperatorContextProperties(CustomOperatorContextFactory.CreateDefaultStringComparisonContextWithKeyValue(kvp.Key, kvp.Value)));
                }
            }

            return contextList;
        }

        private Task<TResult> ExecuteMonitoredOperationAsync<TResult>(string operationName, Func<TResult> action)
        {
            return instrumentedRequestContextAccessor.GetInstrumentedRequestContext().TrackDependencyOperationAsync<OutgoingServiceEvent, TResult>(
                nameof(AppConfiguration),
                operationName,
                prepareOperation: null,
                action: (operation, result) =>
                {
                    return Task.FromResult(action());
                });
        }
    }
}
