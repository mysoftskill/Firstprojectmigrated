// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     Contains data indicating why an XSTS token request was rejected.
    /// </summary>
    public class XstsError
    {
        /// <summary>
        ///     An identity for the error. Xbox documentation recommends to ignore.
        /// </summary>
        public string Identity { get; set; }

        /// <summary>
        ///     A description of the error. Xbox documentation recommends to ignore.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     An Xbox defined list of error codes.
        /// </summary>
        public uint XErr { get; set; }

        /// <summary>
        ///     Returns a string that represents the current XSTS error.
        /// </summary>
        /// <returns>A string that represents the current XSTS error.</returns>
        public override string ToString()
        {
            return "XErr={0}, Message={1}, Identity={2}".FormatInvariant(this.XErr, this.Message, this.Identity);
        }
    }
}
