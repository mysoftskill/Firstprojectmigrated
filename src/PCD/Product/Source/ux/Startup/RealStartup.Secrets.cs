using System.IO;
using Microsoft.Osgs.Infra.Platform;
using Microsoft.PrivacyServices.Common.Azure;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.Security;

namespace Microsoft.PrivacyServices.UX.Startup
{
    public sealed partial class RealStartup
    {
        protected override ICertificateFinder GetCertificateFinder()
        {
            DualLogger logger = DualLogger.Instance;
            DualLogger.AddTraceListener();
            return new CertificateFinder(logger);
        }
    }
}
