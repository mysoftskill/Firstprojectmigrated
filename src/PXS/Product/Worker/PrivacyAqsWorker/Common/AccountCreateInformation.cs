// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common
{
    using SocialAccessorV4;

    /// <summary>
    ///     Contains information on the newly created account
    /// </summary>
    public class AccountCreateInformation
    {
        private ulong puid;

        /// <summary>
        ///     Gets the generated ANID
        /// </summary>
        public string Anid { get; private set; }

        /// <summary>
        ///     Gets or sets the CID.
        /// </summary>
        public long Cid { get; set; }

        /// <summary>
        ///     Gets the generated OPID
        /// </summary>
        public string Opid { get; private set; }

        /// <summary>
        ///     Gets or sets the PUID
        /// </summary>
        public ulong Puid
        {
            get => this.puid;
            set
            {
                this.puid = value;
                this.Anid = IdConverter.AnidFromPuid(value);
                this.Opid = IdConverter.OpidFromPuid(value);
            }
        }

        /// <summary>
        ///     Gets the <see cref="AccountCreateInformation" /> as a comma separated string
        /// </summary>
        /// <returns> A comma separated string containing PUID,ANID,OPID,CID </returns>
        internal string GetCsvString() => $"{this.Puid},{this.Anid},{this.Opid},{this.Cid}";
    }
}
