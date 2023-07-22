namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.CsvSerializer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    public class CsvSerializer : ISerializer
    {
        public string WriteHeader(IEnumerable<string> headers)
        {
            return ConvertToCsvLine(headers);
        }

        public string ConvertResource(Resource resource)
        {
            List<string> stringList = new List<string>();
            switch (resource)
            {
                case AppUsageResource usage:
                    stringList.Add(usage.DateTime.ToString());
                    stringList.Add(usage.EndDateTime.ToString());
                    stringList.Add(usage.DeviceId);
                    stringList.Add(usage.Aggregation);
                    stringList.Add(usage.AppName);
                    stringList.Add(usage.AppPublisher);
                    break;

                case BrowseResource browse:
                    stringList.Add(browse.DateTime.ToString());
                    stringList.Add(browse.DeviceId);
                    stringList.Add(browse.NavigatedToUrl);
                    stringList.Add(browse.PageTitle);
                    stringList.Add(browse.SearchTerms);
                    break;

                case VoiceResource voice:
                    stringList.Add(voice.DateTime.ToString());
                    stringList.Add(voice.DeviceId);
                    stringList.Add(voice.Application);
                    stringList.Add(voice.DeviceType);
                    stringList.Add(voice.DisplayText);
                    break;

                case ContentConsumptionResource consumpution:
                    stringList.Add(consumpution.DateTime.ToString());
                    stringList.Add(consumpution.DeviceId);
                    stringList.Add(consumpution.AppName);
                    stringList.Add(consumpution.Artist);
                    stringList.Add(consumpution.ConsumptionTime.ToString());
                    stringList.Add(consumpution.ContainerName);
                    stringList.Add(consumpution.ContentUrl?.ToString());
                    stringList.Add(consumpution.IconUrl?.ToString());
                    stringList.Add(consumpution.MediaType.ToString());
                    stringList.Add(consumpution.Title);
                    break;

                case LocationResource location:
                    return ConvertLocationDataToCsvLine(location);

                case SearchResource search:
                    return ConvertSearchDataToCsvLine(search);

                default:
                    throw new Exception("Invalid Resource Type");
            }

            return ConvertToCsvLine(stringList);
        }

        private string ConvertLocationDataToCsvLine(LocationResource locationResource)
        {
            IEnumerable<string> sources;
            if (locationResource.Sources == null || !locationResource.Sources.Any())
            {
                // sources is an optional parameter, so a line must be created even if the locationResource.Sources is null or empty.
                sources = new string[] { null };
            }
            else
            {
                sources = locationResource.Sources;
            }

            string result = string.Empty;

            foreach (string source in sources)
            {
                var lineItems = new List<string>
                {
                    locationResource.DateTime.ToString(),
                    locationResource.DeviceId,
                    locationResource.AccuracyRadius.ToString(),
                    locationResource.ActivityType.ToString(),
                    locationResource.Address?.AddressLine1,
                    locationResource.Address?.AddressLine2,
                    locationResource.Address?.AddressLine3,
                    locationResource.Address?.CountryRegion,
                    locationResource.Address?.FormattedAddress,
                    locationResource.Address?.Locality,
                    locationResource.Address?.PostalCode,
                    locationResource.DeviceType.ToString(),
                    locationResource.Distance.ToString(),
                    locationResource.EndDateTime.ToString(),
                    locationResource.Latitude.ToString(),
                    locationResource.Longitude.ToString(),
                    locationResource.Name,
                    locationResource.Url,
                    source
                };

                result += ConvertToCsvLine(lineItems);
            }

            return result;
        }

        private string ConvertSearchDataToCsvLine(SearchResource searchResource)
        {
            IEnumerable<NavigatedToUrlResource> urls;
            if (searchResource.NavigatedToUrls == null || !searchResource.NavigatedToUrls.Any())
            {
                // NavigatedToUrls is an optional parameter, so a line must be created even if the searchResource.NavigatedToUrls is null or empty.
                urls = new NavigatedToUrlResource[] { null }; 
            }
            else
            {
                urls = searchResource.NavigatedToUrls;
            }

            string result = string.Empty;

            foreach (NavigatedToUrlResource url in urls)
            {
                var lineItems = new List<string>
                {
                    searchResource.DateTime.ToString(),
                    searchResource.DeviceId,
                    searchResource.Location?.AccuracyRadius.ToString(),
                    searchResource.Location?.Latitude.ToString(),
                    searchResource.Location?.Longitude.ToString(),
                    url?.Time.ToString(),
                    url?.Title,
                    url?.Url,
                    searchResource.SearchTerms
                };

                result += ConvertToCsvLine(lineItems);
            }

            return result;
        }

        /// <summary>
        /// Converts the list of line items and outputs a sanitized csv line
        /// </summary>
        private string ConvertToCsvLine(IEnumerable<string> lineItems)
        {
            IEnumerable<string> sanitizedLineItems = lineItems.Select(e => SanitizeEntry(e));
            string csvLine = string.Join(",", sanitizedLineItems) + Environment.NewLine;
            return csvLine;
        }

        private string SanitizeEntry(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                return string.Empty;
            }

            if (entry.Contains("\""))
            {
                entry = $"\"{entry.Replace("\"", "\"\"")}\"";
            }

            if (entry.Contains(",") || entry.Contains("/") || entry.Contains("\n"))
            {
                entry = $"\"{entry.Replace("\n", " ")}\"";
            }

            return entry;
        }
    }
}