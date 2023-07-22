[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    /// <summary>
    /// Issues an update call against the service.
    /// </summary>
    public partial class SetDataOwnerCmdlet
    {
        /// <summary>
        /// Calls the ReplaceServiceId API if set instead of the UpdateDataOwner API.
        /// </summary>
        [Parameter(Position = 0, Mandatory = false)]
        public SwitchParameter ReplaceServiceId { get; set; }

        /// <summary>
        /// Calls the service to update the object using custom APIs.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override async Task<IHttpResult<DataOwner>> CustomExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.ReplaceServiceId.IsPresent)
            {
                return await client.DataOwners.ReplaceServiceIdAsync(this.Value, context).ConfigureAwait(false);
            }
            else
            {
                return null;
            }
        }
    }
}