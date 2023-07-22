// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System;

    /// <summary>
    ///     Pair of Privacy Data Type and Command ID
    /// </summary>
    internal class SignalData
    {
        /// <summary>
        ///     Gets the command identifier.
        /// </summary>
        /// <value>
        ///     The command identifier.
        /// </value>
        public Guid CommandId { get; }

        public Guid RequestGuid { get; }

        /// <summary>
        ///     Gets the type of the privacy data.
        /// </summary>
        /// <value>
        ///     The type of the privacy data.
        /// </value>
        public string PrivacyDataType { get; }

        /// <summary>
        ///     The verifier token for the signal
        /// </summary>
        public string VerifierToken { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SignalData" /> class.
        /// </summary>
        /// <param name="privacyDataType">Type of the privacy data.</param>
        /// <param name="requestGuid">The request guid for the vortex event</param>
        public SignalData(string privacyDataType, Guid requestGuid)
        {
            this.PrivacyDataType = privacyDataType;
            this.CommandId = Guid.NewGuid();
            this.RequestGuid = requestGuid;
        }
    }
}
