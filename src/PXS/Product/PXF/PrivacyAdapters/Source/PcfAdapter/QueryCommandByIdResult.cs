// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Response code from PCF
    /// </summary>
    /// <remarks>
    ///     Don't rename these values; they are on the wire as strings.
    /// 
    ///     These values come from the PCF codebase, so they must remain ordered and named the same
    /// </remarks>
    public enum ResponseCode
    {
        /// <summary>
        ///     The command was found and has been returned.
        /// </summary>
        OK,

        /// <summary>
        ///     The given command ID was not found in PCF. It may not exist or may have aged out.
        /// </summary>
        CommandNotFound,

        /// <summary>
        ///     The command has been delivered, but it was not found in the agent's queue. This can happen in the case where the
        ///     agent has already marked the command as completed and the command has been deleted from the queue, but PCF has not
        ///     yet updated the overall state tracking map to reflect this fact. This is expected to be a temporary state and should
        ///     not persist for more than a few minutes normally.
        /// </summary>
        CommandNotFoundInQueue,

        /// <summary>
        /// The command was not (and will not be) delivered to the given agent/asset group
        /// </summary>
        CommandNotApplicable,

        /// <summary>
        /// The command was applicable, but has not yet been delivered to the agent.
        /// </summary>
        CommandNotYetDelivered,

        /// <summary>
        /// The agent has already marked the command as completed.
        /// </summary>
        CommandAlreadyCompleted,

        /// <summary>
        /// This command predates the feature that tracks command insertion locations.
        /// </summary>
        UnableToResolveLocation,
    }

    public class QueryCommandByIdResult
    {
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ResponseCode ResponseCode { get; set; }

        [JsonProperty]
        public JObject Command { get; set; }
    }
}
