namespace Microsoft.PrivacyServices.CommandFeed.Service.AuditLog
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Web;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Audit log record schema
    /// </summary>
    public class AuditLogRecord
    {
        private const int CosmosColumnMaxlength = 50000;
        private const string TruncatedPostfix = "[TRUNCATED]...";

        public AuditLogRecord(
            CommandId commandId,
            DateTimeOffset timestamp,
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            AuditLogCommandAction action,
            int rowCount,
            string[] variantsApplied,
            string exceptions,
            PrivacyCommandType commandType,
            string notApplicableReasonCode,
            string assetGroupStreamName,
            string variantStreamName)
        {
            this.CommandId = commandId;
            this.Timestamp = timestamp;
            this.AgentId = agentId;
            this.AssetGroupId = assetGroupId;
            this.AssetGroupQualifier = assetGroupQualifier;
            this.Action = action;
            this.RowCount = rowCount;
            this.VariantsApplied = variantsApplied;
            this.Exceptions = exceptions;
            this.CommandType = commandType;
            this.NotApplicableReasonCode = notApplicableReasonCode;
            this.AssetGroupStreamName = assetGroupStreamName;
            this.VariantStreamName = variantStreamName;
        }

        /// <summary>
        /// Command ID
        /// </summary>
        public CommandId CommandId { get; }

        /// <summary>
        /// Timestamp for the audit log
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Agent ID
        /// </summary>
        public AgentId AgentId { get; }

        /// <summary>
        /// Asset Group Id
        /// </summary>
        public AssetGroupId AssetGroupId { get; }

        /// <summary>
        /// Asset group qualifier
        /// </summary>
        public string AssetGroupQualifier { get; }

        /// <summary>
        /// Command action
        /// </summary>
        public AuditLogCommandAction Action { get; }

        /// <summary>
        /// Impacted rows
        /// </summary>
        public int RowCount { get; }

        /// <summary>
        /// A list of variant Ids that applied to the command
        /// </summary>
        public string[] VariantsApplied { get; }

        /// <summary>
        /// Non-transient failures reported by agent
        /// </summary>
        public string Exceptions { get; }

        /// <summary>
        /// Command Type
        /// </summary>
        public PrivacyCommandType CommandType { get; }

        /// <summary>
        /// Command not applicable reason code
        /// </summary>
        public string NotApplicableReasonCode { get; }

        /// <summary>
        /// The AssetGroup info config cosmos stream name.
        /// </summary>
        public string AssetGroupStreamName { get; }

        /// <summary>
        /// The Variant info config cosmos stream name.
        /// </summary>
        public string VariantStreamName { get; }

        /// <summary>
        /// Convert Audit log record to tab delimited raw format of one cosmos row
        /// </summary>
        public string ToCosmosRawString(int cosmosColumnMaxlength = CosmosColumnMaxlength)
        {
            var sb = new StringBuilder();
            sb.Append($"{this.CommandId.GuidValue:D}\t");
            sb.Append($"{this.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)}\t");
            sb.Append($"{this.AgentId.GuidValue:D}\t");
            sb.Append($"{this.AssetGroupId.GuidValue:D}\t");
            sb.Append($"{HttpUtility.UrlEncode(this.AssetGroupQualifier)}\t");
            sb.Append($"{this.Action.ToString()}\t");
            sb.Append($"{this.RowCount}\t");
            sb.Append($"[");

            string[] variants = this.VariantsApplied;
            if (variants != null && variants.Length > 0)
            {
                for (int i = 0; i < (variants.Length - 1); i++)
                {
                    sb.Append($"\"{variants[i]}\",");
                }

                sb.Append($"\"{variants[variants.Length - 1]}\"");
            }

            sb.Append($"]\t");

            // Truncate exception string if it is too large
            if (this.Exceptions?.Length > cosmosColumnMaxlength)
            {
                sb.Append($"{HttpUtility.UrlEncode(this.Exceptions.Substring(0, cosmosColumnMaxlength))}");
                sb.Append($"{TruncatedPostfix}\t");
            }
            else
            {
                sb.Append($"{HttpUtility.UrlEncode(this.Exceptions)}\t");
            }

            sb.Append($"{this.CommandType.ToString()}\t");
            sb.Append($"{this.NotApplicableReasonCode}\t");
            sb.Append($"{this.AssetGroupStreamName}\t");
            sb.Append($"{this.VariantStreamName}");

            return sb.ToString();
        }
    }
}
