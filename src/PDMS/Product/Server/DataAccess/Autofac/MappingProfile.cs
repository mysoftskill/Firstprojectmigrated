namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac
{
    using AutoMapper;

    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;

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
            this.CreateMap<DataOwner, DataOwner>()
                .ForMember(dest => dest.TrackingDetails, config => config.Ignore());

            // Ignore service generated properties so that they are immutable.
            this.CreateMap<ServiceTree, ServiceTree>()
                .ForMember(dest => dest.DivisionId, config => config.Ignore())
                .ForMember(dest => dest.DivisionName, config => config.Ignore())
                .ForMember(dest => dest.Level, config => config.Ignore())
                .ForMember(dest => dest.OrganizationId, config => config.Ignore())
                .ForMember(dest => dest.OrganizationName, config => config.Ignore())
                .ForMember(dest => dest.ServiceAdmins, config => config.Ignore())
                .ForMember(dest => dest.ServiceGroupName, config => config.Ignore())
                .ForMember(dest => dest.ServiceName, config => config.Ignore())
                .ForMember(dest => dest.TeamGroupName, config => config.Ignore());

            this.CreateMap<AssetGroup, AssetGroup>()
                .ForMember(dest => dest.TrackingDetails, config => config.Ignore())
                .ForMember(dest => dest.ComplianceState, config => config.Ignore()); // This is pulled from audit report, so make it immutable.

            this.CreateMap<Inventory, Inventory>()
                .ForMember(dest => dest.TrackingDetails, config => config.Ignore());

            this.CreateMap<VariantDefinition, VariantDefinition>()
                .ForMember(dest => dest.TrackingDetails, config => config.Ignore());

            this.CreateMap<DeleteAgent, DeleteAgent>()
                .ForMember(dest => dest.TrackingDetails, config => config.Ignore())
                .ForMember(dest => dest.HasSharingRequests, config => config.Ignore()) // This is a calculated property that is never stored.
                .ForMember(dest => dest.Capabilities, config => config.Ignore()); // This is server generated, so make it immutable.

            this.CreateMap<AssetQualifier, AssetQualifier>()
                .ForMember(dest => dest.CustomProperties, config => config.Ignore())
                .ConstructUsing(src => AssetQualifier.CreateFromDictionary(src.Properties));

            this.CreateMap<ServiceGroup, ServiceTree>()
                .ForMember(dest => dest.ServiceGroupId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ServiceGroupName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ServiceAdmins, opt => opt.MapFrom(src => src.AdminUserNames))
                .ForMember(dest => dest.TeamGroupId, opt => opt.MapFrom<string>(src => null))
                .ForMember(dest => dest.TeamGroupName, opt => opt.MapFrom<string>(src => null))
                .ForMember(dest => dest.ServiceId, opt => opt.MapFrom<string>(src => null))
                .ForMember(dest => dest.ServiceName, opt => opt.MapFrom<string>(src => null));

            this.CreateMap<TeamGroup, ServiceTree>()
                .ForMember(dest => dest.TeamGroupId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TeamGroupName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ServiceAdmins, opt => opt.MapFrom(src => src.AdminUserNames))
                .ForMember(dest => dest.ServiceId, opt => opt.MapFrom<string>(src => null))
                .ForMember(dest => dest.ServiceName, opt => opt.MapFrom<string>(src => null));

            this.CreateMap<Service, ServiceTree>()
                .ForMember(dest => dest.ServiceId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ServiceAdmins, opt => opt.MapFrom(src => src.AdminUserNames));

            this.CreateMap<VariantRequest, VariantRequest>()
                .ForMember(dest => dest.TrackingDetails, config => config.Ignore());

            this.CreateMap<TransferRequest, TransferRequest>()
                .ForMember(dest => dest.TrackingDetails, config => config.Ignore());
        }
    }
}