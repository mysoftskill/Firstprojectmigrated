[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System;
    using System.Management.Automation;

    using Microsoft.PrivacyServices.DataManagement.Client.AAD;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;

    using V2 = Microsoft.PrivacyServices.DataManagement.Client.V2;

    /// <summary>
    /// Contains <c>cmdlets</c> for connection to the PDMS service.
    /// </summary>
    public class ServiceCmdlet
    {
        private const string NgpPdmsNonProdId = "25862df9-0e4d-4fb7-a5c8-dfe8ac261930";

        private static readonly object LockObj = new object();
        private static BaseHttpServiceProxy httpServiceProxy;
        private static IAuthenticationProvider authenticationProvider;

        /// <summary>
        /// Gets or sets the PDMS client.
        /// </summary>
        public static V2.IDataManagementClient DataManagementClient { get; set; }

        /// <summary>
        /// Gets or sets the service tree client.
        /// </summary>
        public static IServiceTreeClient ServiceTreeClient { get; set; }

        /// <summary>
        /// Creates a request context.
        /// </summary>
        /// <returns>The context.</returns>
        public static RequestContext CreateRequestContext()
        {
            return new RequestContext { AuthenticationProvider = authenticationProvider };
        }
        
        /// <summary>
        /// A <c>cmdlet</c> that connects to the PDMS service.
        /// </summary>
        [Cmdlet(VerbsCommunications.Connect, "PdmsService")]
        public class ConnectCmdlet : Cmdlet
        {
            /// <summary>
            /// The PDMS environment.
            /// </summary>
            [Parameter(Position = 0, Mandatory = true)]
            [ValidateSet("INT", "CI1", "CI2", "PPE", "PROD")]
            public string Location { get; set; }

            /// <summary>
            /// The authentication provider. If not provided, local user based authentication is used.
            /// </summary>
            [Parameter(Position = 1, Mandatory = false)]
            public IAuthenticationProvider AuthenticationProvider { get; set; }

            /// <summary>
            /// Instantiates the client based on the <c>cmdlet</c> parameters.
            /// </summary>
            protected override void ProcessRecord()
            {
                lock (ServiceCmdlet.LockObj)
                {
                    Uri environmentUri = null;
                    bool isProd = false;

                    switch (this.Location.ToUpper())
                    {
                        case "INT":
                            environmentUri = new Uri("https://management.privacy.microsoft-int.com");
                            break;
                        case "CI1":
                            environmentUri = new Uri("https://ci1.management.privacy.microsoft-int.com");
                            break;
                        case "CI2":
                            environmentUri = new Uri("https://ci2.management.privacy.microsoft-int.com");
                            break;
                        case "PPE":
                            environmentUri = new Uri("https://management.privacy.microsoft-ppe.com");
                            break;
                        case "PROD":
                            environmentUri = new Uri("https://management.privacy.microsoft.com");
                            isProd = true;
                            break;
                    }

                    ServiceCmdlet.httpServiceProxy = new HttpServiceProxy(environmentUri, defaultTimeout: System.Threading.Timeout.InfiniteTimeSpan);

                    ServiceCmdlet.DataManagementClient = new V2.DataManagementClient(ServiceCmdlet.httpServiceProxy);

                    if (this.AuthenticationProvider == null)
                    {
                        var authenticationProviderFactory = new UserAzureActiveDirectoryProviderFactory(NgpPdmsNonProdId, isProd);
                        ServiceCmdlet.authenticationProvider = authenticationProviderFactory.CreateForCurrentUser();
                    }
                    else
                    {
                        ServiceCmdlet.authenticationProvider = this.AuthenticationProvider;
                    }
                }
            }
        }

        /// <summary>
        /// A <c>cmdlet</c> that disconnects from the PDMS service.
        /// </summary>
        [Cmdlet(VerbsCommunications.Disconnect, "PdmsService")]        
        public class DisconnectCmdlet : Cmdlet
        {
            /// <summary>
            /// Tears down the client.
            /// </summary>
            protected override void ProcessRecord()
            {
                lock (ServiceCmdlet.LockObj)
                {
                    ServiceCmdlet.httpServiceProxy.Dispose();

                    ServiceCmdlet.DataManagementClient = null;
                    ServiceCmdlet.authenticationProvider = null;
                    ServiceCmdlet.httpServiceProxy = null;
                }
            }
        }
    }
}