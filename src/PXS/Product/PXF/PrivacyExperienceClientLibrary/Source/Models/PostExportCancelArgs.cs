// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    /// <summary>
    ///     Post Export Cancel Args
    /// </summary>
    public class PostExportCancelArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     The id of the export to cancel
        /// </summary>
        public string ExportId { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PostExportCancelArgs" /> class.
        /// </summary>
        /// <param name="exportId">The export id to cancel.</param>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        public PostExportCancelArgs(string exportId, string userProxyTicket)
            : base(userProxyTicket)
        {
            this.ExportId = exportId;
        }

        /// <summary>
        ///     Create query string.
        /// </summary>
        /// <returns>The query string collection.</returns>
        public QueryStringCollection CreateQueryStringCollection()
        {
            return new QueryStringCollection
            {
                { "exportId", this.ExportId }
            };
        }
    }
}
