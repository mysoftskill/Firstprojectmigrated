namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using AutoMapper;

    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;

    /// <summary>
    /// Sets up the AutoMapper configuration for this component.
    /// </summary>
    public class MappingProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingProfile"/> class.
        /// </summary>
        public MappingProfile()
        {
            this.CreateMap<AssetGroupVariant, ExtendedAssetGroupVariant>()
                .ForMember(dest => dest.Capabilities, config => config.Ignore())
                .ForMember(dest => dest.DataTypes, config => config.Ignore())
                .ForMember(dest => dest.SubjectTypes, config => config.Ignore());
            this.CreateMap<VariantRequest, ExtendedVariantRequest>();
        }
    }
}
