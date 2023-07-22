[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Generated code.")]

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Generated code.")]

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    #region DataOwner
    /// <summary>
    /// Issues a create call against the service.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsDataOwner")]
    [OutputType(typeof(DataOwner))]
    public sealed partial class NewDataOwnerCmdlet : IHttpResultCmdlet<DataOwner>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public DataOwner Value { get; set; }

        /// <summary>
        /// Calls the service to create the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<DataOwner>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.DataOwners.CreateAsync(this.Value, context);
        }
    }
    
    /// <summary>
    /// Issues a get (by id) call to the serivce.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PdmsDataOwner")]
    [OutputType(typeof(DataOwner))]
    public sealed partial class GetDataOwnerCmdlet : IHttpResultCmdlet<DataOwner>
    {
        /// <summary>
        /// The id of the object to retrieve.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public string Id { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public DataOwnerExpandOptions Expand { get; set; }

        /// <summary>
        /// Calls the service to retrieve the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object from the service.</returns>
        protected override Task<IHttpResult<DataOwner>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.DataOwners.ReadAsync(this.Id, context, this.Expand);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PdmsDataOwner")]
    [OutputType(typeof(DataOwner))]
    public sealed partial class SetDataOwnerCmdlet : IHttpResultCmdlet<DataOwner>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public DataOwner Value { get; set; }

        /// <summary>
        /// Calls the service to update the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<DataOwner>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.DataOwners.UpdateAsync(this.Value, context);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PdmsDataOwner")]
    [OutputType(typeof(DataOwner))]
    public sealed partial class RemoveDataOwnerCmdlet : IHttpResultCmdlet
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public DataOwner Value { get; set; }

        /// <summary>
        /// Calls the service to delete the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>No result.</returns>
        protected override Task<IHttpResult> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.DataOwners.DeleteAsync(this.Value.Id, this.Value.ETag, context);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "PdmsDataOwners")]
    [OutputType(typeof(DataOwner))]
    public sealed partial class FindDataOwnersCmdlet : IHttpResultCmdlet<IEnumerable<DataOwner>>
    {
        /// <summary>
        /// The filter critieria to use when searching for data in the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public DataOwnerFilterCriteria Filter { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public DataOwnerExpandOptions Expand { get; set; }

        /// <summary>
        /// Set this if you want the command to page through all of the results automatically.
        /// Without this, a single page of results will be returned. 
        /// We do this to preserve the service from extremely expensive queries that are issued accidentally.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        /// <summary>
        /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of objects.</returns>
        protected override async Task<IHttpResult<IEnumerable<DataOwner>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.Recurse.IsPresent)
            {
                var result = await client.DataOwners.ReadAllByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);
                return result;
            }
            else
            {
                var result = await client.DataOwners.ReadByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);

                if (result.Response.NextLink != null)
                {
                    this.WarningMessage = "Multiple pages of data found. Use -Recurse to automatically retrieve all pages. Only the first page of data has been added to the Powershell pipeline.";
                }

                return result.Convert(x => x.Response.Value, 2);
            }
        }
    }
    #endregion

    #region AssetGroup
    /// <summary>
    /// Issues a create call against the service.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsAssetGroup")]
    [OutputType(typeof(AssetGroup))]
    public sealed partial class NewAssetGroupCmdlet : IHttpResultCmdlet<AssetGroup>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public AssetGroup Value { get; set; }

        /// <summary>
        /// Calls the service to create the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<AssetGroup>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.AssetGroups.CreateAsync(this.Value, context);
        }
    }
    
    /// <summary>
    /// Issues a get (by id) call to the serivce.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PdmsAssetGroup")]
    [OutputType(typeof(AssetGroup))]
    public sealed partial class GetAssetGroupCmdlet : IHttpResultCmdlet<AssetGroup>
    {
        /// <summary>
        /// The id of the object to retrieve.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public string Id { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public AssetGroupExpandOptions Expand { get; set; }

        /// <summary>
        /// Calls the service to retrieve the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object from the service.</returns>
        protected override Task<IHttpResult<AssetGroup>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.AssetGroups.ReadAsync(this.Id, context, this.Expand);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PdmsAssetGroup")]
    [OutputType(typeof(AssetGroup))]
    public sealed partial class SetAssetGroupCmdlet : IHttpResultCmdlet<AssetGroup>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public AssetGroup Value { get; set; }

        /// <summary>
        /// Calls the service to update the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<AssetGroup>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.AssetGroups.UpdateAsync(this.Value, context);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PdmsAssetGroup")]
    [OutputType(typeof(AssetGroup))]
    public sealed partial class RemoveAssetGroupCmdlet : IHttpResultCmdlet
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public AssetGroup Value { get; set; }

        /// <summary>
        /// Calls the service to delete the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>No result.</returns>
        protected override Task<IHttpResult> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.AssetGroups.DeleteAsync(this.Value.Id, this.Value.ETag, context);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "PdmsAssetGroups")]
    [OutputType(typeof(AssetGroup))]
    public sealed partial class FindAssetGroupsCmdlet : IHttpResultCmdlet<IEnumerable<AssetGroup>>
    {
        /// <summary>
        /// The filter critieria to use when searching for data in the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public AssetGroupFilterCriteria Filter { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public AssetGroupExpandOptions Expand { get; set; }

        /// <summary>
        /// Set this if you want the command to page through all of the results automatically.
        /// Without this, a single page of results will be returned. 
        /// We do this to preserve the service from extremely expensive queries that are issued accidentally.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        /// <summary>
        /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of objects.</returns>
        protected override async Task<IHttpResult<IEnumerable<AssetGroup>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.Recurse.IsPresent)
            {
                var result = await client.AssetGroups.ReadAllByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);
                return result;
            }
            else
            {
                var result = await client.AssetGroups.ReadByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);

                if (result.Response.NextLink != null)
                {
                    this.WarningMessage = "Multiple pages of data found. Use -Recurse to automatically retrieve all pages. Only the first page of data has been added to the Powershell pipeline.";
                }

                return result.Convert(x => x.Response.Value, 2);
            }
        }
    }
    #endregion

    #region VariantDefinition
    /// <summary>
    /// Issues a create call against the service.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsVariantDefinition")]
    [OutputType(typeof(VariantDefinition))]
    public sealed partial class NewVariantDefinitionCmdlet : IHttpResultCmdlet<VariantDefinition>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public VariantDefinition Value { get; set; }

        /// <summary>
        /// Calls the service to create the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<VariantDefinition>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.VariantDefinitions.CreateAsync(this.Value, context);
        }
    }
    
    /// <summary>
    /// Issues a get (by id) call to the serivce.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PdmsVariantDefinition")]
    [OutputType(typeof(VariantDefinition))]
    public sealed partial class GetVariantDefinitionCmdlet : IHttpResultCmdlet<VariantDefinition>
    {
        /// <summary>
        /// The id of the object to retrieve.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public string Id { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public VariantDefinitionExpandOptions Expand { get; set; }

        /// <summary>
        /// Calls the service to retrieve the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object from the service.</returns>
        protected override Task<IHttpResult<VariantDefinition>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.VariantDefinitions.ReadAsync(this.Id, context, this.Expand);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PdmsVariantDefinition")]
    [OutputType(typeof(VariantDefinition))]
    public sealed partial class SetVariantDefinitionCmdlet : IHttpResultCmdlet<VariantDefinition>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public VariantDefinition Value { get; set; }

        /// <summary>
        /// Calls the service to update the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<VariantDefinition>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.VariantDefinitions.UpdateAsync(this.Value, context);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "PdmsVariantDefinitions")]
    [OutputType(typeof(VariantDefinition))]
    public sealed partial class FindVariantDefinitionsCmdlet : IHttpResultCmdlet<IEnumerable<VariantDefinition>>
    {
        /// <summary>
        /// The filter critieria to use when searching for data in the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public VariantDefinitionFilterCriteria Filter { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public VariantDefinitionExpandOptions Expand { get; set; }

        /// <summary>
        /// Set this if you want the command to page through all of the results automatically.
        /// Without this, a single page of results will be returned. 
        /// We do this to preserve the service from extremely expensive queries that are issued accidentally.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        /// <summary>
        /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of objects.</returns>
        protected override async Task<IHttpResult<IEnumerable<VariantDefinition>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.Recurse.IsPresent)
            {
                var result = await client.VariantDefinitions.ReadAllByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);
                return result;
            }
            else
            {
                var result = await client.VariantDefinitions.ReadByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);

                if (result.Response.NextLink != null)
                {
                    this.WarningMessage = "Multiple pages of data found. Use -Recurse to automatically retrieve all pages. Only the first page of data has been added to the Powershell pipeline.";
                }

                return result.Convert(x => x.Response.Value, 2);
            }
        }
    }

    /// <summary>
    /// Issues a delete call to the serivce with an optional 'force' option.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PdmsVariantDefinition")]
    [OutputType(typeof(VariantDefinition))]
    public sealed partial class RemoveVariantDefinitionCmdlet : IHttpResultCmdlet
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public VariantDefinition Value { get; set; }

        /// <summary>
        /// Set this will delink dependent entites and delete will always succeed.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Calls the service to delete the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>No result.</returns>
        protected override Task<IHttpResult> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.VariantDefinitions.DeleteAsync(this.Value.Id, this.Value.ETag, context, this.Force.IsPresent);
        }
    }
    #endregion

    #region DeleteAgent
    /// <summary>
    /// Issues a create call against the service.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsDeleteAgent")]
    [OutputType(typeof(DeleteAgent))]
    public sealed partial class NewDeleteAgentCmdlet : IHttpResultCmdlet<DeleteAgent>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public DeleteAgent Value { get; set; }

        /// <summary>
        /// Calls the service to create the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<DeleteAgent>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.DataAgents.CreateAsync(this.Value, context);
        }
    }
    
    /// <summary>
    /// Issues a get (by id) call to the serivce.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PdmsDeleteAgent")]
    [OutputType(typeof(DeleteAgent))]
    public sealed partial class GetDeleteAgentCmdlet : IHttpResultCmdlet<DeleteAgent>
    {
        /// <summary>
        /// The id of the object to retrieve.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public string Id { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public DataAgentExpandOptions Expand { get; set; }

        /// <summary>
        /// Calls the service to retrieve the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object from the service.</returns>
        protected override Task<IHttpResult<DeleteAgent>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.DataAgents.ReadAsync<DeleteAgent>(this.Id, context, this.Expand);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PdmsDeleteAgent")]
    [OutputType(typeof(DeleteAgent))]
    public sealed partial class SetDeleteAgentCmdlet : IHttpResultCmdlet<DeleteAgent>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public DeleteAgent Value { get; set; }

        /// <summary>
        /// Calls the service to update the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<DeleteAgent>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.DataAgents.UpdateAsync(this.Value, context);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PdmsDeleteAgent")]
    [OutputType(typeof(DeleteAgent))]
    public sealed partial class RemoveDeleteAgentCmdlet : IHttpResultCmdlet
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public DeleteAgent Value { get; set; }

        /// <summary>
        /// Override pending commands check. If set to true pending commands check is not done. 
        /// </summary>
        [Parameter(Position = 1, Mandatory = false, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public bool OverridePendingCommandsCheck { get; set; }

        /// <summary>
        /// Calls the service to delete the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>No result.</returns>
        protected override Task<IHttpResult> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.DataAgents.DeleteAsync(this.Value.Id, this.Value.ETag, context, this.OverridePendingCommandsCheck);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "PdmsDeleteAgents")]
    [OutputType(typeof(DeleteAgent))]
    public sealed partial class FindDeleteAgentsCmdlet : IHttpResultCmdlet<IEnumerable<DeleteAgent>>
    {
        /// <summary>
        /// The filter critieria to use when searching for data in the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public DeleteAgentFilterCriteria Filter { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public DataAgentExpandOptions Expand { get; set; }

        /// <summary>
        /// Set this if you want the command to page through all of the results automatically.
        /// Without this, a single page of results will be returned. 
        /// We do this to preserve the service from extremely expensive queries that are issued accidentally.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        /// <summary>
        /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of objects.</returns>
        protected override async Task<IHttpResult<IEnumerable<DeleteAgent>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.Recurse.IsPresent)
            {
                var result = await client.DataAgents.ReadAllByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);
                return result;
            }
            else
            {
                var result = await client.DataAgents.ReadByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);

                if (result.Response.NextLink != null)
                {
                    this.WarningMessage = "Multiple pages of data found. Use -Recurse to automatically retrieve all pages. Only the first page of data has been added to the Powershell pipeline.";
                }

                return result.Convert(x => x.Response.Value, 2);
            }
        }
    }
    #endregion
    
    #region SharingRequest
    /// <summary>
    /// Issues a get (by id) call to the serivce.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PdmsSharingRequest")]
    [OutputType(typeof(SharingRequest))]
    public sealed partial class GetSharingRequestCmdlet : IHttpResultCmdlet<SharingRequest>
    {
        /// <summary>
        /// The id of the object to retrieve.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public string Id { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public SharingRequestExpandOptions Expand { get; set; }

        /// <summary>
        /// Calls the service to retrieve the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object from the service.</returns>
        protected override Task<IHttpResult<SharingRequest>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.SharingRequests.ReadAsync(this.Id, context, this.Expand);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PdmsSharingRequest")]
    [OutputType(typeof(SharingRequest))]
    public sealed partial class RemoveSharingRequestCmdlet : IHttpResultCmdlet
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public SharingRequest Value { get; set; }

        /// <summary>
        /// Calls the service to delete the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>No result.</returns>
        protected override Task<IHttpResult> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.SharingRequests.DeleteAsync(this.Value.Id, this.Value.ETag, context);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "PdmsSharingRequests")]
    [OutputType(typeof(SharingRequest))]
    public sealed partial class FindSharingRequestsCmdlet : IHttpResultCmdlet<IEnumerable<SharingRequest>>
    {
        /// <summary>
        /// The filter critieria to use when searching for data in the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public SharingRequestFilterCriteria Filter { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public SharingRequestExpandOptions Expand { get; set; }

        /// <summary>
        /// Set this if you want the command to page through all of the results automatically.
        /// Without this, a single page of results will be returned. 
        /// We do this to preserve the service from extremely expensive queries that are issued accidentally.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        /// <summary>
        /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of objects.</returns>
        protected override async Task<IHttpResult<IEnumerable<SharingRequest>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.Recurse.IsPresent)
            {
                var result = await client.SharingRequests.ReadAllByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);
                return result;
            }
            else
            {
                var result = await client.SharingRequests.ReadByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);

                if (result.Response.NextLink != null)
                {
                    this.WarningMessage = "Multiple pages of data found. Use -Recurse to automatically retrieve all pages. Only the first page of data has been added to the Powershell pipeline.";
                }

                return result.Convert(x => x.Response.Value, 2);
            }
        }
    }
    #endregion

    #region VariantRequest
    /// <summary>
    /// Issues a create call against the service.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsVariantRequest")]
    [OutputType(typeof(VariantRequest))]
    public sealed partial class NewVariantRequestCmdlet : IHttpResultCmdlet<VariantRequest>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public VariantRequest Value { get; set; }

        /// <summary>
        /// Calls the service to create the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<VariantRequest>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.VariantRequests.CreateAsync(this.Value, context);
        }
    }

    /// <summary>
    /// Issues a get (by id) call to the serivce.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PdmsVariantRequest")]
    [OutputType(typeof(VariantRequest))]
    public partial class GetVariantRequestCmdlet : IHttpResultCmdlet<VariantRequest>
    {
        /// <summary>
        /// The id of the object to retrieve.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public string Id { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public VariantRequestExpandOptions Expand { get; set; }

        /// <summary>
        /// Calls the service to retrieve the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object from the service.</returns>
        protected override Task<IHttpResult<VariantRequest>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.VariantRequests.ReadAsync(this.Id, context, this.Expand);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PdmsVariantRequest")]
    [OutputType(typeof(VariantRequest))]
    public sealed partial class RemoveVariantRequestCmdlet : IHttpResultCmdlet
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public VariantRequest Value { get; set; }

        /// <summary>
        /// Calls the service to delete the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>No result.</returns>
        protected override Task<IHttpResult> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.VariantRequests.DeleteAsync(this.Value.Id, this.Value.ETag, context);
        }
    }

    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "PdmsVariantRequests")]
    [OutputType(typeof(VariantRequest))]
    public sealed partial class FindVariantRequestsCmdlet : IHttpResultCmdlet<IEnumerable<VariantRequest>>
    {
        /// <summary>
        /// The filter critieria to use when searching for data in the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = false, ValueFromPipeline = true, ParameterSetName = "Default")]
        public VariantRequestFilterCriteria Filter { get; set; }

        /// <summary>
        /// Any expand options to apply when retrieving the object.
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public VariantRequestExpandOptions Expand { get; set; }

        /// <summary>
        /// Set this if you want the command to page through all of the results automatically.
        /// Without this, a single page of results will be returned. 
        /// We do this to preserve the service from extremely expensive queries that are issued accidentally.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        /// <summary>
        /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of objects.</returns>
        protected override async Task<IHttpResult<IEnumerable<VariantRequest>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.Recurse.IsPresent)
            {
                var result = await client.VariantRequests.ReadAllByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);
                return result;
            }
            else
            {
                var result = await client.VariantRequests.ReadByFiltersAsync(context, this.Expand, this.Filter).ConfigureAwait(false);

                if (result.Response.NextLink != null)
                {
                    this.WarningMessage = "Multiple pages of data found. Use -Recurse to automatically retrieve all pages. Only the first page of data has been added to the Powershell pipeline.";
                }

                return result.Convert(x => x.Response.Value, 2);
            }
        }
    }
    #endregion

    #region HistoryItem
    /// <summary>
    /// Provide a description of this class.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "PdmsHistoryItems")]
    [OutputType(typeof(HistoryItem))]
    public sealed partial class FindHistoryItemsCmdlet : IHttpResultCmdlet<IEnumerable<HistoryItem>>
    {
        /// <summary>
        /// The filter critieria to use when searching for data in the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public HistoryItemFilterCriteria Filter { get; set; }

        /// <summary>
        /// Set this if you want the command to page through all of the results automatically.
        /// Without this, a single page of results will be returned. 
        /// We do this to preserve the service from extremely expensive queries that are issued accidentally.
        /// </summary>
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        /// <summary>
        /// Calls the service to retrieve the objects based on the given filter (or all objects if no filter is provided).
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The list of objects.</returns>
        protected override async Task<IHttpResult<IEnumerable<HistoryItem>>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            if (this.Recurse.IsPresent)
            {
                var result = await client.HistoryItems.ReadAllByFiltersAsync(context, this.Filter).ConfigureAwait(false);
                return result;
            }
            else
            {
                var result = await client.HistoryItems.ReadByFiltersAsync(context, this.Filter).ConfigureAwait(false);

                if (result.Response.NextLink != null)
                {
                    this.WarningMessage = "Multiple pages of data found. Use -Recurse to automatically retrieve all pages. Only the first page of data has been added to the Powershell pipeline.";
                }

                return result.Convert(x => x.Response.Value, 2);
            }
        }
    }
    #endregion

    #region Incident
    /// <summary>
    /// Issues a create call against the service.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsIncident")]
    [OutputType(typeof(Incident))]
    public sealed partial class NewIncidentCmdlet : IHttpResultCmdlet<Incident>
    {
        /// <summary>
        /// The input parameter to this cmdlet. This is the full object to pass to the service.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Default")]
        [ValidateNotNull]
        public Incident Value { get; set; }

        /// <summary>
        /// Calls the service to create the object.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The object with service generated properties filled in.</returns>
        protected override Task<IHttpResult<Incident>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            return client.Incidents.CreateAsync(this.Value, context);
        }
    }
    
    #endregion
}
