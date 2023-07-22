// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.DataSource
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    using Microsoft.Data.Edm;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;

    public class BrowseHistoryStoreV2 : StoreBase<BrowseResourceV2>
    {
        private static readonly BrowseHistoryStoreV2 instance = new BrowseHistoryStoreV2(MinBrowseItems, MaxBrowseItems);

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmFullTextProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "navigatedToUrl", EdmPrimitiveTypeKind.String },
            { "pageTitle", EdmPrimitiveTypeKind.String }
        };

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "urlHash", EdmPrimitiveTypeKind.String },
            { "date", EdmPrimitiveTypeKind.DateTimeOffset },
            { "sources", EdmPrimitiveTypeKind.String }, // Actually an array of strings
            { "dateTime", EdmPrimitiveTypeKind.DateTimeOffset },
            { "deviceId", EdmPrimitiveTypeKind.String },
            { "navigatedToUrl", EdmPrimitiveTypeKind.String },
            { "pageTitle", EdmPrimitiveTypeKind.String }
        };

        private const int MaxBrowseItems = 500;

        private const int MinBrowseItems = 2;

        private readonly string[] words;

        private readonly Random wordsRandom = new Random();

        /// <summary>
        ///     Singleton Instance
        /// </summary>
        public static BrowseHistoryStoreV2 Instance
        {
            get { return instance; }
        }

        public static object GetValueByName(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var resource = (BrowseResourceV2)obj;
            switch (propertyName)
            {
                case "urlHash":
                    return resource.UrlHash;
                case "date":
                case "dateTime":
                    return resource.DateTime;
                case "sources":
                    return resource.Sources;
                case "deviceId":
                    return resource.DeviceId;
                case "navigatedToUrl":
                    return resource.NavigatedToUrl;
                case "pageTitle":
                    return resource.PageTitle;
            }

            throw new NotSupportedException($"Field {propertyName} is not supported.");
        }

        /// <summary>
        ///     Generates and stores random browse history data
        /// </summary>
        /// <param name="minRandomItems">Minimum number of random items per user to create.</param>
        /// <param name="maxRandomItems">Maximum number or random items per user to create.</param>
        private BrowseHistoryStoreV2(int minRandomItems, int maxRandomItems)
            : base(minRandomItems, maxRandomItems)
        {
            this.words = File.ReadAllLines("words.txt").Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        }

        protected override List<BrowseResourceV2> CreateRandomTestData()
        {
            var results = new List<BrowseResourceV2>();
            var r = new Random();

            // create random browse results
            int numResults = r.Next(this.MinItems, this.MaxItems);
            for (int i = 0; i < numResults; i++)
            {
                var result = new BrowseResourceV2();

                // random time in last 30 days
                result.DateTime = DateTimeOffset.UtcNow - new TimeSpan(r.Next(30), r.Next(24), r.Next(60), r.Next(60));

                // 25% chance of device id
                if (r.Next(4) == 0)
                {
                    result.DeviceId = Guid.NewGuid().ToString();
                }

                var words = new[] { this.GetRandomWord(), this.GetRandomWord() };

                result.NavigatedToUrl = "https://www." + words[0] + words[1] + ".com";
                result.UrlHash = UrlHashing.HashUrl(result.NavigatedToUrl);
                result.PageTitle = words[0] + " " + words[1];

                results.Add(result);
            }

            return results;
        }

        private string GetRandomWord()
        {
            lock (this.wordsRandom)
            {
                return this.words[this.wordsRandom.Next(this.words.Length)];
            }
        }
    }
}
