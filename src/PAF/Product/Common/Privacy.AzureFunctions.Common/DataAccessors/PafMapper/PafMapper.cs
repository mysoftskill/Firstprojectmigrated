namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System;
    using AutoMapper;

    /// <inheritdoc />
    public class PafMapper : IPafMapper
    {
        private readonly MapperConfiguration config;

        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PafMapper"/> class.
        /// Constructor
        /// </summary>
        /// <param name="profile">Takes in a profile object to create a mapper  </param>
        public PafMapper(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            this.config = new MapperConfiguration(cfg => cfg.AddProfile(profile));
            this.mapper = this.config.CreateMapper();
        }

        /// <inheritdoc />
        public TDestination Map<TSource, TDestination>(TSource sourceItem)
        {
            return this.mapper.Map<TDestination>(sourceItem);
        }
    }
}
