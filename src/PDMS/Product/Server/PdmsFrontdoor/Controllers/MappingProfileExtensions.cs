namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System;

    using AutoMapper;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;

    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Additional mapping functions. These are defined as extensions to support a Fluent API pattern.
    /// </summary>
    public static class MappingProfileExtensions
    {
        /// <summary>
        /// Maps Core Entity properties into API Entity properties.
        /// </summary>
        /// <typeparam name="T">The core type.</typeparam>
        /// <typeparam name="V">The API type.</typeparam>
        /// <param name="expression">The original mapping expression.</param>
        /// <returns>An altered mapping expression that includes the entity mapping behavior.</returns>
        public static IMappingExpression<T, V> MapCoreEntityProperties<T, V>(this IMappingExpression<T, V> expression)
            where T : Core.Entity
            where V : Entity
        {
            return expression;
        }

        /// <summary>
        /// Maps API Entity properties into Core Entity properties.
        /// </summary>
        /// <typeparam name="T">The core type.</typeparam>
        /// <typeparam name="V">The API type.</typeparam>
        /// <param name="expression">The original mapping expression.</param>
        /// <param name="profile">The profile that contains this mapping.</param>
        /// <returns>An altered mapping expression that includes the entity mapping behavior.</returns>
        public static IMappingExpression<T, V> MapApiEntityProperties<T, V>(this IMappingExpression<T, V> expression, Profile profile)
            where T : Entity
            where V : Core.Entity
        {
            return expression
                .ForMember(dest => dest.EntityType, config => config.Ignore())
                .ForMember(dest => dest.IsDeleted, config => config.Ignore())
                .ForMember(dest => dest.Id, config => config.MapFrom<Guid>((src, dest) => MappingProfile.ConstructGuid("id", src.Id)));
        }
    }
}
