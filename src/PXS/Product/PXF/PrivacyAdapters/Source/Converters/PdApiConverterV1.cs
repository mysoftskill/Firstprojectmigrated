// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Converters
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using ConvertToPlayableAudio;

    /// <summary>
    ///     Converts between V1 data contracts and the Pxf object model
    /// </summary>
    internal static class PdApiConverterV1
    {
        /// <summary>
        ///     Converts a V2 AppUsage data contract to the AppUsageResource model
        /// </summary>
        /// <param name="from">V1 data contract</param>
        /// <param name="partnerId">The partner identifier.</param>
        /// <returns>AppUsageResource model</returns>
        public static AppUsageResource ToAppUsageResource(this AppUsageResourceV2 from, string partnerId)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            var to = CreatePxfResourceFromPrivacyResource<AppUsageResource>(from, partnerId);
            to.Id = Guid.NewGuid().ToString();
            to.AppIconBackground = from.AppIconBackground;
            to.AppIconUrl = from.AppIconUrl;
            to.AppId = from.AppId;
            to.AppName = from.AppName;
            to.AppPublisher = from.AppPublisher;
            to.Aggregation = from.Aggregation;
            to.EndDateTime = from.EndDateTime;

            return to;
        }

        /// <summary>
        ///     Converts a V1 Browse data contract to the BrowseResource model
        /// </summary>
        /// <param name="from">V1 data contract</param>
        /// <param name="partnerId">The partner identifier.</param>
        /// <returns>BrowseResource model</returns>
        public static BrowseResource ToBrowseResource(this BrowseResourceV2 from, string partnerId)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            var to = CreatePxfResourceFromPrivacyResource<BrowseResource>(from, partnerId);
            to.Id = Guid.NewGuid().ToString();
            to.NavigatedToUrl = from.NavigatedToUrl;

            to.PageTitle = from.PageTitle;
            if (string.IsNullOrWhiteSpace(to.PageTitle))
            {
                Uri pageUri;
                if (!Uri.TryCreate(from.NavigatedToUrl, UriKind.Absolute, out pageUri))
                    Uri.TryCreate(from.NavigatedToUrl, UriKind.Absolute, out pageUri);
                if (pageUri != null)
                {
                    // A NavigatedToUrl exists, so use the base DNS of it as the page title
                    // Reference Task 8886771 to see the UI comp
                    to.PageTitle = pageUri.Host;
                }
            }

            to.PartnerId = partnerId;
            to.SearchTerms = null;
            to.UrlHash = from.UrlHash;

            return to;
        }

        /// <summary>
        ///     Converts a V2 ContentConsumption data contract to the AppUsageResource model
        /// </summary>
        /// <param name="from">V1 data contract</param>
        /// <param name="partnerId">The partner identifier.</param>
        /// <returns>ContentConsumptionResource model</returns>
        public static ContentConsumptionResource ToContentConsumptionResource(this ContentConsumptionResourceV2 from, string partnerId)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            ContentConsumptionResource.ContentType typeValue;
            if (!Enum.TryParse(from.MediaType, out typeValue))
                throw new InvalidDataException($"Unknown content consumption type from provider: {from.MediaType}");

            var to = CreatePxfResourceFromPrivacyResource<ContentConsumptionResource>(from, partnerId);
            to.Id = from.Id;
            to.AppName = from.AppName;
            to.Artist = from.Artist;
            to.ContainerName = from.ContainerName;
            to.MediaType = typeValue;
            to.Title = from.Title;
            to.ConsumptionTime = TimeSpan.FromSeconds(from.ConsumptionTimeSeconds);
            to.ContentUrl = from.ContentUrl;
            to.IconUrl = from.IconUrl;

            return to;
        }

        /// <summary>
        ///     Converts a V1 Location data contract to the LocationResource model
        /// </summary>
        /// <param name="from">V1 data contract</param>
        /// <param name="partnerId">The partner identifier.</param>
        /// <returns>LocationResource model</returns>
        public static LocationResource ToLocationResource(this LocationResourceV2 from, string partnerId)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            var to = CreatePxfResourceFromPrivacyResource<LocationResource>(from, partnerId);
            to.Id = Guid.NewGuid().ToString();
            to.AccuracyRadius = from.AccuracyRadius;
            to.Latitude = from.Location.Latitude;
            to.Longitude = from.Location.Longitude;
            to.Name = from.Name;
            to.LocationType = ConvertLocationType(from.LocationType);
            to.DeviceType = ConvertLocationDeviceType(from.DeviceType);
            to.ActivityType = ConvertLocationActivityType(from.ActivityType);
            to.EndDateTime = from.EndDateTime;
            to.Url = from.Url;
            to.Distance = (int?)from.Distance;

            return to;
        }

        /// <summary>
        ///     Converts a V1 Search data contract to the SearchResource model
        /// </summary>
        /// <param name="from">V1 data contract</param>
        /// <param name="partnerId">The partner identifier.</param>
        /// <returns>SearchResource model</returns>
        public static SearchResource ToSeachResource(this SearchResourceV2 from, string partnerId)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            var to = CreatePxfResourceFromPrivacyResource<SearchResource>(from, partnerId);
            to.Id = from.Id;
            to.SearchTerms = from.SearchTerms;
            to.NavigatedToUrls = from.Navigations.Select(
                n => new NavigatedToUrlResource
                {
                    Url = n.Url.ToString(),
                    Title = SearchResource.RemoveBoldTags(n.PageTitle),
                    Time = n.DateTime
                }).ToList();

            return to;
        }

        /// <summary>
        ///     Converts a V1 Voice audio data contract to the VoiceAudioResource model
        /// </summary>
        /// <param name="from">V1 data contract</param>
        /// <param name="partnerId">The partner identifier.</param>
        /// <returns>VoiceAudioResource model</returns>
        public static VoiceAudioResource ToVoiceAudioResource(this VoiceAudioResourceV2 from, string partnerId)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            var to = CreatePxfResourceFromPrivacyResource<VoiceAudioResource>(from, partnerId);
            to.Id = from.Id;

            // No audio chunks should be checked by the caller. For the UX we should return an error,
            // but during export we want to increment a perf counter and log an error and carry on.
            if (from.AudioChunks != null && from.AudioChunks.Length > 0)
            {
                using (var memStream = new MemoryStream())
                using (var binaryWriter = new BinaryWriter(memStream))
                {
                    foreach (byte[] chunk in from.AudioChunks)
                        binaryWriter.Write(chunk, 0, chunk.Length);

                    MemoryStream audioStream = AudioConvert.ProduceBinaryWaveStream(
                        memStream.ToArray(),
                        from.AverageByteRate,
                        from.BitsPerSample,
                        from.BlockAlign,
                        from.SampleRate,
                        from.ChannelCount,
                        from.EncodingFormat,
                        from.ExtraHeader ?? new byte[0]);

                    // Return the original audio history result, as well as the audio byte data now.
                    to.Audio = audioStream?.ToArray();
                }
            }

            return to;
        }

        /// <summary>
        ///     Converts a V1 Voice data contract to the VoiceResource model
        /// </summary>
        /// <param name="from">V1 data contract</param>
        /// <param name="partnerId">The partner identifier.</param>
        /// <returns>VoiceResource model</returns>
        public static VoiceResource ToVoiceResource(this VoiceResourceV2 from, string partnerId)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            var to = CreatePxfResourceFromPrivacyResource<VoiceResource>(from, partnerId);
            to.Id = from.Id;
            to.DisplayText = from.DisplayText;
            to.Application = from.Application;
            to.DeviceType = from.DeviceType;

            return to;
        }

        private static LocationActivityType? ConvertLocationActivityType(LocationEnumsV2.LocationActivityTypeV2? from)
        {
            if (!from.HasValue)
                return null;

            switch (from)
            {
                case LocationEnumsV2.LocationActivityTypeV2.Hike:
                    return LocationActivityType.Hike;
                case LocationEnumsV2.LocationActivityTypeV2.Run:
                    return LocationActivityType.Run;
                case LocationEnumsV2.LocationActivityTypeV2.Bike:
                    return LocationActivityType.Bike;
            }

            return LocationActivityType.Unspecified;
        }

        private static LocationDeviceType? ConvertLocationDeviceType(LocationEnumsV2.LocationDeviceTypeV2? from)
        {
            if (!from.HasValue)
                return null;

            switch (from)
            {
                case LocationEnumsV2.LocationDeviceTypeV2.Phone:
                    return LocationDeviceType.Phone;
                case LocationEnumsV2.LocationDeviceTypeV2.Tablet:
                    return LocationDeviceType.Tablet;
                case LocationEnumsV2.LocationDeviceTypeV2.PC:
                    return LocationDeviceType.PC;
                case LocationEnumsV2.LocationDeviceTypeV2.Console:
                    return LocationDeviceType.Console;
                case LocationEnumsV2.LocationDeviceTypeV2.Laptop:
                    return LocationDeviceType.Laptop;
                case LocationEnumsV2.LocationDeviceTypeV2.Accessory:
                    return LocationDeviceType.Accessory;
                case LocationEnumsV2.LocationDeviceTypeV2.Wearable:
                    return LocationDeviceType.Wearable;
                case LocationEnumsV2.LocationDeviceTypeV2.SurfaceHub:
                    return LocationDeviceType.SurfaceHub;
                case LocationEnumsV2.LocationDeviceTypeV2.HeadMountedDisplay:
                    return LocationDeviceType.HeadMountedDisplay;
            }

            return LocationDeviceType.Unknown;
        }

        private static LocationType? ConvertLocationType(LocationEnumsV2.LocationTypeV2? from)
        {
            if (!from.HasValue)
                return null;

            switch (from)
            {
                case LocationEnumsV2.LocationTypeV2.Device:
                    return LocationType.Device;
                case LocationEnumsV2.LocationTypeV2.Implicit:
                    return LocationType.Implicit;
                case LocationEnumsV2.LocationTypeV2.Fitness:
                    return LocationType.Fitness;
                case LocationEnumsV2.LocationTypeV2.Favorite:
                    return LocationType.Favorite;
            }

            return LocationType.Unknown;
        }

        private static T CreatePxfResourceFromPrivacyResource<T>(PrivacyResourceV2 from, string partnerId)
            where T : Resource, new()
        {
            return new T
            {
                DateTime = from.DateTime,
                DeviceId = from.DeviceId,
                Sources = from.Sources,
                Status = ResourceStatus.Unknown,
                PartnerId = partnerId,
                PropertyBag = from.PropertyBag,
            };
        }
    }
}
