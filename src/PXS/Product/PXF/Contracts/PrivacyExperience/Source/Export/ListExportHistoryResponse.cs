// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    using System.Collections.Generic;

    public class ListExportHistoryResponse
    {
        /// <summary>
        ///     Gets or sets the list of user's exports.
        /// </summary>
        public IList<ExportStatus> Exports { get; set; }
    }
}
