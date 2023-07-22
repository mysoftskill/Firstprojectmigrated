namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System.Collections.Generic;
    using AutoMapper;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;

    /// <summary>
    /// Create Paf Mapper object.
    /// </summary>
    public interface IPafMapper
    {
        /// <summary>
        /// Maps TSource object to TDestination object as defined in the mapping profile.
        /// </summary>
        /// <typeparam name="TSource">Source object type</typeparam>
        /// <typeparam name="TDestination">Destination object type</typeparam>
        /// <param name="sourceItem">Source item which needs to be mapped.</param>
        /// <returns>TDestination instance.</returns>
        TDestination Map<TSource, TDestination>(TSource sourceItem);
    }
}
