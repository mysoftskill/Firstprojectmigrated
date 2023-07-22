using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Osgs.Core.Helpers;
using Microsoft.Osgs.Infra.Monitoring.Context;
using Microsoft.Osgs.Infra.Platform;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.AAD;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.Security;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    /// <summary>
    /// Provides access to PDMS client instance.
    /// </summary>
    public class PdmsClientProvider : IPdmsClientProvider
    {
        private readonly ICorrelationVectorContext correlationVectorContext;

        private readonly IJwtBearerTokenAccessor bearerTokenAccessor;

        private readonly IAuthenticationProviderFactory authenticationProviderFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdmsClientProvider"/> class.
        /// </summary>
        public PdmsClientProvider(
            DataManagement.Client.V2.IDataManagementClient instance,
            ICorrelationVectorContext correlationVectorContext,
            IJwtBearerTokenAccessor bearerTokenAccessor,
            IAuthenticationProviderFactory authenticationProviderFactory)
        {
            this.correlationVectorContext = correlationVectorContext ?? throw new ArgumentNullException(nameof(correlationVectorContext));
            this.bearerTokenAccessor = bearerTokenAccessor ?? throw new ArgumentNullException(nameof(bearerTokenAccessor));
            this.authenticationProviderFactory = authenticationProviderFactory ?? throw new ArgumentNullException(nameof(authenticationProviderFactory));

            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        #region IPdmsClientProvider Members

        public DataManagement.Client.V2.IDataManagementClient Instance { get; }

        public async Task<RequestContext> CreateNewRequestContext() => CreateNewRequestContext(await bearerTokenAccessor.GetFromHttpContextAsync());

        public RequestContext CreateNewRequestContext(string jwtAuthToken) => new RequestContext()
        {
            CorrelationVector = correlationVectorContext.Current.Value,
            AuthenticationProvider = authenticationProviderFactory.CreateForUserDelegate(jwtAuthToken)
        };

        #endregion
    }
}
