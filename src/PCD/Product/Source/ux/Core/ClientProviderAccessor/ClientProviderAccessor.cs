using System;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.PrivacyServices.UX.Configuration;

namespace Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor
{
    /// <summary>
    /// Provides access to client provider with either the real client or mocked client.
    /// </summary>
    public class ClientProviderAccessor<TClientProvider, TMockClientProvider> : IClientProviderAccessor<TClientProvider> 
        where TMockClientProvider : TClientProvider
    {
        private readonly TClientProvider realProvider;
        private readonly Lazy<TMockClientProvider> lazyMockProvider;
        private readonly bool enableMocks;

        public ClientProviderAccessor(
            TClientProvider realClientProvider,
            IConfiguration configuration,
            IMocksConfig mocksConfig,
            IHttpContextAccessor contextAccessor)
        {
            realProvider = realClientProvider;

            lazyMockProvider = new Lazy<TMockClientProvider>(() => (TMockClientProvider)Activator.CreateInstance(
                                                                        typeof(TMockClientProvider),
                                                                        new object[] { contextAccessor }
                                                                    ));

            //  If mocks are allowed and (I9n mode is enabled or x-scenarios header is passed), enable WebRole mocks.
            if (mocksConfig.AllowMocks && (
                    configuration.GetValue<string>("i9nMode").Equals(bool.TrueString) ||
                    contextAccessor.HttpContext.Request.Headers.Keys.Contains("x-scenarios")))
            {
                enableMocks = true;
            }
        }

        public TClientProvider ProviderInstance => enableMocks ? lazyMockProvider.Value : realProvider;
    }
}
