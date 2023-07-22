namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.SignalApplicability;

    /// <summary>
    /// Defines an asset group.
    /// </summary>
    public interface IAssetGroupInfo
    {
        /// <summary>
        /// Assotiated DataAgent Info.
        /// </summary>
        IDataAgentInfo AgentInfo { get; }

        /// <summary>
        /// The ID of the associated data agent.
        /// </summary>
        AgentId AgentId { get; }

        /// <summary>
        /// The ID of the asset group.
        /// </summary>
        AssetGroupId AssetGroupId { get; }

        /// <summary>
        /// Asset group qualifier.
        /// </summary>
        string AssetGroupQualifier { get; }

        /// <summary>
        /// AssetQualifier from AssetGroupQualifier string
        /// </summary>
        AssetQualifier AssetQualifier { get; }

        /// <summary>
        /// Indicates if this asset group is the fake PPE group.
        /// </summary>
        bool IsFakePreProdAssetGroup { get; }

        /// <summary>
        /// The set of data types covered by this asset group.
        /// </summary>
        IEnumerable<DataTypeId> SupportedDataTypes { get; }

        /// <summary>
        /// The set of subject types covered by this asset group.
        /// </summary>
        IEnumerable<SubjectType> SupportedSubjectTypes { get; }

        /// <summary>
        /// Original Subject Types that come in the PDMS stream.
        /// </summary>
        IEnumerable<PdmsSubjectType> PdmsSubjectTypes { get; }

        /// <summary>
        /// True if this AssetGroup was deprecated.
        /// </summary>
        bool IsDeprecated { get; }

        /// <summary>
        /// The set of command types covered by this asset group.
        /// </summary>
        IEnumerable<PrivacyCommandType> SupportedCommandTypes { get; }

        /// <summary>
        /// The set of blanket VariantInfos that this asset group has been approved for
        /// that are applied by Pcf for checking if the commands are actionable
        /// </summary>
        IList<IAssetGroupVariantInfo> VariantInfosAppliedByPcf { get; }

        /// <summary>
        /// The set of partial VariantInfos and blanket variants that this asset group has been approved for
        /// that are passed to the agent for the agent to determine if the command is actionable
        /// </summary>
        IList<IAssetGroupVariantInfo> VariantInfosAppliedByAgents { get; }

        /// <summary>
        /// The set of command cloud instances supported by the agent associated with this asset group.
        /// </summary>
        IEnumerable<CloudInstanceId> SupportedCloudInstances { get; }

        /// <summary>
        /// The cloud deployment location of the agent associated with this asset group.
        /// </summary>
        CloudInstanceId DeploymentLocation { get; }

        /// <summary>
        /// The set of TenantIds that this asset group has been approved for.
        /// </summary>
        IEnumerable<TenantId> TenantIds { get; }

        /// <summary>
        /// Indicates if delink is a valid response code.
        /// </summary>
        bool DelinkApproved { get; }

        /// <summary>
        /// Is the agent ProdReady or still TestInProd
        /// </summary>
        AgentReadinessState AgentReadinessState { get; }

        /// <summary>
        /// Gets or sets the extended properties for the asset. This is a property bag to hold any additional properties for the asset.
        /// </summary>
        IDictionary<string, string> ExtendedProps { get; }

        /// <summary>
        /// Computes whether a command that has these criteria should be processed by this agent for this asset group.
        /// This method also modifies the command properties according to applicability logic.
        /// </summary>
        bool IsCommandActionable(PrivacyCommand command, out ApplicabilityResult applicabilityResult);

        /// <summary>
        /// Verify if AssetGroupInfo is valid.
        /// </summary>
        bool IsValid(out string justification);

        /// <summary>
        /// Gets a value indicating if this AssetGroupInfo supports low priority queue.
        /// </summary>
        bool SupportsLowPriorityQueue { get; }
    }
}
