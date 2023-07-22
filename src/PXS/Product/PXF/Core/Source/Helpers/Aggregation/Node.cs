// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System.Data.Spatial;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    public class Node
    {
        private static readonly DbGeography AnchorPoint = GeographyHelper.CreatePoint(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="location">The location.</param>
        public Node(LocationHistoryV1 location)
        {
            this.Latitude = location.Latitude;
            this.Longitude = location.Longitude;

            this.DistanceFromCenter = this.CalculateDistance();
            this.Properties = location;
            this.PartnerId = location.PartnerId;
            this.DeviceId = location.DeviceId;
        }

        /// <summary>
        /// Gets or sets the node properties.
        /// </summary>
        public LocationHistoryV1 Properties { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        public double Longitude { get; private set; }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        public double Latitude { get; private set; }

        /// <summary>
        /// Gets or sets the partner identifier.
        /// </summary>
        public string PartnerId { get; private set; }

        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Gets or sets the distance from center.
        /// </summary>
        public double DistanceFromCenter { get; private set; }

        private double CalculateDistance()
        {
            return GeographyHelper.CalculateDistance(this.Latitude, this.Longitude, AnchorPoint);
        }
    }
}