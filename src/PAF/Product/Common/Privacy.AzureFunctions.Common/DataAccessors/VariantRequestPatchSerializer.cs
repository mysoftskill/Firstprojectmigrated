namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.VisualStudio.Services.WebApi.Patch;
    using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

    /// <summary>
    /// Produces patch documents for VariantRequestWorkItems
    /// </summary>
    public class VariantRequestPatchSerializer : IVariantRequestPatchSerializer
    {
        private const string TableStyle = @"style=""width:100%; border-collapse: collapse; text-align:left;""";
        private const string TCellStyle = @"style=""border: 1px solid black;""";

        private static readonly string[] ValidStates =
            {
                "New",
                "Active",
                "GC Approved",
                "CELA Approved",
                "GC on behalf of CELA Approved",
                "Approved",
                "Rejected",
                "Removed"
            };

        private static readonly string[] ValidFields =
            {
                "System.Id",
                "System.Title",
                "System.Description",
                "System.State",
                "System.AssignedTo",
                "Custom.ListofVariantsDescrip",
                "Custom.ListofAssetsGroups",
                "Custom.VariantRequestId",
                "Custom.GeneralContractorAlias",
                "Custom.CELAContactAlias",
                "Custom.RequesterAlias"
            };

        /// <inheritdoc/>
        public JsonPatchDocument UpdateVariantRequestPatchDocument(Dictionary<string, string> fieldsToUpdate)
        {
            if (this.InvalidFields(fieldsToUpdate))
            {
                throw new ArgumentException("The field must match the valid field");
            }

            JsonPatchDocument patchdocument = new JsonPatchDocument();
            foreach (var fieldToUpdate in fieldsToUpdate)
            {
                patchdocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = $"/fields/{fieldToUpdate.Key}",
                        Value = fieldToUpdate.Value
                    });
            }

            return patchdocument;
        }

        /// <inheritdoc/>
        public JsonPatchDocument CreateVariantRequestPatchDocument(VariantRequestWorkItem workItem)
        {
            JsonPatchDocument patchDocument = new JsonPatchDocument();
            Dictionary<string, string> workItemFields = new Dictionary<string, string>()
            {
                { "/fields/System.Title", workItem.WorkItemTitle },
                { "/fields/System.Description", workItem.WorkItemDescription },
                { "/fields/System.State", "New" },
                { "/fields/Custom.ListofVariantsDescrip", ConvertListOfVariantsToHtml(workItem.Variants) },
                { "/fields/Custom.ListofAssetGroups", ConvertListOfAssetGroupsToHtml(workItem.AssetGroups) },
                { "/fields/Custom.VariantRequestId", workItem.VariantRequestId },
                { "/fields/Custom.GeneralContractorAlias", workItem.GeneralContractorAlias },
                { "/fields/Custom.CELAContactAlias", workItem.CelaContactAlias },
                { "/fields/Custom.RequesterAlias", workItem.RequesterAlias }
            };
            foreach (var field in workItemFields)
            {
                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = field.Key,
                        Value = field.Value
                    });
            }

            return patchDocument;
        }

        /// <summary>
        /// Generate a table containing the PDMS Variant Id and Name, with the EGRC Id and Name for the
        /// Variants in the request.
        /// </summary>
        /// <param name="variants">The variants to format.</param>
        /// <returns>returns an html string</returns>
        private static string ConvertListOfVariantsToHtml(IEnumerable<ExtendedAssetGroupVariant> variants)
        {
            if (variants != null)
            {
                StringBuilder sb = new StringBuilder($@"<table {TableStyle}><tr><th {TCellStyle}>Variant Id</th><th {TCellStyle}>Variant Name</th><th {TCellStyle}>EGRC Id</th><th {TCellStyle}>EGRC Name</th><th {TCellStyle}>Data Types</th><th {TCellStyle}>Data Subjects</th><th {TCellStyle}>Capabilities</th></tr>");
                IEnumerable<string> placeHolder = new List<string>() { string.Empty }.AsEnumerable<string>();
                foreach (var variant in variants)
                {
                     sb.Append($@"<tr><td {TCellStyle}>{variant.VariantId}</td><td {TCellStyle}>{variant.VariantName}</td><td {TCellStyle}>{variant.EgrcId}</td><td {TCellStyle}>{variant.EgrcName}</td><td {TCellStyle}>{string.Join(",", variant.DataTypes ?? placeHolder)}</td><td {TCellStyle}>{string.Join(",", variant.SubjectTypes ?? placeHolder)}</td><td {TCellStyle}>{string.Join(",", variant.Capabilities ?? placeHolder)}</td></tr>");
                }

                sb.Append("</table>");

                return sb.ToString();
            }

            return "<div>Variant list is empty.<div>";
        }

        /// <summary>
        /// Generate a table containing the AssetGroup Id and Qualifier for the
        /// Asset Groups in the request.
        /// </summary>
        /// <param name="assetGroups">The asset groups to format.</param>
        /// <returns>returns an html string</returns>
        private static string ConvertListOfAssetGroupsToHtml(IEnumerable<VariantRelationship> assetGroups)
        {
            if (assetGroups != null)
            {
                StringBuilder sb = new StringBuilder($"<table {TableStyle}><tr><th {TCellStyle}>Asset Group Id</th><th {TCellStyle}>Asset Group Qualifier</th></tr>");

                foreach (var assetGroup in assetGroups)
                {
                    sb.Append($"<tr><td {TCellStyle}>{assetGroup.AssetGroupId}</td><td {TCellStyle}>{assetGroup.AssetQualifier}</td></tr>");
                }

                sb.Append("</table>");

                return sb.ToString();
            }

            return "<div>Asset group list is empty.<div>";
        }

        /// <summary>
        /// Determines if the fields are invalid
        /// </summary>
        /// <param name="fieldsToUpdate">Fields to check</param>
        /// <returns>returns true if the field is invalid</returns>
        private bool InvalidFields(Dictionary<string, string> fieldsToUpdate)
        {
            foreach (var field in fieldsToUpdate)
            {
                // The field does not match a valid field
                if (!ValidFields.Contains(field.Key))
                {
                    return true;
                }

                // Tries to change into an invalid state
                if (field.Key == "System.State" && !ValidStates.Contains(field.Value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
