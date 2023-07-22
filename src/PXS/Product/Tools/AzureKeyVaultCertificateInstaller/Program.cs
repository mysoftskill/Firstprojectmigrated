// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.AzureKeyVaultCertificateInstaller
{
    using System;
    using System.Diagnostics;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                ILogger logger = DualLogger.Instance;
                IPrivacyConfigurationManager configuration = PrivacyConfigurationManager.LoadCurrentConfiguration(logger);

#if DEBUG
                await SetupDevBoxAsync(configuration, logger);
#endif

                await new CertificateInstaller(logger, configuration).RunAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press [Enter] to exit");
                Console.ReadLine();
            }
        }

        private static async Task SetupDevBoxAsync(IPrivacyConfigurationManager configuration, ILogger logger)
        {
            if (configuration.EnvironmentConfiguration.EnvironmentType == EnvironmentType.OneBox)
            {
                // Note: This applies to dev box only.
                // If we are a new dev, or a dev box is operating in a clean state, we need a certificate before we can do anything.
                // A bootstrapper cert is used, stored in a non-prod KeyVault we have access to from a corpnet account as long as we are in the right team security group.
                // This cert is used to get an access token, which is then able to authenticate with an AME KeyVault for the rest of the certs needed.

                AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider();
                IKeyVaultClient client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));
                Uri devBoxKeyVault = new Uri("https://pxs-devbox.vault.azure.net/");
                SecretBundle secretBundle = await client.GetSecretAsync(devBoxKeyVault.ToString(), "aad", CancellationToken.None).ConfigureAwait(false);
                X509Certificate2 cert = new X509Certificate2(
                    Convert.FromBase64String(secretBundle.Value),
                    (SecureString)null,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                logger.Information(nameof(Program), cert.ToLogMessage("PXS", "The PXS aad certificate."));
                if (CertificateInstaller.TryInstallCertificate(cert, logger))
                {
                    logger.Information(nameof(Program), "Success installing bootstrapper cert.");
                }
                else
                {
                    logger.Error(
                        nameof(Program),
                        "Fatal error. Could not install bootstrapper cert. Ensure you are in the appropriate REDMOND SecurityGroup used by the team, and your VS has your REDMOND credentials.");
                }
            }
        }
    }
}
