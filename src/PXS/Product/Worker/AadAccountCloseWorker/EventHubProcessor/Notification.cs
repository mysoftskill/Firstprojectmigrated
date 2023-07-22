// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    using Newtonsoft.Json;

    /// <summary>
    ///     A class representing a notification.
    /// </summary>
    public class Notification
    {
        /// <summary>
        ///     Gets or sets the change type.
        /// </summary>
        [JsonProperty(PropertyName = "changeType")]

        public string ChangeType { get; set; }

        /// <summary>
        ///     Gets or sets the client state used to verify the notification.
        /// </summary>
        [JsonProperty(PropertyName = "clientState")]
        public string ClientState { get; set; }

        /// <summary>
        ///     Gets or sets the endpoint of the resource that changed.
        /// </summary>
        [JsonProperty(PropertyName = "resource")]
        public string Resource { get; set; }

        /// <summary>
        ///     Gets or sets the properties of the changed resource.
        /// </summary>
        [JsonProperty(PropertyName = "resourceData")]
        public ResourceData ResourceData { get; set; }

        /// <summary>
        ///     Gets or sets the UTC date and time when the subscription expires.
        /// </summary>
        [JsonProperty(PropertyName = "subscriptionExpirationDateTime")]
        public string SubscriptionExpirationDateTime { get; set; }

        /// <summary>
        ///     Gets or sets the unique identifier for the subscription.
        /// </summary>
        [JsonProperty(PropertyName = "subscriptionId")]
        public string SubscriptionId { get; set; }

        /// <summary>
        ///     Gets or sets the token. For account close notifications, this value represents the 'pre-verifier' token.
        /// </summary>
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }
    }
}
