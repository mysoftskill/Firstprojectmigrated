namespace Microsoft.PrivacyServices.DataManagement.Common
{
    using System;

    /// <summary>
    /// Extension methods for Guids.
    /// </summary>
    public static class GuidExtentions
    {
        /// <summary>
        /// Determines if the Guid is set.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>True if set.</returns>
        public static bool IsSet(this Guid value)
        {
            return value != Guid.Empty;
        }

        /// <summary>
        /// Determines if the Guid is set.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>True if set.</returns>
        public static bool IsSet(this Guid? value)
        {
            return value.HasValue && value.Value.IsSet();
        }
    }
}