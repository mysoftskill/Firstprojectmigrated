namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    /// <summary>
    /// Exposes the available APIs for the service.
    /// </summary>
    public interface IDataManagementClient
    {
        /// <summary>
        /// Gets the data owner client.
        /// </summary>
        IDataOwnerClient DataOwners { get; }

        /// <summary>
        /// Gets the asset group client.
        /// </summary>
        IAssetGroupClient AssetGroups { get; }

        /// <summary>
        /// Gets the variant definition client.
        /// </summary>
        IVariantDefinitionClient VariantDefinitions { get; }

        /// <summary>
        /// Gets the data agent client.
        /// </summary>
        IDataAgentClient DataAgents { get; }

        /// <summary>
        /// Gets the data asset client.
        /// </summary>
        IDataAssetClient DataAssets { get; }

        /// <summary>
        /// Gets the user client.
        /// </summary>
        IUserClient Users { get; }

        /// <summary>
        /// Gets the history item client.
        /// </summary>
        IHistoryItemClient HistoryItems { get; }

        /// <summary>
        /// Gets the sharing request client.
        /// </summary>
        ISharingRequestClient SharingRequests { get; }

        /// <summary>
        /// Gets the variant request client.
        /// </summary>
        IVariantRequestClient VariantRequests { get; }

        /// <summary>
        /// Gets the incident client.
        /// </summary>
        IIncidentClient Incidents { get; }

        /// <summary>
        /// Gets the transfer request client.
        /// </summary>
        ITransferRequestClient TransferRequests { get; }
    }
}