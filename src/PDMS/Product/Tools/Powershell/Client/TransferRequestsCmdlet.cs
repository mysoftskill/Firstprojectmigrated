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
    /// Transfer Request Actions.
    /// </summary>
    public enum TransferRequestAction
    {
        /// <summary>
        /// Approve Transfer Request.
        /// </summary>
        Approve = 0,

        /// <summary>
        /// Deny Transfer Request.
        /// </summary>
        Deny
    }

    /// <summary>
    /// Create a new transfer request.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsTransferRequest")]
    [OutputType(typeof(TransferRequest))]
    public partial class NewTransferRequestCmdlet : IHttpResultCmdlet<TransferRequest>
    {
        /// <summary>
        /// Specify source owner.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public string SourceOwner { get; set; }

        /// <summary>
        /// Specify Target Owner.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public string TargetOwner { get; set; }

        /// <summary>
        /// Specify semi-colon separated list of asset groups.
        /// </summary>
        [Parameter(Position = 2, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public string AssetGroups { get; set; }

        /// <summary>
        /// Calls the service to create a new transfer request.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The newly created transfer request.</returns>
        protected override Task<IHttpResult<TransferRequest>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            string[] assetGroups = this.AssetGroups.Split(';');

            var newtr = new TransferRequest
            {
                SourceOwnerId = this.SourceOwner,
                TargetOwnerId = this.TargetOwner,
                AssetGroups = assetGroups
            };

            return client.TransferRequests.CreateAsync(newtr, context);
        }
    }

    /// <summary>
    /// Get the list of transfer requests.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PdmsTransferRequest")]
    [OutputType(typeof(TransferRequest))]
    public partial class GetTransferRequestCmdlet : IHttpResultCmdlet<IEnumerable<TransferRequest>>
    {
        /// <summary>
        /// Filter Transfer Requests by Source Owner.
        /// </summary>
        [Parameter(Position = 0, Mandatory = false, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        public string SourceOwner { get; set; }

        /// <summary>
        /// Filter Transfer Requests by Target Owner.
        /// </summary>
        [Parameter(Position = 1, Mandatory = false, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        public string TargetOwner { get; set; }

        /// <summary>
        /// Calls the service to retrieve the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of transfer requests.</returns>
        protected override Task<IHttpResult<IEnumerable<TransferRequest>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            var filter = new TransferRequestFilterCriteria
            {
                SourceOwnerId = this.SourceOwner,
                TargetOwnerId = this.TargetOwner
            };

            var result = client.TransferRequests.ReadAllByFiltersAsync(context, TransferRequestExpandOptions.None, filter).GetAwaiter().GetResult();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Approve or deny transfer request.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PdmsTransferRequest")]
    [OutputType(typeof(TransferRequest))]
    public partial class SetTransferRequestCmdlet : IHttpResultCmdlet
    {
        /// <summary>
        /// Transfer Request Id.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        public string Id { get; set; }

        /// <summary>
        /// Action on Variant Request.
        /// </summary>
        [Parameter(Position = 1, Mandatory = false, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        public TransferRequestAction Action { get; set; }

        /// <summary>
        /// Calls the service to approve or deny the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of transfer requests.</returns>
        protected override Task<IHttpResult> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            var result = client.TransferRequests.ReadAsync(this.Id, context, TransferRequestExpandOptions.None);
            var transferRequest = result.Result.Response;

            if (this.Action == TransferRequestAction.Approve)
            {
                return client.TransferRequests.ApproveAsync(transferRequest.Id, transferRequest.ETag, context);
            }
            else
            {
                return client.TransferRequests.DeleteAsync(transferRequest.Id, transferRequest.ETag, context);
            }
        }
    }

    /// <summary>
    /// Delete transfer request.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PdmsTransferRequest")]
    [OutputType(typeof(TransferRequest))]
    public partial class RemoveTransferRequestCmdlet : IHttpResultCmdlet
    {
        /// <summary>
        /// Filter Transfer Requests by Source Owner.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        public string Id { get; set; }

        /// <summary>
        /// Calls the service to retrieve the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of transfer requests.</returns>
        protected override Task<IHttpResult> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            var result = client.TransferRequests.ReadAsync(this.Id, context, TransferRequestExpandOptions.None);
            var transferRequest = result.Result.Response;
            if (transferRequest == null)
            {
                throw new Exception("The Transfer Request with specified Id could not be found");
            }

            return client.TransferRequests.DeleteAsync(transferRequest.Id, transferRequest.ETag, context);
        }
    }
}