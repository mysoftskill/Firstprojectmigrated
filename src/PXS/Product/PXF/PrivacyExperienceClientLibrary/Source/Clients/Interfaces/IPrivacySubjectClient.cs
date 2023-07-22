// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models.PrivacySubject;

    /// <summary>
    ///     Client operations for privacy subjects.
    /// </summary>
    public interface IPrivacySubjectClient
    {
        /// <summary>
        ///     Deletes privacy subject's data by types.
        /// </summary>
        Task<OperationResponse> DeleteByTypesAsync(DeleteByTypesArgs args);

        /// <summary>
        ///     Exports privacy subject's data by types.
        /// </summary>
        Task<OperationResponse> ExportByTypesAsync(ExportByTypesArgs args);
    }
}
