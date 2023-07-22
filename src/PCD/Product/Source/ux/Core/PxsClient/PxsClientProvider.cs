using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Osgs.Infra.Monitoring.Context;
using Microsoft.PrivacyServices.PrivacyOperation.Client;
using Microsoft.PrivacyServices.PrivacyOperation.Client.Models;
using Microsoft.PrivacyServices.UX.Core.Security;

namespace Microsoft.PrivacyServices.UX.Core.PxsClient
{
    public class PxsClientProvider : IPxsClientProvider
    {
        private readonly ICorrelationVectorContext correlationVectorContext;

        private readonly IJwtBearerTokenAccessor bearerTokenAccessor;

        public PxsClientProvider(
            IPrivacyOperationClient instance,
            ICorrelationVectorContext correlationVectorContext,
            IJwtBearerTokenAccessor bearerTokenAccessor)
        {
            this.correlationVectorContext = correlationVectorContext ?? throw new ArgumentNullException(nameof(correlationVectorContext));
            this.bearerTokenAccessor = bearerTokenAccessor ?? throw new ArgumentNullException(nameof(bearerTokenAccessor));

            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public IPrivacyOperationClient Instance { get; }

        public async Task<T> ApplyRequestContext<T>(T operationArgs) where T : BasePrivacyOperationArgs
        {
            var jwtAuthToken = await bearerTokenAccessor.GetFromHttpContextAsync();

            operationArgs.CorrelationVector = correlationVectorContext.Current.Value;
            operationArgs.UserAssertion = new UserAssertion(jwtAuthToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");

            return operationArgs;
        }
    }
}
