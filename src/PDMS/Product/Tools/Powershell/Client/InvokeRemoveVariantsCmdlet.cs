[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    /// <summary>
    /// <c>Cmdlets</c> for DataAssets.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "PdmsRemoveVariants")]
    public class InvokeRemoveVariantsCmdlet : IHttpResultCmdlet<AssetGroup>
    {
        /// <summary>
        /// The id of the asset group to retrieve.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true)]
        [ValidateNotNull]
        public string Id { get; set; }

        /// <summary>
        /// The list of variant ids to remove.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public string[] VariantIds { get; set; }

        /// <summary>
        /// The e-tag of the asset group.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipeline = true, Mandatory = true)]
        [ValidateNotNull]
        public string ETag { get; set; }

        /// <summary>
        /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The updated asset group.</returns>
        protected override async Task<IHttpResult<AssetGroup>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            var value = await client.AssetGroups.RemoveVariantsAsync(this.Id, this.VariantIds, this.ETag, context).ConfigureAwait(false);
            return value.Convert(x => x.Response, 2);
        }
    }
}