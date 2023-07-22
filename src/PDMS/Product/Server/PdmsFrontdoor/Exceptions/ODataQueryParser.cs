namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.OData.UriParser;

    /// <summary>
    /// A utility class to help parse the OData query options.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ODataQueryParser
    {
        /// <summary>
        /// Determine whether or not the given property should be expanded.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="queryOption">The query option.</param>
        /// <param name="nestedClause">The nested select expand clause for further expansion.</param>
        /// <returns>True if expansion should occur or false.</returns>
        public static bool ShouldExpand(string propertyName, SelectExpandQueryOption queryOption, out SelectExpandClause nestedClause)
        {
            nestedClause = null;

            if (queryOption != null)
            {
                return ShouldExpand(propertyName, queryOption.SelectExpandClause, out nestedClause);
            }

            return false;
        }

        /// <summary>
        /// Determine whether or not the given property should be expanded.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="selectExpandClause">The select expand clause.</param>
        /// <param name="nestedClause">The nested select expand clause for further expansion.</param>
        /// <returns>True if expansion should occur or false.</returns>
        public static bool ShouldExpand(string propertyName, SelectExpandClause selectExpandClause, out SelectExpandClause nestedClause)
        {
            nestedClause = null;

            foreach (var selectExpand in selectExpandClause.SelectedItems)
            {
                var expansion = selectExpand as ExpandedNavigationSelectItem;

                if (expansion != null && expansion.NavigationSource.Name == propertyName)
                {
                    nestedClause = expansion.SelectAndExpand;
                    return true;
                }
            }

            return false;
        }
    }
}