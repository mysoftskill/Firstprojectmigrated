[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// <c>Cmdlets</c> for DataAssets.
    /// </summary>
    public class DataAssetCmdlet
    {
        /// <summary>
        /// A <c>cmdlet</c> that connects to the PDMS service.
        /// </summary>
        [Cmdlet(VerbsCommon.Find, "PdmsDataAsset")]
        public class FindCmdlet : IHttpResultCmdlet<IEnumerable<DataAsset>>
        {
            /// <summary>
            /// The qualifier string to search for.
            /// </summary>
            [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
            [ValidateNotNull]
            public string Qualifier { get; set; }

            /// <summary>
            /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
            /// </summary>
            /// <param name="client">The PDMS client.</param>
            /// <param name="context">The request context.</param>
            /// <returns>The list of objects.</returns>
            protected override async Task<IHttpResult<IEnumerable<DataAsset>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
            {
                var qualifier = AssetQualifier.Parse(this.Qualifier);
                var result = await client.DataAssets.FindByQualifierAsync(qualifier, context).ConfigureAwait(false);

                if (result.Response.NextLink != null)
                {
                    this.WarningMessage = "Multiple pages of data found. Only the first page of data has been added to the Powershell pipeline.";
                }

                return result.Convert(x => x.Response.Value, 2);
            }
        }
    }
}