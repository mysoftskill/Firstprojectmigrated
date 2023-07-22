//--------------------------------------------------------------------------------
// <copyright file="MsaId.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common
{
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// A class used to store the various user identification formats used by MSA.
    /// </summary>
    public class MsaId
    {
        /// <summary>
        /// The PUID (decimal).
        /// </summary>
        private long puidDecimal;

        /// <summary>
        /// The PUID (hex).
        /// </summary>
        private string puidHex;

        /// <summary>
        /// The anonymous-Id (hex).
        /// </summary>
        private string anidHex;

        /// <summary>
        /// The CID.
        /// </summary>
        private long? cid;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsaId"/> class.
        /// </summary>
        /// <param name="puid">The user PUID.</param>
        /// <param name="cid">The user CID.</param>
        /// <param name="userProxyTicket">The user proxy-token.</param>
        /// <param name="userPartnerTicket">The user partner ticket</param>
        public MsaId(long? puid, long? cid = null, string userProxyTicket = null, string userPartnerTicket = null)
        {
            this.PuidDecimal = puid ?? 0;
            this.cid = cid;
            this.UserProxyTicket = userProxyTicket;
            this.UserPartnerTicket = userPartnerTicket;
        }

        /// <summary>
        /// Gets or sets the various formats are updated when the PuidDecimal is set. This is to avoid converting at each request.
        /// </summary>
        public long PuidDecimal
        {
            get
            {
                return this.puidDecimal;
            }

            set
            {
                this.puidDecimal = value;
                this.puidHex = LiveIdUtils.ToHexFormat(this.puidDecimal);
                this.anidHex = LiveIdUtils.ToAnonymousHex(this.puidDecimal);
            }
        }

        /// <summary>
        /// Gets the highest 32-bits of the 64-bit PUID as an integer.
        /// </summary>
        public int PuidHigh
        {
            get { return (int)(this.PuidDecimal >> 32); }
        }

        /// <summary>
        /// Gets the lowest 32-bits of the 64-bit PUID as an integer.
        /// </summary>
        public int PuidLow
        {
            get { return (int)(this.puidDecimal & 0x00000000FFFFFFFF); }
        }

        /// <summary>
        /// Gets the PUID (hex).
        /// </summary>
        public string PuidHex
        {
            get { return this.puidHex; }
        }

        /// <summary>
        /// Gets the PUID (decimal).
        /// </summary>
        public string PuidDecimalString
        {
            get { return this.puidDecimal.ToString(CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Gets the ANID is not stored in decimal format because it will exceed the maximum value for long type.
        /// </summary>
        public string AnidHex
        {
            get { return this.anidHex; }
        }

        /// <summary>
        /// Gets the CID.
        /// </summary>
        public long? Cid
        {
            get { return this.cid; }
        }

        /// <summary>
        /// Gets the CID.
        /// </summary>
        public string CidString
        {
            get { return this.cid.HasValue ? this.cid.Value.ToString(CultureInfo.InvariantCulture) : null; }
        }

        /// <summary>
        /// Gets the CID (hex).
        /// </summary>
        public string CidHex
        {
            get { return this.cid.HasValue ? LiveIdUtils.ToHexFormat(this.cid.Value) : null; }
        }

        /// <summary>
        /// Gets the user-proxy-ticket.
        /// </summary>
        public string UserProxyTicket { get; private set; }

        /// <summary>
        /// Gets the users's compact ticket for use on partner site (only used in test environments)
        /// </summary>
        public string UserPartnerTicket { get; private set; }

        /// <summary>
        /// Converts the value of this instance to a <see cref="System.String"/>.
        /// </summary>
        /// <returns>A string whose value is the same as this instance.</returns>
        public override string ToString()
        {
            string puid = this.PuidDecimalString;
            string cidString = this.CidString;
            string anid = this.AnidHex;

            // UserProxyTicket not logged
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("(");
            if (puid != null)
            {
                stringBuilder.Append("Puid=").Append(puid).Append(", ");
            }

            if (cidString != null)
            {
                stringBuilder.Append("Cid=").Append(cidString).Append(", ");
            }

            if (anid != null)
            {
                stringBuilder.Append("Anid=").Append(anid);
            }

            stringBuilder.Append(")");

            return stringBuilder.ToString();
        }
    }
}
