using System.IO;
using Microsoft.Osgs.Infra.Platform;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.Security;

namespace Microsoft.PrivacyServices.UX.Startup
{
    public sealed partial class I9nStartup
    {
        protected override ICertificateFinder GetCertificateFinder()
        {
            return new MockCerficateFinder();
        }
    }
}
