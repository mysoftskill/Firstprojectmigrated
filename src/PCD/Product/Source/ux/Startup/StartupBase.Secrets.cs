using Microsoft.Extensions.DependencyInjection;
using Microsoft.PrivacyServices.UX.Core.Security;

namespace Microsoft.PrivacyServices.UX.Startup
{
    public abstract partial class StartupBase
    {
        /// <summary>
        /// Configure the certificate finder.
        /// </summary>
        private void AddCertificateFinder(IServiceCollection services)
        {
            services.AddSingleton<ICertificateFinder>(sp => { return GetCertificateFinder(); });
        }

        /// <summary>
        /// Gets the mocked or real implementation of certificate finder.
        /// </summary>
        protected abstract ICertificateFinder GetCertificateFinder();
    }
}
