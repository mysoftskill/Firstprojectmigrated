namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

    /// <summary>
    /// Produces patch documents for serializing
    /// </summary>
    public interface IVariantRequestPatchSerializer
    {
        /// <summary>
        /// Creates an Update patch document
        /// </summary>
        /// <param name="fieldsToUpdate">lists fields to update and new values</param>
        /// <returns>Update Patch document</returns>
        JsonPatchDocument UpdateVariantRequestPatchDocument(Dictionary<string, string> fieldsToUpdate);

        /// <summary>
        /// Creates an Add patch document
        /// </summary>
        /// <param name="workItem">Workitem to be formatted into a patch document</param>
        /// <returns>Add Patch document</returns>
        JsonPatchDocument CreateVariantRequestPatchDocument(VariantRequestWorkItem workItem);
    }
}
