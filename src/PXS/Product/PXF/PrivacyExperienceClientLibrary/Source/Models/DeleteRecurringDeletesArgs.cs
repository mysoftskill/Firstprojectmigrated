namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Delete Recurring deletes args.
    /// </summary>
    public class DeleteRecurringDeletesArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     Gets or sets the Categories collection/DataTypes
        /// </summary>
        public string DataType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteRecurringDeletesArgs"/> class.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        /// <param name="dataType">Data types.</param>
        public DeleteRecurringDeletesArgs(string userProxyTicket, string dataType) : base(userProxyTicket)
        {
            this.DataType = dataType;
        }

        /// <summary>
        ///     Creates the appropriate query string collection
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            var queryString = new QueryStringCollection();
            queryString.Add("dataType", this.DataType);

            return queryString;
        }
    }
}
