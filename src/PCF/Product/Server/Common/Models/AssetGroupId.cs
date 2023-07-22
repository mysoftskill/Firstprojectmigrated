namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// Defines an Asset Group ID, which is a GUID.
    /// </summary>
    public sealed class AssetGroupId : Identifier
    {
        /// <summary>
        /// Creates a new Asset Group ID from the given string (which is assumed to be a GUID).
        /// </summary>
        public AssetGroupId(string value) : base(value, true)
        {
        }

        /// <summary>
        /// Creates a new Asset group ID from the given GUID.
        /// </summary>
        public AssetGroupId(Guid value) : base(value)
        {
        }
    }
}