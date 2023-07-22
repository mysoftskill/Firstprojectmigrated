namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// Defines a VariantId, which is a GUID.
    /// It takes any valid guid string and always returns a guid without dashes as the Guid Value. (value.ToString("n"))
    /// </summary>
    public sealed class VariantId : Identifier
    {
        /// <summary>
        /// Creates a new VariantId from the given string, which is assumed to be a valid GUID.
        /// </summary>
        public VariantId(string value) : base(value, true)
        {
        }

        /// <summary>
        /// Creates a new VariantId from the given GUID.
        /// </summary>
        public VariantId(Guid value) : base(value)
        {
        }
    }
}
