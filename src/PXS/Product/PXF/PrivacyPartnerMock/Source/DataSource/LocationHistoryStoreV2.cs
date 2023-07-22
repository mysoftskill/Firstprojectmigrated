// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.DataSource
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Spatial;

    using Microsoft.Data.Edm;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;

    public class LocationHistoryStoreV2 : StoreBase<LocationResourceV2>
    {
        private static readonly LocationHistoryStoreV2 instance = new LocationHistoryStoreV2(MinLocationItems, MaxLocationItems);

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmFullTextProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "locationType", EdmPrimitiveTypeKind.String },
            { "deviceType", EdmPrimitiveTypeKind.String },
            { "name", EdmPrimitiveTypeKind.String },
            { "address", EdmPrimitiveTypeKind.String },
            { "activityType", EdmPrimitiveTypeKind.String },
            { "url", EdmPrimitiveTypeKind.String }
        };

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "id", EdmPrimitiveTypeKind.String },
            { "date", EdmPrimitiveTypeKind.DateTimeOffset },
            { "dateTime", EdmPrimitiveTypeKind.DateTimeOffset },
            { "deviceId", EdmPrimitiveTypeKind.String },
            { "sources", EdmPrimitiveTypeKind.String }, // Actually an array of strings
            { "locationType", EdmPrimitiveTypeKind.String },
            { "deviceType", EdmPrimitiveTypeKind.String },
            { "name", EdmPrimitiveTypeKind.String },
            { "location", EdmPrimitiveTypeKind.GeographyPoint },
            { "accuracyRadius", EdmPrimitiveTypeKind.Double },
            { "activityType", EdmPrimitiveTypeKind.String },
            { "endDate", EdmPrimitiveTypeKind.DateTimeOffset },
            { "endDateTime", EdmPrimitiveTypeKind.DateTimeOffset },
            { "url", EdmPrimitiveTypeKind.String },
            { "distance", EdmPrimitiveTypeKind.Int32 }
        };

        private const int MaxLocationItems = 500;

        private const int MinLocationItems = 2;

        /// <summary>
        ///     Singleton Instance
        /// </summary>
        public static LocationHistoryStoreV2 Instance
        {
            get { return instance; }
        }


        public static object GetValueByName(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var resource = (LocationResourceV2)obj;
            switch (propertyName)
            {
                case "date":
                case "dateTime":
                    return resource.DateTime;
                case "deviceId":
                    return resource.DeviceId;
                case "sources":
                    return resource.Sources;
                case "locationType":
                    return resource.LocationType?.ToString();
                case "deviceType":
                    return resource.DeviceType?.ToString();
                case "name":
                    return resource.Name;
                case "location":
                    return resource.Location;
                case "accuracyRadius":
                    return resource.AccuracyRadius ?? 0;
                case "activityType":
                    return resource.ActivityType?.ToString();
                case "endDate":
                case "endDateTime":
                    return resource.EndDateTime;
                case "url":
                    return resource.Url;
                case "distance":
                    return resource.Distance ?? 0;
            }

            throw new NotSupportedException($"Field {propertyName} is not supported.");
        }

        private LocationHistoryStoreV2(int minItems, int maxItems)
            : base(minItems, maxItems)
        {
        }

        protected override List<LocationResourceV2> CreateRandomTestData()
        {
            var results = new List<LocationResourceV2>();
            var r = new Random();
            int numResults = r.Next(this.MinItems, this.MaxItems);
            for (int i = 0; i < numResults; i++)
            {
                var result = new LocationResourceV2
                {
                    DateTime = DateTimeOffset.UtcNow - new TimeSpan(r.Next(30), r.Next(24), r.Next(60), r.Next(60)),
                    Location = GeographyPoint.Create(((double)r.Next(180 * 1000) / 1000) - 90.0, ((double)r.Next(360 * 1000) / 1000) - 180.0),
                    AccuracyRadius = r.Next(100)
                };

                // 25% chance of device id
                if (r.Next(4) == 0)
                {
                    result.DeviceId = Guid.NewGuid().ToString();
                }

                results.Add(result);
            }

            return results;
        }


    }
}
