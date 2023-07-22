[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Generated code.")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Generated code.")]
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    /// <summary>
    /// Variant Request Actions.
    /// </summary>
    public enum VariantRequestAction
    {
        /// <summary>
        /// Approve Variant Request.
        /// </summary>
        Approve = 0,

        /// <summary>
        /// Deny Variant Request.
        /// </summary>
        Deny,

        /// <summary>
        /// Update Variant Request.
        /// </summary>
        Update
    }

    /// <summary>
    /// Get the list of variant requests.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "PdmsVariantRequest")]
    [OutputType(typeof(VariantRequest))]
    public partial class FindVariantRequestCmdlet : IHttpResultCmdlet<IEnumerable<VariantRequest>>
    {
        /// <summary>
        /// Filter Variant Requests by Owner.
        /// </summary>
        [Parameter(Position = 0, Mandatory = false, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        public string Owner { get; set; }

        /// <summary>
        /// Calls the service to retrieve the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of transfer requests.</returns>
        protected override Task<IHttpResult<IEnumerable<VariantRequest>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            var filter = new VariantRequestFilterCriteria
            {
                OwnerId = this.Owner,
            };

            var result = client.VariantRequests.ReadAllByFiltersAsync(context, VariantRequestExpandOptions.None, filter).GetAwaiter().GetResult();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Update or approves or denies a variant request.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PdmsVariantRequest")]
    [OutputType(typeof(VariantRequest))]
    public sealed partial class SetVariantRequestCmdlet : IHttpResultCmdlet<VariantRequest>
    {
        /// <summary>
        /// The variant request object to be updated.
        /// </summary>
        [Parameter(Position = 0, Mandatory = false, ValueFromPipeline = true, ParameterSetName = "Default")]
        public VariantRequest Value { get; set; }

        /// <summary>
        /// Variant Request Id.
        /// </summary>
        [Parameter(Position = 1, Mandatory = false, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        public string Id { get; set; }

        /// <summary>
        /// Action on Variant Request.
        /// </summary>
        [Parameter(Position = 2, Mandatory = false, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        public VariantRequestAction Action { get; set; }

        /// <summary>
        /// Calls the service to update the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<VariantRequest>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.Action == VariantRequestAction.Approve || this.Action == VariantRequestAction.Deny)
            {
                if (this.Id == null)
                {
                    throw new Exception("The Variant Request Id not specified");
                }
            }

            var result = client.VariantRequests.ReadAsync(this.Id, context, VariantRequestExpandOptions.None);
            var request = result.Result.Response;

            if (this.Action == VariantRequestAction.Approve)
            {
                client.VariantRequests.ApproveAsync(request.Id, request.ETag, context).GetAwaiter().GetResult();
                return result;
            }
            
            if (this.Action == VariantRequestAction.Deny)
            {
                client.VariantRequests.DeleteAsync(request.Id, request.ETag, context).GetAwaiter().GetResult();
                return result;
            }

            return client.VariantRequests.UpdateAsync(this.Value, context);
        }
    }
}