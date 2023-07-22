namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
{
    /// <summary>
    /// A set of static extensions to string for constructing client filter request string.
    /// </summary>
    internal static class FilterRequestString
    {
        /// <summary>
        /// And two filter request string together.
        /// </summary>
        /// <param name="filterStr1">The first filter request string.</param>
        /// <param name="filterStr2">The second filter request string.</param>
        /// <returns>The composite string.</returns>
        internal static string And(this string filterStr1, string filterStr2)
        {
            if (!string.IsNullOrEmpty(filterStr1) && !string.IsNullOrEmpty(filterStr2))
            {
                return filterStr1 + " and " + filterStr2;
            }
            else if (!string.IsNullOrEmpty(filterStr1))
            {
                return filterStr1;
            }
            else if (!string.IsNullOrEmpty(filterStr2))
            {
                return filterStr2;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}