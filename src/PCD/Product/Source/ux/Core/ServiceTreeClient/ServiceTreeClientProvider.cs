using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Osgs.Core.Helpers;
using Microsoft.Osgs.Infra.Monitoring.Context;
using Microsoft.Osgs.Infra.Platform;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.AAD;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.Security;

namespace Microsoft.PrivacyServices.UX.Core.ServiceTreeClient
{
    /// <summary>
    /// Provides access to Service Tree client instance.
    /// </summary>
    public class ServiceTreeClientProvider : IServiceTreeClientProvider
    {
        private readonly ICorrelationVectorContext correlationVectorContext;

        private readonly IJwtBearerTokenAccessor bearerTokenAccessor;

        private readonly IAuthenticationProviderFactory authenticationProviderFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTreeClientProvider"/> class.
        /// </summary>
        public ServiceTreeClientProvider(
            DataManagement.Client.ServiceTree.IServiceTreeClient instance,
            IAzureADConfig aadConfig,
            ICertificateFinder certificateFinder,
            ICorrelationVectorContext correlationVectorContext,
            IJwtBearerTokenAccessor bearerTokenAccessor,
            IHostingEnvironment hostingEnvironment)
        {
            this.correlationVectorContext = correlationVectorContext ?? throw new ArgumentNullException(nameof(correlationVectorContext));
            this.bearerTokenAccessor = bearerTokenAccessor ?? throw new ArgumentNullException(nameof(bearerTokenAccessor));

            Instance = instance ?? throw new ArgumentNullException(nameof(instance));

            EnsureArgument.NotNull(aadConfig, nameof(aadConfig));
            EnsureArgument.NotNull(certificateFinder, nameof(certificateFinder));
            EnsureArgument.NotNull(hostingEnvironment, nameof(hostingEnvironment));

            authenticationProviderFactory = new ServiceAzureActiveDirectoryProviderFactory(aadConfig.AppId, certificateFinder.FindBySubjectName(aadConfig.CertSubjectName), targetProductionEnvironment: hostingEnvironment.IsProduction(), sendX5c: true)
            {
                ResourceId = Defaults.ServiceTreeResourceId,
            };
        }

        #region IServiceTreeClientProvider Members

        public DataManagement.Client.ServiceTree.IServiceTreeClient Instance { get; }

        public async Task<RequestContext> CreateNewRequestContext() => new RequestContext()
        {
            CorrelationVector = correlationVectorContext.Current.Value,
            AuthenticationProvider = authenticationProviderFactory.CreateForClient()
        };

        #endregion
    }
}
