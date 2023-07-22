[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client.AAD;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;

    /// <summary>
    /// Contains <c>cmdlets</c> for connection to the PDMS service.
    /// </summary>
    public class ServiceTreeCmdlet
    {
        private const string NgpPdmsNonProdId = "25862df9-0e4d-4fb7-a5c8-dfe8ac261930";

        private static readonly object LockObj = new object();
        private static BaseHttpServiceProxy httpServiceProxy;
        private static IAuthenticationProvider authenticationProvider;

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
        [Cmdlet(VerbsCommunications.Connect, "PdmsServiceTree")]
        public class ConnectCmdlet : Cmdlet
        {
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
                lock (ServiceTreeCmdlet.LockObj)
                {
                    Uri environmentUri = new Uri("https://servicetree.msftcloudes.com/");

                    ServiceTreeCmdlet.httpServiceProxy = new HttpServiceProxy(environmentUri, defaultTimeout: System.Threading.Timeout.InfiniteTimeSpan);

                    ServiceTreeCmdlet.ServiceTreeClient = new ServiceTreeClient(ServiceTreeCmdlet.httpServiceProxy);

                    if (this.AuthenticationProvider == null)
                    {
                        var authenticationProviderFactory = new UserAzureActiveDirectoryProviderFactory(NgpPdmsNonProdId, true);
                        authenticationProviderFactory.ResourceId = Defaults.ServiceTreeResourceId;
                        ServiceTreeCmdlet.authenticationProvider = authenticationProviderFactory.CreateForCurrentUser();
                    }
                    else
                    {
                        ServiceTreeCmdlet.authenticationProvider = this.AuthenticationProvider;
                    }
                }
            }
        }

        /// <summary>
        /// A <c>cmdlet</c> that disconnects from the PDMS service.
        /// </summary>
        [Cmdlet(VerbsCommunications.Disconnect, "PdmsServiceTree")]        
        public class DisconnectCmdlet : Cmdlet
        {
            /// <summary>
            /// Tears down the client.
            /// </summary>
            protected override void ProcessRecord()
            {
                lock (ServiceTreeCmdlet.LockObj)
                {
                    ServiceTreeCmdlet.httpServiceProxy.Dispose();

                    ServiceTreeCmdlet.ServiceTreeClient = null;
                    ServiceTreeCmdlet.authenticationProvider = null;
                    ServiceTreeCmdlet.httpServiceProxy = null;
                }
            }
        }

        /// <summary>
        /// A <c>cmdlet</c> that disconnects from the PDMS service.
        /// </summary>
        [Cmdlet(VerbsCommon.Get, "PdmsServiceTree")]
        public class GetCmdlet : IHttpResultCmdlet<object>.ServiceTree
        {
            /// <summary>
            /// Uses the service id for querying.
            /// </summary>
            [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
            public string ServiceId { get; set; }
            
            /// <summary>
            /// Uses the team group id for querying.
            /// </summary>
            [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
            public string TeamGroupId { get; set; }

            /// <summary>
            /// Uses the service group id for querying.
            /// </summary>
            [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
            public string ServiceGroupId { get; set; }

            /// <summary>
            /// Calls the service to retrieve the data.
            /// </summary>
            /// <param name="client">The service tree client.</param>
            /// <param name="context">The request context.</param>
            /// <returns>The service tree data.</returns>
            protected override async Task<IHttpResult<object>> ExecuteAsync(IServiceTreeClient client, RequestContext context)
            {
                if (this.ServiceId != null)
                {
                    var result = await client.ReadServiceWithExtendedProperties(Guid.Parse(this.ServiceId), context).ConfigureAwait(false);
                    return result.Convert(x => (object)x.Response, 2);
                }
                else if (this.TeamGroupId != null)
                {
                    var result = await client.ReadTeamGroupWithExtendedProperties(Guid.Parse(this.TeamGroupId), context).ConfigureAwait(false);
                    return result.Convert(x => (object)x.Response, 2);
                }
                else
                {
                    var result = await client.ReadServiceGroupWithExtendedProperties(Guid.Parse(this.ServiceGroupId), context).ConfigureAwait(false);
                    return result.Convert(x => (object)x.Response, 2);
                }
            }
        }
    }
}