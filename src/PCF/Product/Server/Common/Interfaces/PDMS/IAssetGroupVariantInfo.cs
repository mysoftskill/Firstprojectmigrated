namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Represents a variant on an asset group.
    /// </summary>
    public interface IAssetGroupVariantInfo
    {
        /// <summary>
        /// The variant's ID.
        /// </summary>
        VariantId VariantId { get; }

        /// <summary>
        /// The associated asset group's ID.
        /// </summary>
        AssetGroupId AssetGroupId { get; }

        /// <summary>
        /// The asset group qualifier.
        /// </summary>
        string AssetGroupQualifier { get; }
        
        /// <summary>
        /// The name of the variant.
        /// </summary>
        string VariantName { get; }

        /// <summary>
        /// The description of the variant.
        /// </summary>
        string VariantDescription { get; }

        /// <summary>
        /// If true, we always add it to the variant list that is applied by agent
        /// and pcf never applies this
        /// </summary>
        bool IsAgentApplied { get; }

        /// <summary>
        /// Enumeration of the data type Ids of applicable DataTypes
        /// </summary>
        IEnumerable<DataTypeId> ApplicableDataTypeIds { get; }

        /// <summary>
        /// Enumeration of the applicable subject types
        /// </summary>
        IEnumerable<SubjectType> ApplicableSubjectTypes { get; }

        /// <summary>
        /// PrivacyCommandTypes that are applicable
        /// </summary>
        IEnumerable<PrivacyCommandType> ApplicableCapabilities { get; }

        /// <summary>
        /// Evaluates whether this variant is applicable to the given command.
        /// </summary>
        bool IsApplicableToCommand(PrivacyCommand command, bool isPcfAppliedVariant);
    }
}
