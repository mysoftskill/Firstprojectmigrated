// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.DataSource
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Data.Edm;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.PrivacyServices.Common.Azure;

    public class AppUsageStoreV2 : StoreBase<AppUsageResourceV2>
    {
        private static readonly AppUsageStoreV2 instance = new AppUsageStoreV2(MinAppUsageItems, MaxAppUsageItems);

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmFullTextProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "appName", EdmPrimitiveTypeKind.String },
            { "appPublisher", EdmPrimitiveTypeKind.String }
        };

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "date", EdmPrimitiveTypeKind.DateTimeOffset },
            { "dateTime", EdmPrimitiveTypeKind.DateTimeOffset },
            { "endDateTime", EdmPrimitiveTypeKind.DateTimeOffset },
            { "deviceId", EdmPrimitiveTypeKind.String },
            { "sources", EdmPrimitiveTypeKind.String }, // Actually an array of strings
            { "appId", EdmPrimitiveTypeKind.String },
            { "appName", EdmPrimitiveTypeKind.String },
            { "appPublisher", EdmPrimitiveTypeKind.String },
            { "appIconUrl", EdmPrimitiveTypeKind.String },
            { "appIconBackground", EdmPrimitiveTypeKind.String },
            { "aggregation", EdmPrimitiveTypeKind.String }
        };

        private const int MaxAppUsageItems = 500;

        private const int MinAppUsageItems = 2;

        private readonly string[] words;

        private readonly Random wordsRandom = new Random();

        private ILogger Logger { get; } = DualLogger.Instance;

        /// <summary>
        ///     Singleton Instance
        /// </summary>
        public static AppUsageStoreV2 Instance
        {
            get { return instance; }
        }

        public static object GetValueByName(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var resource = (AppUsageResourceV2)obj;
            switch (propertyName)
            {
                case "date":
                case "dateTime":
                    return resource.DateTime;
                case "endDateTime":
                    return resource.EndDateTime;
                case "deviceId":
                    return resource.DeviceId;
                case "appId":
                    return resource.AppId;
                case "sources":
                    return resource.Sources;
                case "appName":
                    return resource.AppName;
                case "appPublisher":
                    return resource.AppPublisher;
                case "appIconUrl":
                    return resource.AppIconUrl;
                case "appIconBackground":
                    return resource.AppIconBackground;
                case "aggregation":
                    return resource.Aggregation;
            }

            throw new NotSupportedException($"Field {propertyName} is not supported.");
        }

        /// <summary>
        ///     Generates and stores random AppUsage data
        /// </summary>
        /// <param name="minRandomItems">Minimum number of random items per user to create.</param>
        /// <param name="maxRandomItems">Maximum number or random items per user to create.</param>
        private AppUsageStoreV2(int minRandomItems, int maxRandomItems)
            : base(minRandomItems, maxRandomItems)
        {
            this.words = File.ReadAllLines("words.txt").Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        }

        protected override List<AppUsageResourceV2> CreateRandomTestData()
        {
            var results = new List<AppUsageResourceV2>();
            var r = new Random();

            DateTimeOffset minDate = DateTimeOffset.UtcNow.UtcDateTime.Date + TimeSpan.FromDays(r.Next(100));
            DateTimeOffset maxDate = DateTimeOffset.UtcNow.UtcDateTime.Date - TimeSpan.FromDays(r.Next(100));

            // create random AppUsage results
            int numResults = r.Next(this.MinItems, this.MaxItems);
            for (int i = 0; i < numResults; i++)
            {
                var result = new AppUsageResourceV2();

                // random time in last 30 days
                result.DateTime = DateTimeOffset.UtcNow.UtcDateTime.Date - TimeSpan.FromDays(r.Next(30));
                result.EndDateTime = result.DateTime.AddDays(1);
                result.DeviceId = Guid.NewGuid().ToString();
                result.AppId = Guid.NewGuid().ToString();
                result.AppName = this.GetRandomWord();
                result.AppPublisher = this.GetRandomWord() + " " + this.GetRandomWord();
                result.AppIconUrl = "https://microsoft.sharepoint.com/search/Style%20Library/Icon/logo_microsoft.png"; // Aww, it's a kitten
                result.AppIconBackground = "#FF0000"; // horrible dev-testing red :)
                result.Aggregation = r.Next(1) == 0 ? "Daily" : "Monthly";
                result.PropertyBag = this.GetPropertyBag(r);

                results.Add(result);

                if (result.DateTime < minDate)
                {
                    minDate = result.DateTime;
                }

                if (result.DateTime > maxDate)
                {
                    maxDate = result.DateTime;
                }
            }

            this.Logger?.Information(nameof(AppUsageStoreV2), $"CreateRandomTestData generated {results.Count} items, minDate {minDate.ToString()}, maxDate {maxDate.ToString()}");

            return results;
        }

        private string GetRandomWord()
        {
            lock (this.wordsRandom)
            {
                return this.words[this.wordsRandom.Next(this.words.Length)];
            }
        }

        private IDictionary<string, IList<string>> GetPropertyBag(Random r)
        {
            IDictionary<string, IList<string>> result = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);

            if (r == null)
            {
                r = new Random();
            }

            // We should expect the property bags to be up to 2k in size, so generate them to have 1k-2k of data per bag.
            // We'll use multiple keys just for the serialization overhead, so also choose the number of keys to add and distribute
            //  the data (roughly) evenly amongst the key set.
              
            int minTotalSize = r.Next(1000, 2000);
            int countKeys = r.Next(1, 7);

            do
            {
                string key = this.GetRandomWord();
                int current = (minTotalSize / countKeys) - key.Length;

                IList<string> list;

                // if we randomly get an existing word, just append more values to that existing word
                if (!result.TryGetValue(key, out list))
                {
                    list = new List<string>();
                    result[key] = list;
                }

                do
                {
                    string nextValue = this.GetRandomWord();

                    current -= nextValue.Length;
                    list.Add(nextValue);
                }
                while (current > 0);
            }
            while (--countKeys > 0);

            return result;
        }
    }
}
