// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    ///     Delete Export Archives Args
    /// </summary>
    public class DeleteExportArchivesArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     The id of the export to delete archives
        /// </summary>
        public string ExportId { get; }

        /// <summary>
        ///     The type of the export 
        /// </summary>
        public ExportType ExportType { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DeleteExportArchivesArgs" /> class.
        /// </summary>
        /// <param name="exportId">The export id to delete archives.</param>
        /// <param name="exportType"></param>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        public DeleteExportArchivesArgs(string exportId, ExportType exportType, string userProxyTicket)
            : base(userProxyTicket)
        {
            this.ExportId = exportId;
            this.ExportType = exportType;
        }

        /// <summary>
        ///     Create query string.
        /// </summary>
        /// <returns>The query string collection.</returns>
        public QueryStringCollection CreateQueryStringCollection()
        {
            return new QueryStringCollection
            {
                { "exportId", this.ExportId },
                { "exportType", this.ExportType.ToString() }
            };
        }
    }
}
