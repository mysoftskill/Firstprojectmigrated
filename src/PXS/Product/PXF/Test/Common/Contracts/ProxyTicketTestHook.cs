//--------------------------------------------------------------------------------
// <copyright file="ProxyTicketTestHook.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Contracts
{
    using Newtonsoft.Json;

    public class ProxyTicketRequest
    {
        [JsonProperty("userTicket")]
        public string UserTicket { get; set; }
    }

    public class ProxyTicketResponse
    {
        [JsonProperty("proxyTicket")]
        public string ProxyTicket { get; set; }

    }
}
