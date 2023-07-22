// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System.Globalization;
    using System.Security.Principal;

    /// <summary>
    ///     MsaSiteIdentity
    /// </summary>
    public class MsaSiteIdentity : IIdentity
    {
        /// <summary>
        ///     Gets the type of authentication used.
        /// </summary>
        public virtual string AuthenticationType => this.AuthType.ToString();

        /// <summary>
        ///     Gets the type of the authentication: self, obo
        /// </summary>
        public AuthType AuthType { get; protected set; }

        /// <summary>
        ///     Gets or sets the caller msa site identifier.
        /// </summary>
        public long CallerMsaSiteId { get; protected set; }

        /// <summary>
        ///     Gets or sets the name of the caller.
        /// </summary>
        public string CallerName { get; protected set; }

        /// <summary>
        ///     Gets the caller name formatted.
        /// </summary>
        public string CallerNameFormatted => FormatCallerName(this.CallerName, this.CallerMsaSiteId);

        /// <summary>
        ///     Gets a value that indicates whether the user has been authenticated.
        /// </summary>
        public bool IsAuthenticated { get; protected set; }

        /// <summary>
        ///     Gets the name of the current user.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MsaSiteIdentity" /> class.
        /// </summary>
        /// <param name="callerName">Name of the caller.</param>
        /// <param name="siteId">The site identifier.</param>
        public MsaSiteIdentity(string callerName, long siteId)
        {
            this.AuthType = AuthType.MsaSite;
            this.CallerName = callerName;
            this.CallerMsaSiteId = siteId;
            this.IsAuthenticated = true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{MsaSiteIdentity: {this.CallerMsaSiteId}}}";
        }

        /// <summary>
        ///     Formats the name of the caller.
        /// </summary>
        /// <param name="callerName">Name of the caller.</param>
        /// <param name="siteId">The site identifier.</param>
        protected static string FormatCallerName(string callerName, long siteId)
        {
            string siteIdString = siteId.ToString(CultureInfo.InvariantCulture);

            if (callerName == null)
            {
                return siteIdString;
            }

            return callerName + "_" + siteIdString;
        }
    }
}
