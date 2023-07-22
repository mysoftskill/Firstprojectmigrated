[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    /// <summary>
    /// <c>Cmdlets</c> for DataAssets.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "PdmsRegistrationStatus")]
    public class InvokeRegistrationStatusCmdlet : IHttpResultCmdlet<object>
    {
        /// <summary>
        /// The id of the object to retrieve.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public string Id { get; set; }

        /// <summary>
        /// The entity type whose status should be invoked.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateSet("DeleteAgent", "AssetGroup")]
        public string EntityType { get; set; }

        /// <summary>
        /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of objects.</returns>
        protected override async Task<IHttpResult<object>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.EntityType == "DeleteAgent")
            {
                var value = await client.DataAgents.CalculateDeleteAgentRegistrationStatus(this.Id, context).ConfigureAwait(false);
                return value.Convert(x => (object)x.Response, 2);
            }
            else if (this.EntityType == "AssetGroup")
            {
                var value = await client.AssetGroups.CalculateRegistrationStatus(this.Id, context).ConfigureAwait(false);
                return value.Convert(x => (object)x.Response, 2);
            }

            return null; // This should never be hit due to the parameter validation.
        }
    }
}