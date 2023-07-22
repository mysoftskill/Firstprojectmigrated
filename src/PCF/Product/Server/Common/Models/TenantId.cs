namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// Defines a Tenant ID, which is a GUID.
    /// </summary>
    public sealed class TenantId : Identifier
    {
        /// <summary>
        /// Creates a new Tenant ID from the given string, which is assumed to be a valid GUID.
        /// </summary>
        public TenantId(string value) : base(value)
        {
        }

        /// <summary>
        /// Creates a new Tenant ID from the given GUID.
        /// </summary>
        public TenantId(Guid value) : base(value)
        {
        }
    }
}
