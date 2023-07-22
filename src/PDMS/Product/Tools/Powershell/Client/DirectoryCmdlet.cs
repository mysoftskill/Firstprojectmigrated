[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Management.Automation;

    using Microsoft.Graph;
    using Microsoft.PrivacyServices.DataManagement.Client.AAD;

    /// <summary>
    /// Contains <c>cmdlets</c> for connection to the PDMS service.
    /// </summary>
    public class DirectoryCmdlet
    {
        private const string NgpPdmsNonProdId = "25862df9-0e4d-4fb7-a5c8-dfe8ac261930";

        private static IHttpProvider httpProvider;

        /// <summary>
        /// Gets or sets the service tree client.
        /// </summary>
        public static IGraphServiceClient GraphClient { get; set; }

        /// <summary>
        /// A <c>cmdlet</c> that connects to the PDMS service.
        /// </summary>
        [Cmdlet(VerbsCommunications.Connect, "PdmsDirectory")]
        public class ConnectCmdlet : Cmdlet
        {
            /// <summary>
            /// Instantiates the client based on the <c>cmdlet</c> parameters.
            /// </summary>
            protected override void ProcessRecord()
            {
                DirectoryCmdlet.httpProvider = new HttpProvider();

                var authenticationProviderFactory = new UserAzureActiveDirectoryProviderFactory(NgpPdmsNonProdId, true);
                authenticationProviderFactory.ResourceId = "https://graph.microsoft.com";
                var authenticationProvider = new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    var tokenFactory = authenticationProviderFactory.CreateForCurrentUser();
                    requestMessage.Headers.Authorization = await tokenFactory.AcquireTokenAsync(System.Threading.CancellationToken.None).ConfigureAwait(false);
                });

                DirectoryCmdlet.GraphClient = new GraphServiceClient(authenticationProvider, DirectoryCmdlet.httpProvider);
            }
        }

        /// <summary>
        /// A <c>cmdlet</c> that disconnects from the PDMS service.
        /// </summary>
        [Cmdlet(VerbsCommunications.Disconnect, "PdmsDirectory")]
        public class DisconnectCmdlet : Cmdlet
        {
            /// <summary>
            /// Tears down the client.
            /// </summary>
            protected override void ProcessRecord()
            {
                DirectoryCmdlet.httpProvider.Dispose();
                DirectoryCmdlet.GraphClient = null;
            }
        }

        /// <summary>
        /// A <c>cmdlet</c> that disconnects from the PDMS service.
        /// </summary>
        [Cmdlet(VerbsCommon.Get, "PdmsDirectoryUser")]
        public class GetUserCmdlet : Cmdlet
        {
            /// <summary>
            /// Uses the service id for querying.
            /// </summary>
            [Parameter(Position = 0)]
            public string Id { get; set; }

            /// <summary>
            /// Gets the user.
            /// </summary>
            protected override void ProcessRecord()
            {
                if (this.Id.StartsWith("a:"))
                {
                    this.Id = this.Id.Substring(2);
                }

                var request = DirectoryCmdlet.GraphClient.Users[this.Id].Request();
                var user = request.GetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                this.WriteObject(user);
            }
        }
    }
}