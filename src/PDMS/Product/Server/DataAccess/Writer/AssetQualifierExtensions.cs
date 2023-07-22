namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{
    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Extension methods for AssetQualifier class.
    /// </summary>
    public static class AssetQualifierExtensions
    {
        /// <summary>
        /// Checks if two AssetQualifiers are equivalent, based on normalization rules.
        /// </summary>
        /// <param name="left">Left side of the comparison.</param>
        /// <param name="right">Right side of the comparison.</param>
        /// <returns>True if the two asset qualifiers are equivalent.</returns>
        /// <remarks>NOTE: I will move this into the AssetQualifier library in an upcoming change - this is for expediency.</remarks>
        public static bool IsEquivalentTo(this AssetQualifier left, AssetQualifier right)
        {
            // Make copies so that we don't effect either side
            var leftCopy = AssetQualifier.Parse(left.Value);
            var rightCopy = AssetQualifier.Parse(right.Value);

            // Compare the normalized string versions of the qualifiers
            return leftCopy.Value.Equals(rightCopy.Value);
        }
    }
}
