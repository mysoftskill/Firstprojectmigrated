namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;

    /// <summary>
    /// Defines contracts for calling Pdms
    /// </summary>
    public interface IPdmsService
    {
        /// <summary>
        /// Given VariantRequestId, call PDMS for the VariantRequest
        /// </summary>
        /// <param name="variantRequestId">Guid - Id of the VariantRequest</param>
        /// <returns>VariantRequest</returns>
        Task<VariantRequest> GetVariantRequestAsync(Guid variantRequestId);

        /// <summary>
        /// Update the VariantRequest
        /// </summary>
        /// <param name="variantRequest">modified VariantRequest</param>
        /// <returns>The updated workItem</returns>
        Task<VariantRequest> UpdateVariantRequestAsync(VariantRequest variantRequest);

        /// <summary>
        /// Mark the Variant Request as approved
        /// </summary>
        /// <param name="variantRequestId">Guid - VariantRequestId</param>
        /// <returns>A <see cref="Task"/> that returns true if the call succeeded</returns>
        Task<bool> ApproveVariantRequestAsync(Guid variantRequestId);

        /// <summary>
        /// Delete the VariantRequest as it was rejected
        /// </summary>
        /// <param name="variantRequestId">Guid - VariantRequestId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<bool> DeleteVariantRequestAsync(Guid variantRequestId);

        /// <summary>
        /// Get VariantDefinition from PDMS
        /// </summary>
        /// <param name="variantDefinitionId">Guid - VariantDefinitionId</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<VariantDefinition> GetVariantDefinitionAsync(Guid variantDefinitionId);
    }
}
