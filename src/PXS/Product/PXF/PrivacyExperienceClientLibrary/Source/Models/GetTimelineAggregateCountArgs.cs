

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    /// <summary>
    ///     Arguments for getting aggregate counts of the timeline data
    /// </summary>
    public class GetTimelineAggregateCountArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     The card types to retrieve aggregate counts that should come from <see cref="TimelineCard.CardTypes" />
        /// </summary>
        public IList<string> cardTypes { get; }

        /// <summary>
        ///     Create an aggregate count argument object
        /// </summary>
        /// <param name="cardTypes">list of card types to get aggregate counts</param>
        /// <param name="userProxyTicket">user proxy ticket</param>
        public GetTimelineAggregateCountArgs(
            string userProxyTicket,
            IList<string> cardTypes
            )
            : base(userProxyTicket)
        {
            if (cardTypes == null)
                throw new ArgumentNullException(nameof(cardTypes));
            this.cardTypes = cardTypes;
        }

        /// <summary>
        ///     Creates the appropriate query string collection
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            var queryString = new QueryStringCollection();
            queryString.Add("cardTypes", string.Join(",", this.cardTypes));

            return queryString;
        }
    }
}
