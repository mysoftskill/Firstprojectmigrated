// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.DataSource
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Microsoft.Data.Edm;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;

    public class VoiceHistoryStoreV2 : StoreBase<VoiceResourceV2>
    {
        private string[] words = null;
        private Random wordsRandom = new Random();

        private const int MinVoiceItems = 2;

        private const int MaxVoiceItems = 100;

        private static VoiceHistoryStoreV2 instance = new VoiceHistoryStoreV2(MinVoiceItems, MaxVoiceItems);

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmFullTextProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "displayText", EdmPrimitiveTypeKind.String },
            { "application", EdmPrimitiveTypeKind.String },
            { "deviceType", EdmPrimitiveTypeKind.String }
        };

        public static IDictionary<string, EdmPrimitiveTypeKind> EdmProperties = new Dictionary<string, EdmPrimitiveTypeKind>
        {
            { "id", EdmPrimitiveTypeKind.String },
            { "date", EdmPrimitiveTypeKind.DateTimeOffset },
            { "dateTime", EdmPrimitiveTypeKind.DateTimeOffset },
            { "sources", EdmPrimitiveTypeKind.String }, // Actually an array of strings
            { "deviceId", EdmPrimitiveTypeKind.String },
            { "displayText", EdmPrimitiveTypeKind.String },
            { "application", EdmPrimitiveTypeKind.String },
            { "deviceType", EdmPrimitiveTypeKind.String }
        };

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static VoiceHistoryStoreV2 Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Generates and stores random voice history data
        /// </summary>
        /// <param name="minRandomItems">Minimum number of random items per user to create.</param>
        /// <param name="maxRandomItems">Maximum number or random items per user to create.</param>
        private VoiceHistoryStoreV2(int minRandomItems, int maxRandomItems)
            : base(minRandomItems, maxRandomItems)
        {
            words = File.ReadAllLines("words.txt").Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        }

        public static object GetValueByName(object obj, string propertyName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var resource = (VoiceResourceV2)obj;
            switch (propertyName)
            {
                case "id":
                    return resource.Id;
                case "date":
                case "dateTime":
                    return resource.DateTime;
                case "sources":
                    return resource.Sources;
                case "deviceId":
                    return resource.DeviceId;
                case "displayText":
                    return resource.DisplayText;
                case "application":
                    return resource.Application;
                case "deviceType":
                    return resource.DeviceType;
            }

            throw new NotSupportedException($"Field {propertyName} is not supported.");
        }

        private readonly byte[][] audioChunks = new[] { File.ReadAllBytes("hello.bin") };

        protected override List<VoiceResourceV2> CreateRandomTestData()
        {
            List<VoiceResourceV2> results = new List<VoiceResourceV2>();
            Random r = new Random();

            // create random voice results
            int numResults = r.Next(this.MinItems, this.MaxItems);
            for (int i = 0; i < numResults; i++)
            {
                VoiceAudioResourceV2 result = new VoiceAudioResourceV2();

                // random time in last 30 days
                result.DateTime = DateTimeOffset.UtcNow - new TimeSpan(r.Next(30), r.Next(24), r.Next(60), r.Next(60));

                // 25% chance of device id
                if (r.Next(4) == 0)
                {
                    result.DeviceId = Guid.NewGuid().ToString();
                }

                result.Id = Guid.NewGuid().ToString();
                StringBuilder sb = new StringBuilder();
                int count = r.Next(1, 5);
                for (int j = 0; j < count; j++)
                {
                    if (sb.Length > 0)
                        sb.Append(" ");
                    sb.Append(GetRandomWord());
                }
                result.DisplayText = sb.ToString();
                result.Application = GetRandomWord();
                result.DeviceType = GetRandomWord();

                // This data captured from actual voice data
                result.AudioChunks = this.audioChunks;
                result.ExtraHeader = null;
                result.AverageByteRate = 32000;
                result.BitsPerSample = 16;
                result.BlockAlign = 2;
                result.ChannelCount = 1;
                result.EncodingFormat = 1;
                result.SampleRate = 16000;

                results.Add(result);
            }

            return results;
        }

        private string GetRandomWord()
        {
            lock (this.wordsRandom)
            {
                return words[wordsRandom.Next(words.Length)];
            }
        }
    }
}
