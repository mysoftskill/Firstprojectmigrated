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

    public class SearchHistoryStoreV2 : StoreBase<SearchResourceV2>
    {
        private static readonly SearchHistoryStoreV2 instance = new SearchHistoryStoreV2(MinSearchItems, MaxSearchItems);

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmFullTextProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "searchTerms", EdmPrimitiveTypeKind.String },
            { "searchType", EdmPrimitiveTypeKind.String }
        };

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "id", EdmPrimitiveTypeKind.String },
            { "date", EdmPrimitiveTypeKind.DateTimeOffset },
            { "dateTime", EdmPrimitiveTypeKind.DateTimeOffset },
            { "sources", EdmPrimitiveTypeKind.String }, // Actually an array of strings
            { "deviceId", EdmPrimitiveTypeKind.String },
            { "searchTerms", EdmPrimitiveTypeKind.String }
        };

        private const int MaxSearchItems = 500;

        private const int MinSearchItems = 2;

        private readonly string[] words;

        private readonly Random wordsRandom = new Random();

        /// <summary>
        ///     Singleton Instance
        /// </summary>
        public static SearchHistoryStoreV2 Instance
        {
            get { return instance; }
        }

        public static object GetValueByName(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var resource = (SearchResourceV2)obj;
            switch (propertyName)
            {
                case "id":
                    return resource.Id;
                case "date":
                case "dateTime":
                    return resource.DateTime;
                case "deviceId":
                    return resource.DeviceId;
                case "sources":
                    return resource.Sources;
                case "searchTerms":
                    return resource.SearchTerms;
            }

            throw new NotSupportedException($"Field {propertyName} is not supported.");
        }

        /// <summary>
        ///     Generates and stores random Search history data
        /// </summary>
        /// <param name="minRandomItems">Minimum number of random items per user to create.</param>
        /// <param name="maxRandomItems">Maximum number or random items per user to create.</param>
        private SearchHistoryStoreV2(int minRandomItems, int maxRandomItems)
            : base(minRandomItems, maxRandomItems)
        {
            this.words = File.ReadAllLines("words.txt").Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        }

        protected override List<SearchResourceV2> CreateRandomTestData()
        {
            var results = new List<SearchResourceV2>();
            var r = new Random();

            // create random Search results
            int numResults = r.Next(this.MinItems, this.MaxItems);
            for (int i = 0; i < numResults; i++)
            {
                var result = new SearchResourceV2();

                // random time in last 30 days
                result.DateTime = DateTimeOffset.UtcNow - new TimeSpan(r.Next(30), r.Next(24), r.Next(60), r.Next(60));

                // 25% chance of device id
                if (r.Next(4) == 0)
                {
                    result.DeviceId = Guid.NewGuid().ToString();
                }

                result.SearchTerms = this.GetRandomWord() + " " + this.GetRandomWord();
                result.Id = Guid.NewGuid().ToString();
                result.Navigations = new List<NavigatedUrlV2>
                {
                    new NavigatedUrlV2
                    {
                        PageTitle = this.GetRandomWord(),
                        Url = new Uri("https://www." + this.GetRandomWord() + this.GetRandomWord() + ".com"),
                        DateTime = result.DateTime + TimeSpan.FromSeconds(15)
                    }
                };

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
