// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;

    /// <summary>
    ///     The arguments for fetching a voice cards audio for playback
    /// </summary>
    public class GetVoiceCardAudioArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     The id of the card to retrieve the audio stream for
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Constructs the arguments
        /// </summary>
        public GetVoiceCardAudioArgs(string userProxyTicket, string id)
            : base(userProxyTicket)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            this.Id = id;
        }

        /// <summary>
        ///     Creates the query string collection
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            return new QueryStringCollection
            {
                { "id", this.Id }
            };
        }
    }
}
