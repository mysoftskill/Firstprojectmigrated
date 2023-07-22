namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys;

    /// <summary>
    /// Wraps the key and timetolive for caching
    /// </summary>
    public class CacheItem
    {
        /// <summary>
        /// Expiration time for the item
        /// </summary>
        public DateTimeOffset Expiration { get; set; }

        /// <summary>
        /// True if this key is found and returned by the Key Discovery API else false
        /// </summary>
        public bool Found { get; set; }

        /// <summary>
        /// The <see cref="JsonWebKey" /> to be cached
        /// </summary>
        public JsonWebKey Item { get; set; }

        /// <summary>
        /// Initializes a new <see cref="CacheItem" />.
        /// </summary>
        /// <param name="item">Value of Key</param>
        /// <param name="found">Is this a valid key found via the KeyDiscoveryAPI</param>
        /// <param name="expiration">expiration time for the key</param>
        /// <param name="timeToLive">time in seconds before expiration, if both are provided expiration trumps.</param>
        public CacheItem(JsonWebKey item, bool found = true, DateTimeOffset? expiration = null, int timeToLive = 360)
        {
            this.Item = item;
            this.Found = found;
            this.Expiration = expiration ?? DateTimeOffset.UtcNow.AddSeconds(timeToLive);
        }
    }
}
