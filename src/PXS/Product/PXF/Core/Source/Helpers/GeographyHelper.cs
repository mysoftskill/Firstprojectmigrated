// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System;
    using System.Data.Spatial;
    using System.Globalization;

    /// <summary>
    /// Geography Helper
    /// </summary>
    public static class GeographyHelper
    {
        /// <summary>
        /// Calculates the distance.
        /// </summary>
        /// <param name="latitude1">The latitude1.</param>
        /// <param name="longitude1">The longitude1.</param>
        /// <param name="latitude2">The latitude2.</param>
        /// <param name="longitude2">The longitude2.</param>
        /// <returns>Distance in meters.</returns>
        public static double CalculateDistance(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            return CalculateDistance(latitude1, longitude1, CreatePoint(latitude2, longitude2));
        }

        /// <summary>
        /// Calculates the distance.
        /// </summary>
        /// <param name="latitude1">The latitude1.</param>
        /// <param name="longitude1">The longitude1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>Distance in meters.</returns>
        /// <exception cref="System.ArgumentException">Distance calculated is null.</exception>
        public static double CalculateDistance(double latitude1, double longitude1, DbGeography point2)
        {
            var point1 = CreatePoint(latitude1, longitude1);
            var distance = point1.Distance(point2);

            if (distance == null)
            {
                throw new ArgumentException("Distance calculated is null.");
            }

            return distance.Value;
        }

        /// <summary>
        /// Creates the point.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <returns>A <see cref="DbGeography"/> point represents data in the geodetic (round earth) coordinate system.</returns>
        public static DbGeography CreatePoint(double latitude, double longitude)
        {
            // Create the Point using 'Well-known text (WKT)'. This is a standard format for vector geometry.
            // https://msdn.microsoft.com/en-us/library/system.data.spatial.dbgeography.pointfromtext(v=vs.110).aspx
            var text = string.Format(CultureInfo.InvariantCulture.NumberFormat, "POINT({0} {1})", longitude, latitude);

            // 4326 is most common coordinate system used by GPS/Maps
            return DbGeography.PointFromText(text, 4326);
        } 
    }
}