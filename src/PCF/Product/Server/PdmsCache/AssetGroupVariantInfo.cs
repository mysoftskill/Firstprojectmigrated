namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Asset group variant schema
    /// </summary>
    public sealed class AssetGroupVariantInfo : IAssetGroupVariantInfo
    {
        private readonly HashSet<PdmsSubjectType> pdmsSubjectTypes;

        public AssetGroupVariantInfo(AssetGroupVariantInfoDocument document, bool enableTolerantParsing)
        {
            this.AssetGroupId = document.AssetGroupId;
            this.VariantId = document.VariantId;
            this.VariantName = document.VariantName;
            this.VariantDescription = document.VariantDescription;
            this.IsAgentApplied = document.IsAgentApplied;
            this.AssetGroupQualifier = document.AssetGroupQualifier;
            this.ApplicableAssetQualifier = AssetQualifier.Parse(this.AssetGroupQualifier);

            List<DataTypeId> dataTypes = new List<DataTypeId>();
            List<Common.SubjectType> subjectTypes = new List<Common.SubjectType>();
            List<PrivacyCommandType> capabilities = new List<PrivacyCommandType>();
            this.pdmsSubjectTypes = new HashSet<PdmsSubjectType>();

            PdmsInfoParser.ParseDataTypes(document.DataTypes ?? new string[0], dataTypes, enableTolerantParsing);
            PdmsInfoParser.ParseCapabilities(document.Capabilities ?? new string[0], capabilities, enableTolerantParsing);
            PdmsInfoParser.ParseSubjects(document.SubjectTypes, this.pdmsSubjectTypes, enableTolerantParsing);
            PdmsInfoParser.ParsePcfSubjects(this.pdmsSubjectTypes, subjectTypes, enableTolerantParsing);

            this.ApplicableDataTypeIds = dataTypes;
            this.ApplicableCapabilities = capabilities;
            this.ApplicableSubjectTypes = subjectTypes;
        }

        /// <inheritdoc/>
        public AssetGroupId AssetGroupId { get; }

        /// <inheritdoc/>
        public VariantId VariantId { get; }

        /// <inheritdoc/>
        public string AssetGroupQualifier { get; }

        /// <inheritdoc/>
        public string VariantName { get; }

        /// <inheritdoc/>
        public string VariantDescription { get; }

        /// <inheritdoc/>
        public bool IsAgentApplied { get; }

        /// <summary>
        /// AssetQualifier parsed from the AssetGroupQualifier string
        /// </summary>
        public AssetQualifier ApplicableAssetQualifier { get; }

        /// <inheritdoc/>
        public IEnumerable<DataTypeId> ApplicableDataTypeIds { get; }

        /// <inheritdoc/>
        public IEnumerable<Common.SubjectType> ApplicableSubjectTypes { get; }

        /// <inheritdoc/>
        public IEnumerable<PrivacyCommandType> ApplicableCapabilities { get; }

        /// <inheritdoc/>
        public bool IsApplicableToCommand(PrivacyCommand command, bool isPcfAppliedVariant)
        {
            PdmsSubjectType commandSubjectType = PdmsInfoParser.GetPdmsSubjectFromPrivacySubject(command.Subject);

            if (this.pdmsSubjectTypes?.Any() == true && !this.pdmsSubjectTypes.Contains(commandSubjectType))
            {
                return false;
            }

            if (this.ApplicableCapabilities?.Any() == true && !this.ApplicableCapabilities.Contains(command.CommandType))
            {
                return false;
            }

            if (this.ApplicableDataTypeIds?.Any() == true)
            {
                // For account close, 
                // if don't suppress in PCF but annotate the command with the variant 
                // TODO need to fix this to convert the pcf variant into an agent variant
                if (command.CommandType == PrivacyCommandType.AccountClose)
                {
                    return !isPcfAppliedVariant;
                }

                // If isPcfAppliedVariant, then 
                //   if all the datatypes in the command are covered by the variant - suppress the command
                //   else, remove the datatypes that are covered by the variant from the command
                // else
                //    true if at least one datatype is covered by the variant
                if (!this.ApplicableDataTypeIds.Any(dt => command.DataTypeIds.Contains(dt)))
                {
                    return false;
                }

                if (!isPcfAppliedVariant)
                {
                    return true;
                }

                // exclude this.ApplicableDataTypeIds from command data types
                command.DataTypeIds = command.DataTypeIds.Except(this.ApplicableDataTypeIds);

                if (!command.DataTypeIds.Any())
                {
                    return true;
                }

                return false;
            }

            return true;
        }
    }
}
