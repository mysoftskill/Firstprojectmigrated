// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Abstract base class for ContentConsumption Card
    /// </summary>
    public abstract class ContentConsumptionCard : TimelineCard
    {
        /// <summary>
        ///     Id of the media
        /// </summary>
        public string MediaId { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContentConsumptionCard" /> class.
        /// </summary>
        /// <param name="mediaId"></param>
        /// <param name="id">The identifier.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="deviceIds">The device ids.</param>
        /// <param name="sources"></param>
        protected ContentConsumptionCard(string mediaId, string id, DateTimeOffset timestamp, IList<string> deviceIds, IList<string> sources)
            : base(id, timestamp, deviceIds, sources)
        {
            this.MediaId = mediaId;
        }
    }
}
