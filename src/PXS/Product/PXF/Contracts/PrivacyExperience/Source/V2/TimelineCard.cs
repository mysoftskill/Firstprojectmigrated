// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    using Newtonsoft.Json;

    /// <summary>
    ///     The abstract base type for all timeline cards.
    /// </summary>
    public abstract class TimelineCard
    {
        /// <summary>
        ///     Card types that can be passed into GetTimeline
        /// </summary>
        public static class CardTypes
        {
            public const string AppUsageCard = "AppUsageCard";

            public const string BookConsumptionCard = "BookConsumptionCard";

            public const string BrowseCard = "BrowseCard";

            public const string ContentConsumptionCount = "ContentConsumptionCount";

            public const string EpisodeConsumptionCard = "EpisodeConsumptionCard";

            public const string LocationCard = "LocationCard";

            public const string SearchCard = "SearchCard";

            public const string SongConsumptionCard = "SongConsumptionCard";

            public const string SurroundVideoConsumptionCard = "SurroundVideoConsumptionCard";

            public const string VideoConsumptionCard = "VideoConsumptionCard";

            public const string VoiceCard = "VoiceCard";
        }

        private string id;

        public static bool Aggregate(TimeSpan timeZoneOffset, TimelineCard a, TimelineCard b)
        {
            var sa = a as SearchCard;
            var sb = b as SearchCard;
            if (sa != null && sb != null)
                return SearchCard.Aggregate(timeZoneOffset, sa, sb);

            var ba = a as BrowseCard;
            var bb = b as BrowseCard;
            if (ba != null && bb != null)
                return BrowseCard.Aggregate(timeZoneOffset, ba, bb);

            var la = a as LocationCard;
            var lb = b as LocationCard;
            if (la != null && lb != null)
                return LocationCard.Aggregate(timeZoneOffset, la, lb);

            return false;
        }

        public static TimelineCard DeserializeId(string id)
        {
            var key = Base64UrlSerializer.Decode<Key>(id);

            TimelineCard card;
            switch (key.Type)
            {
                case CardTypes.AppUsageCard:
                    card = AppUsageCard.FromKeyComponents(key.Keys);
                    break;
                case CardTypes.BrowseCard:
                    card = BrowseCard.FromKeyComponents(key.Keys);
                    break;
                case CardTypes.SearchCard:
                    card = SearchCard.FromKeyComponents(key.Keys);
                    break;
                case CardTypes.VoiceCard:
                    card = VoiceCard.FromKeyComponents(key.Keys);
                    break;
                case CardTypes.LocationCard:
                    card = LocationCard.FromKeyComponents(key.Keys);
                    break;
                case CardTypes.BookConsumptionCard:
                    card = BookConsumptionCard.FromKeyComponents(key.Keys);
                    break;
                case CardTypes.EpisodeConsumptionCard:
                    card = EpisodeConsumptionCard.FromKeyComponents(key.Keys);
                    break;
                case CardTypes.SongConsumptionCard:
                    card = SongConsumptionCard.FromKeyComponents(key.Keys);
                    break;
                case CardTypes.SurroundVideoConsumptionCard:
                    card = SurroundVideoConsumptionCard.FromKeyComponents(key.Keys);
                    break;
                case CardTypes.VideoConsumptionCard:
                    card = VideoConsumptionCard.FromKeyComponents(key.Keys);
                    break;
                default:
                    throw new Exception("Unknown card type: " + key.Type);
            }
            card.id = id;

            return card;
        }

        /// <summary>
        ///     The device ids represented in this card.
        /// </summary>
        public IList<string> DeviceIds { get; }

        /// <summary>
        ///     This should be completely opaque. PXS will encode in this the delete filter. For example
        ///     for AppUsage entries the id might be something like:
        ///     "dateTime eq datetimeoffset'2017-03-23T00:00:00Z and appId eq 'foo' and deviceId eq 'bar'"
        ///     If there are multiple DeviceIds though it's probably multiple deletes, so the above string
        ///     needs to be encoded as multiple strings somehow. Either way, this is an internal PXS micro-protocol
        /// </summary>
        public string Id
        {
            get
            {
                if (this.id != null)
                    return this.id;
                this.id = this.SerializeId();
                return this.id;
            }
        }

        /// <summary>
        ///     Property bag containing properties to aid in processing commands
        /// </summary>
        /// <remarks>
        ///     though this is not expected to be used by all timeline cards initially, it is expected to expand to several other cards
        ///     in the near to mid future, so it is being placed on the base class to avoid having to refactor later.
        /// </remarks>
        [JsonIgnore]
        public IDictionary<string, IList<string>> PropertyBag { get; }

        /// <summary>
        ///     The list of sources from where this card came.
        /// </summary>
        public IList<string> Sources { get; }

        /// <summary>
        ///     The timestamp for the card
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        public string SerializeId()
        {
            string typeName;
            if (this is AppUsageCard)
                typeName = CardTypes.AppUsageCard;
            else if (this is BrowseCard)
                typeName = CardTypes.BrowseCard;
            else if (this is SearchCard)
                typeName = CardTypes.SearchCard;
            else if (this is VoiceCard)
                typeName = CardTypes.VoiceCard;
            else if (this is LocationCard)
                typeName = CardTypes.LocationCard;
            else if (this is BookConsumptionCard)
                typeName = CardTypes.BookConsumptionCard;
            else if (this is EpisodeConsumptionCard)
                typeName = CardTypes.EpisodeConsumptionCard;
            else if (this is SongConsumptionCard)
                typeName = CardTypes.SongConsumptionCard;
            else if (this is SurroundVideoConsumptionCard)
                typeName = CardTypes.SurroundVideoConsumptionCard;
            else if (this is VideoConsumptionCard)
                typeName = CardTypes.VideoConsumptionCard;
            else
                throw new Exception("Unknown card type: " + this.GetType().FullName);

            return Base64UrlSerializer.Encode(new Key(typeName, this.GetKeyComponents()));
        }

        protected TimelineCard(
            string id,
            DateTimeOffset timestamp,
            IList<string> deviceIds,
            IList<string> sources,
            IDictionary<string, IList<string>> propertyBag)
        {
            this.id = id;
            this.Timestamp = timestamp;
            this.DeviceIds = deviceIds;
            this.Sources = sources;
            this.PropertyBag = propertyBag;
        }

        protected TimelineCard(string id, DateTimeOffset timestamp, IList<string> deviceIds, IList<string> sources)
        {
            this.id = id;
            this.Timestamp = timestamp;
            this.DeviceIds = deviceIds;
            this.Sources = sources;
        }

        protected abstract IDictionary<string, string> GetKeyComponents();

        /// <summary>
        ///     Gets the property bag as a serialized blob
        /// </summary>
        /// <remarks>
        ///     this is generally expected to be used when constructing the id key components
        ///     it's located in this class so that this class is the only one that needs know about the details of how the property is
        ///     serialized
        /// </remarks>
        protected string GetSerializedPropertyBag()
        {
            return this.PropertyBag != null ? JsonConvert.SerializeObject(this.PropertyBag) : null;
        }

        private class Key
        {
            [JsonProperty("k")]
            public IDictionary<string, string> Keys { get; }

            [JsonProperty("t")]
            public string Type { get; }

            [JsonConstructor]
            public Key(string type, IDictionary<string, string> keys)
            {
                this.Type = type;
                this.Keys = keys;
            }
        }

        /// <summary>
        ///     Extracts a property bag from an set of key components
        /// </summary>
        /// <param name="components">key components to extract property bag from</param>
        /// <returns>extracted property bag</returns>
        protected static IDictionary<string, IList<string>> DeserializePropertyBagFromIdKeyComponents(
            IDictionary<string, string> components)
        {
            string serializedBag;

            if (components.TryGetValue(KeyConstants.PropertyBag, out serializedBag))
            {
                try
                {
                    return serializedBag != null ? JsonConvert.DeserializeObject<IDictionary<string, IList<string>>>(serializedBag) : null;
                }
                catch (JsonException)
                {
                    throw new ArgumentOutOfRangeException(nameof(components), $"Invalid key component: {KeyConstants.PropertyBag}");
                }
            }

            throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.PropertyBag}");
        }

        /// <summary>
        ///     Keep these short, so our card ids are short.
        /// </summary>
        protected static class KeyConstants
        {
            /// <summary>
            ///     The additional locations impressions in the card, specific to the <see cref="LocationCard" />. This is needed for data agents to consume individual delete signals of locations
            ///     from de-aggregated data.
            /// </summary>
            public const string AdditionalLocations = "l";

            /// <summary>
            ///     This is needed since PDOS needs to sent back as part of the key for deletion. This is a PDOS artifact.
            /// </summary>
            public const string Aggregation = "a";

            /// <summary>
            ///     The end timestamp key
            /// </summary>
            public const string EndTimestamp = "e";

            /// <summary>
            ///     The id key
            /// </summary>
            public const string Id = "i";

            /// <summary>
            ///     Id key for the property bag
            /// </summary>
            public const string PropertyBag = "b";

            /// <summary>
            ///     The search key
            /// </summary>
            public const string Search = "s";

            /// <summary>
            ///     The timestamp key
            /// </summary>
            public const string Timestamp = "t";

            /// <summary>
            ///     The uri key
            /// </summary>
            public const string Uri = "u";
        }
    }
}
