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
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    public class ContentConsumptionStoreV2 : StoreBase<ContentConsumptionResourceV2>
    {
        private static readonly ContentConsumptionStoreV2 instance = new ContentConsumptionStoreV2(MinContentConsumptionItems, MaxContentConsumptionItems);

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmFullTextProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "artist", EdmPrimitiveTypeKind.String },
            { "title", EdmPrimitiveTypeKind.String },
            { "containerName", EdmPrimitiveTypeKind.String },
            { "appName", EdmPrimitiveTypeKind.String }
        };

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "id", EdmPrimitiveTypeKind.String },
            { "mediaType", EdmPrimitiveTypeKind.String },
            { "date", EdmPrimitiveTypeKind.DateTimeOffset },
            { "dateTime", EdmPrimitiveTypeKind.DateTimeOffset },
            { "sources", EdmPrimitiveTypeKind.String }, // Actually an array of strings
            { "artist", EdmPrimitiveTypeKind.String },
            { "title", EdmPrimitiveTypeKind.String },
            { "containerName", EdmPrimitiveTypeKind.String },
            { "iconUrl", EdmPrimitiveTypeKind.String },
            { "appName", EdmPrimitiveTypeKind.String },
            { "contentUrl", EdmPrimitiveTypeKind.String },
            { "consumptionTime", EdmPrimitiveTypeKind.Int32 }
        };

        private const int MaxContentConsumptionItems = 500;

        private const int MinContentConsumptionItems = 2;

        private readonly string[] words;

        private readonly Random wordsRandom = new Random();

        /// <summary>
        ///     Singleton Instance
        /// </summary>
        public static ContentConsumptionStoreV2 Instance
        {
            get { return instance; }
        }

        public static object GetValueByName(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var resource = (ContentConsumptionResourceV2)obj;
            switch (propertyName)
            {
                case "id":
                    return resource.Id;
                case "mediaType":
                    return resource.MediaType;
                case "date":
                case "dateTime":
                    return resource.DateTime;
                case "sources":
                    return resource.Sources;
                case "artist":
                    return resource.Artist;
                case "title":
                    return resource.Title;
                case "containerName":
                    return resource.ContainerName;
                case "iconUrl":
                    return resource.IconUrl;
                case "appName":
                    return resource.AppName;
                case "contentUrl":
                    return resource.ContentUrl;
                case "consumptionTime":
                    return resource.ConsumptionTimeSeconds;
            }

            throw new NotSupportedException($"Field {propertyName} is not supported.");
        }

        /// <summary>
        ///     Generates and stores random ContentConsumption data
        /// </summary>
        /// <param name="minRandomItems">Minimum number of random items per user to create.</param>
        /// <param name="maxRandomItems">Maximum number or random items per user to create.</param>
        private ContentConsumptionStoreV2(int minRandomItems, int maxRandomItems)
            : base(minRandomItems, maxRandomItems)
        {
            this.words = File.ReadAllLines("words.txt").Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        }

        protected override List<ContentConsumptionResourceV2> CreateRandomTestData()
        {
            var results = new List<ContentConsumptionResourceV2>();
            var r = new Random();

            // create random ContentConsumption results
            int numResults = r.Next(this.MinItems, this.MaxItems);
            for (int i = 0; i < numResults; i++)
            {
                var contentTypes = (ContentConsumptionResource.ContentType[])Enum.GetValues(typeof(ContentConsumptionResource.ContentType));
                var result = new ContentConsumptionResourceV2
                {
                    Id = Guid.NewGuid().ToString(),
                    DateTime = DateTimeOffset.UtcNow - new TimeSpan(r.Next(30), r.Next(24), r.Next(60), r.Next(60)),
                    MediaType = contentTypes[r.Next(contentTypes.Length)].ToString(),
                    AppName = this.GetRandomWord(),
                    Artist = this.GetRandomWord() + " " + this.GetRandomWord(),
                    ConsumptionTimeSeconds = r.Next(60 * 30),
                    ContainerName = this.GetRandomWord(),
                    ContentUrl = new Uri("https://" + this.GetRandomWord() + ".com"),
                    IconUrl = new Uri("https://" + this.GetRandomWord() + ".com"),
                    Title = this.GetRandomWord() + " and " + this.GetRandomWord() + " presents " + this.GetRandomWord() + " " + this.GetRandomWord()
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
