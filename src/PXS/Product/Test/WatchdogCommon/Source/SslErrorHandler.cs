// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.WatchdogCommon
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Membership.MemberServices.Common.Logging;

    using Microsoft.PrivacyServices.Common.Azure;

    [ExcludeFromCodeCoverage]
    public static class SslErrorHandler
    {
        private const string ComponentName = nameof(SslErrorHandler);

        public static void HandleSslErrors(ILogger logger, string sslCertificateCommonName)
        {
            // Watchdogs need to check every instance running. To do so, you make a request to an environment specific url with the machine's name.
            // However, the machine will give the cert for windowsphone-int.com or windowsphone.com instead. This is designed to ensure that we
            // accept this cert, but also that we don't accept any other cert.
            ServicePointManager.ServerCertificateValidationCallback = delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                try
                {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                    {
                        return true;
                    }

                    if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
                    {
                        X509Certificate2 cert2 = new X509Certificate2(certificate);
                        string commonName = cert2.GetNameInfo(X509NameType.SimpleName, false);

                        if (string.Equals(commonName, sslCertificateCommonName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        logger.Information(ComponentName, "Rejecting cert with common name {0} because it was not valid", commonName);
                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    logger.Error(ComponentName, exception, "Error with certificate validation.");
                    return false;
                }
            };
        }
    }
}
