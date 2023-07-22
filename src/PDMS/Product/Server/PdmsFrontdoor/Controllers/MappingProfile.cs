namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AutoMapper;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;

    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Sets up the AutoMapper configuration for this translating between the Core models
    /// and the DataAccess models.
    /// </summary>
    public class MappingProfile : Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingProfile"/> class.
        /// </summary>
        public MappingProfile()
        {
            Policy policy = Policies.Current; // TODO: Replace with dependency injection.

            this.CreateMap<Core.DataOwner, DataOwner>().MapCoreEntityProperties();
            this.CreateMap<DataOwner, Core.DataOwner>()
                .ForMember(
                    dest => dest.WriteSecurityGroups,
                    config => config.MapFrom((src, dest) => ConstructMany("writeSecurityGroups", src.WriteSecurityGroups, ConstructGuid)))
                .ForMember(
                    dest => dest.TagSecurityGroups,
                    config => config.MapFrom((src, dest) => ConstructMany("tagSecurityGroups", src.TagSecurityGroups, ConstructGuid)))
                .ForMember(
                    dest => dest.TagApplicationIds,
                    config => config.MapFrom((src, dest) => ConstructMany("tagApplicationIds", src.TagApplicationIds, ConstructGuid)))
                .ForMember(
                    dest => dest.AlertContacts,
                    config => config.MapFrom((src, dest) => ConstructMany("alertContacts", src.AlertContacts, ConstructEmailAddress)))
                .ForMember(
                    dest => dest.AnnouncementContacts,
                    config => config.MapFrom((src, dest) => ConstructMany("announcementContacts", src.AnnouncementContacts, ConstructEmailAddress)))
                .ForMember(
                    dest => dest.SharingRequestContacts,
                    config => config.MapFrom((src, dest) => ConstructMany("sharingRequestContacts", src.SharingRequestContacts, ConstructEmailAddress)))
                .MapApiEntityProperties(this);

            this.CreateMap<Core.ServiceTree, ServiceTree>();
            this.CreateMap<ServiceTree, Core.ServiceTree>();

            this.CreateMap<Core.DeleteAgent, DeleteAgent>().MapCoreEntityProperties();

            this.CreateMap<DeleteAgent, Core.DeleteAgent>()
                            .ForMember(dest => dest.DerivedEntityType, config => config.Ignore())
                            .MapApiEntityProperties(this);

            this.CreateMap<Core.AgentRegistrationStatus, AgentRegistrationStatus>();
            this.CreateMap<AgentRegistrationStatus, Core.AgentRegistrationStatus>();

            this.CreateMap<Core.AssetGroupRegistrationStatus, AssetGroupRegistrationStatus>();
            this.CreateMap<AssetGroupRegistrationStatus, Core.AssetGroupRegistrationStatus>();

            this.CreateMap<Core.AssetRegistrationStatus, AssetRegistrationStatus>();
            this.CreateMap<AssetRegistrationStatus, Core.AssetRegistrationStatus>();

            this.CreateMap<Core.RegistrationState, RegistrationState>();
            this.CreateMap<RegistrationState, Core.RegistrationState>();

            this.CreateMap<Core.AssetGroup, AssetGroup>().MapCoreEntityProperties();
            this.CreateMap<AssetGroup, Core.AssetGroup>()
                .ForMember(dest => dest.ComplianceState, config => config.Ignore())
                .ForMember(dest => dest.QualifierParts, config => config.Ignore())
                .MapApiEntityProperties(this);

            this.CreateMap<Core.Inventory, Inventory>().MapCoreEntityProperties();
            this.CreateMap<Inventory, Core.Inventory>()
                .ForMember(
                    dest => dest.DocumentationLink,
                    config => config.MapFrom((src, dest) => ConstructUri("documentationLink", src.DocumentationLink)))
                .MapApiEntityProperties(this);

            this.CreateMap<Core.VariantDefinition, VariantDefinition>().MapCoreEntityProperties();
            this.CreateMap<VariantDefinition, Core.VariantDefinition>().MapApiEntityProperties(this);

            this.CreateMap<Core.VariantDefinitionState, VariantDefinitionState>();
            this.CreateMap<VariantDefinitionState, Core.VariantDefinitionState>();
            this.CreateMap<Core.VariantDefinitionReason, VariantDefinitionReason>();
            this.CreateMap<VariantDefinitionReason, Core.VariantDefinitionReason>();

            this.CreateMap<Core.AssetGroupVariant, AssetGroupVariant>();
            this.CreateMap<AssetGroupVariant, Core.AssetGroupVariant>()
                .ForMember(
                    dest => dest.TfsTrackingUris,
                    config => config.MapFrom((src, dest) => ConstructMany("tfsTrackingUris", src.TfsTrackingUris, ConstructUri)));

            this.CreateMap<Core.ComplianceState, ComplianceState>();
            this.CreateMap<ComplianceState, Core.ComplianceState>();

            this.CreateMap<Core.SharingRequest, SharingRequest>().MapCoreEntityProperties();
            this.CreateMap<SharingRequest, Core.SharingRequest>().MapApiEntityProperties(this);
            this.CreateMap<Core.SharingRelationship, SharingRelationship>();
            this.CreateMap<SharingRelationship, Core.SharingRelationship>();

            this.CreateMap<Core.VariantRequest, VariantRequest>().MapCoreEntityProperties();
            this.CreateMap<VariantRequest, Core.VariantRequest>()
                .ForMember(
                    dest => dest.WorkItemUri,
                    config => config.MapFrom((src, dest) => ConstructUri("workItemUri", src.WorkItemUri)))
                .MapApiEntityProperties(this);
            this.CreateMap<Core.VariantRelationship, VariantRelationship>();
            this.CreateMap<VariantRelationship, Core.VariantRelationship>();

            this.CreateMap<Core.SetAgentRelationshipParameters, SetAgentRelationshipParameters>();
            this.CreateMap<SetAgentRelationshipParameters, Core.SetAgentRelationshipParameters>();
            this.CreateMap<Core.SetAgentRelationshipParameters.Action, SetAgentRelationshipParameters.Action>();
            this.CreateMap<SetAgentRelationshipParameters.Action, Core.SetAgentRelationshipParameters.Action>();
            this.CreateMap<Core.SetAgentRelationshipParameters.ActionType, SetAgentRelationshipParameters.ActionType>();
            this.CreateMap<SetAgentRelationshipParameters.ActionType, Core.SetAgentRelationshipParameters.ActionType>();
            this.CreateMap<Core.SetAgentRelationshipParameters.Relationship, SetAgentRelationshipParameters.Relationship>();
            this.CreateMap<SetAgentRelationshipParameters.Relationship, Core.SetAgentRelationshipParameters.Relationship>();

            this.CreateMap<Core.SetAgentRelationshipResponse, SetAgentRelationshipResponse>();
            this.CreateMap<SetAgentRelationshipResponse, Core.SetAgentRelationshipResponse>();
            this.CreateMap<Core.SetAgentRelationshipResponse.AssetGroupResult, SetAgentRelationshipResponse.AssetGroupResult>();
            this.CreateMap<SetAgentRelationshipResponse.AssetGroupResult, Core.SetAgentRelationshipResponse.AssetGroupResult>();
            this.CreateMap<Core.SetAgentRelationshipResponse.CapabilityResult, SetAgentRelationshipResponse.CapabilityResult>();
            this.CreateMap<SetAgentRelationshipResponse.CapabilityResult, Core.SetAgentRelationshipResponse.CapabilityResult>();
            this.CreateMap<Core.SetAgentRelationshipResponse.StatusType, SetAgentRelationshipResponse.StatusType>();
            this.CreateMap<SetAgentRelationshipResponse.StatusType, Core.SetAgentRelationshipResponse.StatusType>();

            this.CreateMap<IDictionary<Guid, Core.SharingRelationship>, IEnumerable<SharingRelationship>>()
                .ConstructUsing((src, ctx) =>
                    src
                    .Select(c =>
                    {
                        c.Value.AssetGroupId = c.Key;
                        return c.Value;
                    })
                    .Select(c => ctx.Mapper.Map<SharingRelationship>(c)));

            this.CreateMap<IEnumerable<SharingRelationship>, IDictionary<Guid, Core.SharingRelationship>>()
                .ConstructUsing((src, ctx) =>
                {
                    var values = src.Select(c => ctx.Mapper.Map<Core.SharingRelationship>(c));
                    var result = new Dictionary<Guid, Core.SharingRelationship>();

                    foreach (var value in values)
                    {
                        if (!result.ContainsKey(value.AssetGroupId))
                        {
                            result.Add(value.AssetGroupId, value);
                        }
                        else
                        {
                            throw new InvalidArgumentError($"relationships", value.AssetGroupId.ToString(), "Duplicate asset group id found.");
                        }
                    }

                    return result;
                });

            this.CreateMap<IDictionary<Guid, Core.VariantRelationship>, IEnumerable<VariantRelationship>>()
                .ConstructUsing((src, ctx) =>
                    src
                    .Select(c =>
                    {
                        c.Value.AssetGroupId = c.Key;
                        return c.Value;
                    })
                    .Select(c => ctx.Mapper.Map<VariantRelationship>(c)));

            this.CreateMap<IEnumerable<VariantRelationship>, IDictionary<Guid, Core.VariantRelationship>>()
                .ConstructUsing((src, ctx) =>
                {
                    var values = src.Select(c => ctx.Mapper.Map<Core.VariantRelationship>(c));
                    var result = new Dictionary<Guid, Core.VariantRelationship>();

                    foreach (var value in values)
                    {
                        if (!result.ContainsKey(value.AssetGroupId))
                        {
                            result.Add(value.AssetGroupId, value);
                        }
                        else
                        {
                            throw new InvalidArgumentError($"variantRelationships", value.AssetGroupId.ToString(), "Duplicate asset group id found.");
                        }
                    }

                    return result;
                });

            this.CreateMap<Core.DataAgent, DataAgent>()
                .Include<Core.DeleteAgent, DeleteAgent>()
                .MapCoreEntityProperties();

            this.CreateMap<DataAgent, Core.DataAgent>()
                .Include<DeleteAgent, Core.DeleteAgent>()
                .ForMember(dest => dest.DerivedEntityType, config => config.Ignore())
                .MapApiEntityProperties(this);

            this.CreateMap<IDictionary<Core.ReleaseState, Core.ConnectionDetail>, IEnumerable<ConnectionDetail>>()
                .ConstructUsing((src, ctx) =>
                    src
                    .Select(c =>
                    {
                        c.Value.ReleaseState = c.Key;
                        return c.Value;
                    })
                    .Select(c => ctx.Mapper.Map<ConnectionDetail>(c)));

            this.CreateMap<IEnumerable<ConnectionDetail>, IDictionary<Core.ReleaseState, Core.ConnectionDetail>>()
                .ConstructUsing((src, ctx) =>
                {
                    var values = src.Select(c => ctx.Mapper.Map<Core.ConnectionDetail>(c));
                    var result = new Dictionary<Core.ReleaseState, Core.ConnectionDetail>();

                    foreach (var value in values)
                    {
                        if (!result.ContainsKey(value.ReleaseState))
                        {
                            result.Add(value.ReleaseState, value);
                        }
                    }

                    return result;
                });

            this.CreateMap<Core.ConnectionDetail, ConnectionDetail>();
            this.CreateMap<ConnectionDetail, Core.ConnectionDetail>();
                
            this.CreateMap<Core.DataAsset, DataAsset>();
            this.CreateMap<DataAsset, Core.DataAsset>().ForMember(x => x.Tags, config => config.Ignore());

            this.CreateMap<AssetQualifier, string>()
                .ConstructUsing(src => ConstructOutboundAssetQualifierString(src));
            this.CreateMap<string, AssetQualifier>()
                .ConstructUsing(src => ConstructAssetQualifier("qualifier", src))
                .IgnoreAllPropertiesWithAnInaccessibleSetter()
                .ForMember(dest => dest.CustomProperties, config => config.Ignore());

            this.CreateMap<Core.TrackingDetails, TrackingDetails>();
            this.CreateMap<TrackingDetails, Core.TrackingDetails>();

            this.CreateMap<Core.Tag, Tag>();
            this.CreateMap<Tag, Core.Tag>();

            this.CreateMap<Core.Icm, Icm>();
            this.CreateMap<Icm, Core.Icm>();

            this.CreateMap<Core.Incident, Incident>();
            this.CreateMap<Incident, Core.Incident>();

            this.CreateMap<Core.IncidentInputParameters, IncidentInputParameters>();
            this.CreateMap<IncidentInputParameters, Core.IncidentInputParameters>();

            this.CreateMap<Core.IncidentResponseMetadata, IncidentResponseMetadata>();
            this.CreateMap<IncidentResponseMetadata, Core.IncidentResponseMetadata>();

            this.CreateMap<Core.RouteData, RouteData>();
            this.CreateMap<RouteData, Core.RouteData>();

            // Transfer Request.
            this.CreateMap<Core.TransferRequest, TransferRequest>().MapCoreEntityProperties();
            this.CreateMap<TransferRequest, Core.TransferRequest>().MapApiEntityProperties(this);

            this.CreateMap<Core.HistoryItem, HistoryItem>();
            this.CreateMap<Core.Entity, Entity>()
                .Include<Core.DataOwner, DataOwner>()
                .Include<Core.AssetGroup, AssetGroup>()
                .Include<Core.DeleteAgent, DeleteAgent>()
                .Include<Core.VariantDefinition, VariantDefinition>()
                .Include<Core.Inventory, Inventory>()
                .Include<Core.SharingRequest, SharingRequest>()
                .Include<Core.VariantRequest, VariantRequest>()
                .Include<Core.TransferRequest, TransferRequest>();

            // Uri mapping.
            this.CreateMap<Uri, string>().ConstructUsing(src => src.ToString());

            // Contacts mapping.
            this.CreateMap<System.Net.Mail.MailAddress, string>()
                .ConstructUsing(src => src.Address);

            // Policy property type mapping.
            this.CreateMap<string, DataTypeId>()
                .ConstructUsing(src => policy.DataTypes.CreateId(src))
                .ForMember(dest => dest.Value, config => config.Ignore());

            this.CreateMap<string, ProtocolId>()
                .ConstructUsing(src => policy.Protocols.CreateId(src))
                .ForMember(dest => dest.Value, config => config.Ignore());

            this.CreateMap<string, CapabilityId>()
                .ConstructUsing(src => policy.Capabilities.CreateId(src))
                .ForMember(dest => dest.Value, config => config.Ignore());

            this.CreateMap<string, OptionalFeatureId>()
                .ConstructUsing(src => policy.OptionalFeatures.CreateId(src))
                .ForMember(dest => dest.Value, config => config.Ignore());

            this.CreateMap<string, SubjectTypeId>()
                .ConstructUsing(src => policy.SubjectTypes.CreateId(src))
                .ForMember(dest => dest.Value, config => config.Ignore());

            this.CreateMap<string, CloudInstanceId>()
                .ConstructUsing(src => policy.CloudInstances.CreateId(src))
                .ForMember(dest => dest.Value, config => config.Ignore());
                
            this.CreateMap<string, DataResidencyInstanceId>()
                .ConstructUsing(src => policy.DataResidencyInstances.CreateId(src))
                .ForMember(dest => dest.Value, config => config.Ignore());

            this.CreateMap<Id, string>().ConstructUsing(src => src.Value);

            Func<CoreException, ServiceException> getExceptionMapping = (src) =>
            {
                if (src is MissingPropertyException nullError)
                {
                    return new NullArgumentError(nullError.ParamName, nullError.Message);
                }

                if (src is InvalidPropertyException invalidError)
                {
                    var error = new InvalidArgumentError(invalidError.ParamName, invalidError.Value, invalidError.Message);

                    if (invalidError is InvalidCharacterException)
                    {
                        error.ServiceError.InnerError.NestedError = new UnsupportedCharacterError();
                    }

                    if (invalidError is MutuallyExclusiveException mutuallyExclusiveError)
                    {
                        error.ServiceError.InnerError.NestedError = new MutuallyExclusiveError(mutuallyExclusiveError.Source);
                    }

                    return error;
                }

                if (src is EntityNotFoundException notFoundError)
                {
                    return new EntityNotFoundError(notFoundError.Id, notFoundError.EntityType);
                }

                if (src is ServiceTreeNotFoundException serviceTreeNotFoundError)
                {
                    var error = new ServiceTreeNotFoundError(serviceTreeNotFoundError.Id);

                    if (serviceTreeNotFoundError is ServiceNotFoundException)
                    {
                        error.ServiceError.InnerError.NestedError = new ServiceNotFoundError();
                    }

                    return error;
                }

                if (src is SecurityGroupNotFoundException securityGroupNotFoundError)
                {
                    return new SecurityGroupNotFoundError(securityGroupNotFoundError.Id);
                }

                if (src is ETagMismatchException etagMismatchError)
                {
                    return new ETagMismatchError(etagMismatchError.Message, etagMismatchError.Value);
                }

                if (src is MissingWritePermissionException missingWritePermissionError)
                {
                    var error = new UserNotAuthorizedError(missingWritePermissionError.UserName, missingWritePermissionError.Role, missingWritePermissionError.Message);

                    switch (missingWritePermissionError)
                    {
                        case ServiceTreeMissingWritePermissionException serviceTreeMissingWritePermissionError:
                            error.ServiceError.InnerError.NestedError = new ServiceTreeNotAuthorizedError(serviceTreeMissingWritePermissionError.ServiceId);
                            break;
                        case SecurityGroupMissingWritePermissionException securityGroupMissingWritePermissionError:
                            error.ServiceError.InnerError.NestedError = new SecurityGroupNotAuthorizedError(securityGroupMissingWritePermissionError.SecurityGroups);
                            break;
                    }

                    return error;
                }

                if (src is ConflictException conflictException)
                {
                    var error = new EntityConflictError(conflictException);

                    if (conflictException is AlreadyOwnedException alreadyOwnedException)
                    {
                        error.ServiceError.InnerError.NestedError = new AlreadyOwnedError(alreadyOwnedException.OwnerId);
                    }

                    return error;
                }

                // General fallback, but should not be hit.
                return new InvalidRequestError(src.Message);
            };

            this.CreateMap<CoreException, ServiceException>()
                .ConstructUsing(src => getExceptionMapping(src))
                .ForMember(dest => dest.StatusCode, opt => opt.Ignore()) // Set internally.
                .ForMember(dest => dest.ServiceError, opt => opt.Ignore()); // Set internally.
        }

        /// <summary>
        /// Create a guid from a string.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <returns>The converted value.</returns>
        internal static Guid ConstructGuid(string propertyName, string value)
        {
            if (value == null)
            {
                return Guid.Empty; // Internally, we treat Guid.Empty as null.
            }
            
            if (!Guid.TryParse(value, out var result))
            {
                throw new InvalidArgumentError(propertyName, value, "Invalid guid.");
            }
            
            return result;
        }

        /// <summary>
        /// Create an asset qualifier from a string.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value.</param>
        /// <returns>The converted value.</returns>
        internal static AssetQualifier ConstructAssetQualifier(string propertyName, string value)
        {
            try
            {
                return AssetQualifier.Parse(value);
            }
            catch
            {
                throw new InvalidArgumentError(propertyName, value, "Invalid asset qualifier.");
            }
        }

        /// <summary>
        /// Create a normalized assetQualifier string for returning to the caller in the APIs.
        /// </summary>
        /// <param name="qualifier">Asset Qualifier.</param>
        /// <returns>Asset Qualifier string.</returns>
        internal static string ConstructOutboundAssetQualifierString(AssetQualifier qualifier)
        {
            var copy = AssetQualifier.Parse(qualifier.Value);

            return copy.Value;
        }

        private static System.Net.Mail.MailAddress ConstructEmailAddress(string propertyName, string value)
        {
            try
            {
                return new System.Net.Mail.MailAddress(value);
            }
            catch
            {
                throw new InvalidArgumentError(propertyName, value, "Invalid email address.");
            }
        }

        private static Uri ConstructUri(string propertyName, string value)
        {
            if (value == null)
            {
                return null;
            }
            
            if (!Uri.TryCreate(value, UriKind.Absolute, out var result))
            {
                throw new InvalidArgumentError(propertyName, value, "Invalid uri.");
            }
            
            return result;
        }

        private static IEnumerable<T> ConstructMany<T>(string propertyName, IEnumerable<string> values, Func<string, string, T> mapping)
        {
            return values?.Select(v => mapping(propertyName, v));
        }
    }
}
