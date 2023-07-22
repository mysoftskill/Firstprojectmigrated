using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.MarketReadiness;
using Microsoft.Osgs.Core.Helpers;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.Osgs.Web.Core.Configuration;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.PrivacyServices.UX.Core.Flighting;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Core.ServiceTreeClient;
using Newtonsoft.Json;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;
using PdmsIdentityModels = Microsoft.PrivacyServices.Identity;
using PdmsModels = Microsoft.PrivacyServices.UX.Models.Pdms;
using ServiceTreeApiModels = Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
using ServiceTreeModels = Microsoft.PrivacyServices.UX.Models.ServiceTree;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    [Authorize("Api")]
    [HandleAjaxErrors(ErrorCode = "generic", CustomErrorHandler = typeof(PdmsClientExceptionHandler))]
    public class ApiController : Controller
    {
        private readonly IHostingEnvironment hostingEnvironment;

        private readonly IParallaxConfiguration parallax;

        private readonly IAzureADConfig aadConfig;

        private readonly IClientProviderAccessor<IPdmsClientProvider> pdmsClientProviderAccessor;

        private readonly IClientProviderAccessor<IServiceTreeClientProvider> serviceTreeClientProviderAccessor;

        private readonly IClientProviderAccessor<IGroundControlProvider> groundControlProviderAccessor;

        private readonly IPrivacyPolicyAccessor privacyPolicy;

        private readonly IDictionary<PdmsModels.SetAgentRelationshipRequest.Capability, Policy.CapabilityId> capabilitiesUxToPdmsMapping =
            new Dictionary<PdmsModels.SetAgentRelationshipRequest.Capability, Policy.CapabilityId>()
            {
                { PdmsModels.SetAgentRelationshipRequest.Capability.Delete, Policy.Policies.Current.Capabilities.Ids.Delete },
                { PdmsModels.SetAgentRelationshipRequest.Capability.Export, Policy.Policies.Current.Capabilities.Ids.Export }
            };

        private readonly IDictionary<PdmsModels.SetAgentRelationshipRequest.ActionVerb, PdmsApiModelsV2.SetAgentRelationshipParameters.ActionType> actionTypeUxToPdmsMapping =
            new Dictionary<PdmsModels.SetAgentRelationshipRequest.ActionVerb, PdmsApiModelsV2.SetAgentRelationshipParameters.ActionType>()
            {
                { PdmsModels.SetAgentRelationshipRequest.ActionVerb.Set, PdmsApiModelsV2.SetAgentRelationshipParameters.ActionType.Set },
                { PdmsModels.SetAgentRelationshipRequest.ActionVerb.Clear, PdmsApiModelsV2.SetAgentRelationshipParameters.ActionType.Clear }
            };

        private readonly IMarketReadinessData marketReadinessData;

        private readonly IAntiforgery antiforgery;

        public ApiController(
            IHostingEnvironment hostingEnvironment,
            IParallaxConfiguration parallax,
            IAzureADConfig aadConfig,
            IClientProviderAccessor<IPdmsClientProvider> pdmsClientProviderAccessor,
            IClientProviderAccessor<IServiceTreeClientProvider> serviceTreeClientProviderAccessor,
            IClientProviderAccessor<IGroundControlProvider> groundControlProviderAccessor,
            IPrivacyPolicyAccessor privacyPolicy,
            IMarketReadinessData marketReadinessData,
            IAntiforgery antiforgery)
        {
            this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            this.parallax = parallax ?? throw new ArgumentNullException(nameof(parallax));
            this.aadConfig = aadConfig ?? throw new ArgumentNullException(nameof(aadConfig));
            this.pdmsClientProviderAccessor = pdmsClientProviderAccessor ?? throw new ArgumentNullException(nameof(pdmsClientProviderAccessor));
            this.serviceTreeClientProviderAccessor = serviceTreeClientProviderAccessor ?? throw new ArgumentNullException(nameof(serviceTreeClientProviderAccessor));
            this.groundControlProviderAccessor = groundControlProviderAccessor ?? throw new ArgumentNullException(nameof(groundControlProviderAccessor));
            this.privacyPolicy = privacyPolicy ?? throw new ArgumentNullException(nameof(privacyPolicy));
            this.marketReadinessData = marketReadinessData ?? throw new ArgumentNullException(nameof(marketReadinessData));
            this.antiforgery = antiforgery ?? throw new ArgumentNullException(nameof(antiforgery));
        }

        public IActionResult Unknown()
        {
            return NotFound();
        }

        [HttpGet]
        public IActionResult GetCsrfToken()
        {
            return Json(new
            {
                token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken
            });
        }

        [HttpGet]
        [ResponseCache(Duration = 1209600 /* Two weeks, in seconds */)]
        public IActionResult GetCountriesList()
        {
            var requestCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var requestCulture = requestCultureFeature.RequestCulture;
            var cultureCode = requestCulture.UICulture.Name;

            var result = marketReadinessData.GetCountriesWithTranslation(cultureCode).Select(c => new
            {
                CountryName = c.Translation.Value,
                IsoCode = c.Translation.IsoCode
            }).Concat(new[]
            {
                new
                {
                    CountryName = "Unknown/Unspecified",
                    IsoCode = "--"
                }
            }).OrderBy(c => c.CountryName);

            return Json(result);
        }

        /// <summary>
        /// This method determines if user has access for Incident Manager.
        /// Returns 200 when "IncidentManager" claim is found on user ticket, otherwise 403.
        /// </summary>
        [HttpGet]
        [Authorize("IncidentManager")]
        public void HasAccessForIncidentManager()
        {
            // TODO Bug 15983410: This should not be called after the claim exists for the authenticated user.
        }

        [HttpGet]
        public IActionResult GetPrivacyPolicy()
        {
            var dataTypes = privacyPolicy.Get().DataTypes.Set.ToDictionary(dataType => dataType.Id.Value, dataType => new PdmsModels.PrivacyDataType()
            {
                Id = dataType.Id.Value,
                Name = dataType.Name,
                Description = dataType.Description,
                Capabilities = dataType.RequiredCapabilities.Select(capability => capability.Id.Value)
            });
            var capabilities = privacyPolicy.Get().Capabilities.Set.ToDictionary(capability => capability.Id.Value, capability => new PdmsModels.PrivacyCapability()
            {
                Id = capability.Id.Value,
                Name = capability.Name,
                Description = capability.Description,
                Protocols = capability.SupportedProtocols.Select(protocol => protocol.Id.Value)
            });
            var protocols = privacyPolicy.Get().Protocols.Set.ToDictionary(protocol => protocol.Id.Value, protocol => new PdmsModels.PrivacyProtocol()
            {
                Id = protocol.Id.Value,
                Name = protocol.Name,
                Description = protocol.Description
            });

            return Json(new PdmsModels.PrivacyPolicy()
            {
                DataTypes = dataTypes,
                Capabilities = capabilities,
                Protocols = protocols
            });
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAssetGroup([FromBody] PdmsModels.AssetGroup assetGroup)
        {
            var pdmsAssetGroup = new PdmsApiModelsV2.AssetGroup()
            {
                DeleteAgentId = assetGroup.DeleteAgentId,
                ExportAgentId = assetGroup.ExportAgentId,
                OwnerId = assetGroup.OwnerId,
                Qualifier = PdmsIdentityModels.AssetQualifier.CreateFromDictionary(assetGroup.Qualifier.Properties),
                IsDeleteAgentInheritanceBlocked = assetGroup.IsDeleteAgentInheritanceBlocked,
                IsExportAgentInheritanceBlocked = assetGroup.IsExportAgentInheritanceBlocked,
                IsVariantsInheritanceBlocked = assetGroup.IsVariantsInheritanceBlocked
            };

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.CreateAsync(
                pdmsAssetGroup, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(ConvertToAssetGroupModel(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssetGroup([FromBody] PdmsModels.AssetGroup assetGroup)
        {
            var pdmsAssetGroup = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.ReadAsync(
                assetGroup.Id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            // These are the only asset group properties users are allowed to update.
            pdmsAssetGroup.IsDeleteAgentInheritanceBlocked = assetGroup.IsDeleteAgentInheritanceBlocked;
            pdmsAssetGroup.IsExportAgentInheritanceBlocked = assetGroup.IsExportAgentInheritanceBlocked;
            pdmsAssetGroup.IsVariantsInheritanceBlocked = assetGroup.IsVariantsInheritanceBlocked;

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.UpdateAsync(
                pdmsAssetGroup, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(ConvertToAssetGroupModel(result));
        }

        [HttpGet]
        public async Task<IActionResult> GetServicesByName(string nameSubstring)
        {
            var result = (await serviceTreeClientProviderAccessor.ProviderInstance.Instance.FindServicesByName(nameSubstring,
                await serviceTreeClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(FilterOnlySupportedServiceTreeLevels(result).Select(ConvertToServiceTreeSearchResult));
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceById(string id, ServiceTreeModels.ServiceTreeEntityKind kind)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var serviceTreeEntityId = new Guid(id);
            var requestContext = await serviceTreeClientProviderAccessor.ProviderInstance.CreateNewRequestContext();

            ServiceTreeApiModels.ServiceTreeNode serviceTreeRecord;
            switch (kind)
            {
                case ServiceTreeModels.ServiceTreeEntityKind.Service:
                    serviceTreeRecord = (await serviceTreeClientProviderAccessor.ProviderInstance.Instance.ReadServiceWithExtendedProperties(
                        serviceTreeEntityId, requestContext)).Response;
                    break;

                case ServiceTreeModels.ServiceTreeEntityKind.TeamGroup:
                    serviceTreeRecord = (await serviceTreeClientProviderAccessor.ProviderInstance.Instance.ReadTeamGroupWithExtendedProperties(
                        serviceTreeEntityId, requestContext)).Response;
                    break;

                case ServiceTreeModels.ServiceTreeEntityKind.ServiceGroup:
                    serviceTreeRecord = (await serviceTreeClientProviderAccessor.ProviderInstance.Instance.ReadServiceGroupWithExtendedProperties(
                        serviceTreeEntityId, requestContext)).Response;
                    break;

                default:
                    throw new NotSupportedException($"Service tree ID kind {kind} is not supported.");
            }

            return Json(ConvertToServiceTreeEntityDetails(serviceTreeRecord));
        }

        [HttpGet]
        public async Task<IActionResult> GetOwnersByAuthenticatedUser()
        {
            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.FindAllByAuthenticatedUserAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.DataOwnerExpandOptions.TrackingDetails | PdmsApiModelsV2.DataOwnerExpandOptions.ServiceTree)).Response;

            return Json(result.OrderByDescending(da => da.TrackingDetails?.UpdatedOn ?? DateTimeOffset.MinValue).Select(da => ConvertToDataOwnerModel(da)));
        }

        [HttpGet]
        public async Task<IActionResult> GetDataOwnerWithServiceTree(string id)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.ReadAsync(
                id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(), PdmsApiModelsV2.DataOwnerExpandOptions.ServiceTree)).Response;
            return Json(ConvertToDataOwnerModel(result));
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDataOwner([FromBody] PdmsModels.DataOwner owner)
        {
            var pdmsDataOwner = new PdmsApiModelsV2.DataOwner()
            {
                Name = owner.Name,
                Description = owner.Description,
                AlertContacts = owner.AlertContacts,
                AnnouncementContacts = owner.AnnouncementContacts,
                SharingRequestContacts = owner.SharingRequestContacts,
                WriteSecurityGroups = owner.WriteSecurityGroups,
                TagSecurityGroups = owner.TagSecurityGroups,
                TagApplicationIds = owner.TagApplicationIds,
                Icm = ConvertIcmInformation(owner, initialIcmConfig: null)
            };

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.CreateAsync(
                pdmsDataOwner, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(ConvertToDataOwnerModel(result));
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDataOwnerWithServiceTree([FromBody] PdmsModels.ServiceTreeOwner owner)
        {
            var pdmsDataOwner = new PdmsApiModelsV2.DataOwner()
            {
                WriteSecurityGroups = owner.WriteSecurityGroups,
                TagSecurityGroups = owner.TagSecurityGroups,
                TagApplicationIds = owner.TagApplicationIds,
                SharingRequestContacts = owner.SharingRequestContacts,
                Icm = ConvertIcmInformation(owner, initialIcmConfig: null)
            };
            AddServiceTreeInformation(pdmsDataOwner, owner);

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.CreateAsync(
                pdmsDataOwner, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(ConvertToDataOwnerModel(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDataOwner([FromBody] PdmsModels.DataOwner owner)
        {
            var pdmsDataOwner =
                (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.ReadAsync(
                    owner.Id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(), PdmsApiModelsV2.DataOwnerExpandOptions.None)).Response;
            pdmsDataOwner.Name = owner.Name;
            pdmsDataOwner.Description = owner.Description;
            pdmsDataOwner.AlertContacts = owner.AlertContacts;
            pdmsDataOwner.AnnouncementContacts = owner.AnnouncementContacts;
            pdmsDataOwner.SharingRequestContacts = owner.SharingRequestContacts;
            pdmsDataOwner.WriteSecurityGroups = owner.WriteSecurityGroups;
            pdmsDataOwner.TagSecurityGroups = owner.TagSecurityGroups;
            pdmsDataOwner.TagApplicationIds = owner.TagApplicationIds;
            pdmsDataOwner.Icm = ConvertIcmInformation(owner, initialIcmConfig: pdmsDataOwner.Icm);

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.UpdateAsync(
                pdmsDataOwner, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(ConvertToDataOwnerModel(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDataOwnerWithServiceTree([FromBody] PdmsModels.ServiceTreeOwner owner)
        {
            var pdmsDataOwner =
                (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.ReadAsync(
                    owner.Id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(), PdmsApiModelsV2.DataOwnerExpandOptions.ServiceTree)).Response;
            pdmsDataOwner.WriteSecurityGroups = owner.WriteSecurityGroups;
            pdmsDataOwner.TagSecurityGroups = owner.TagSecurityGroups;
            pdmsDataOwner.TagApplicationIds = owner.TagApplicationIds;
            pdmsDataOwner.SharingRequestContacts = owner.SharingRequestContacts;
            pdmsDataOwner.Icm = ConvertIcmInformation(owner, initialIcmConfig: pdmsDataOwner.Icm);

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.UpdateAsync(
                pdmsDataOwner, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            return Json(ConvertToDataOwnerModel(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkDataOwnerToServiceTree(string id, string serviceTreeId, ServiceTreeModels.ServiceTreeEntityKind serviceTreeIdKind)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var pdmsDataOwner =
                (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.ReadAsync(
                    id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(), PdmsApiModelsV2.DataOwnerExpandOptions.ServiceTree)).Response;
            AddServiceTreeInformation(pdmsDataOwner, serviceTreeId, serviceTreeIdKind);

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.ReplaceServiceIdAsync(
                pdmsDataOwner, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(ConvertToDataOwnerModel(result));
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task DeleteDataOwnerById(string id)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var pdmsDataOwner =
                (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.ReadAsync(
                    id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.DeleteAsync(
                id, pdmsDataOwner.ETag, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext());
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task DeleteDataAgentById(string id, bool overridePendingCommands = false)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var pdmsDataAgent = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.ReadAsync<PdmsApiModelsV2.DeleteAgent>(
                id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.DeleteAsync(
                id, pdmsDataAgent.ETag,
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                overridePendingCommands);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task DeleteAssetGroupById(string id)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var pdmsAssetGroup = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.ReadAsync(
                id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.DeleteAsync(
                id, pdmsAssetGroup.ETag, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext());
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetGroupById(string id)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.ReadAsync(
                id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(ConvertToAssetGroupModel(result));
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetGroupsByOwnerId(string ownerId)
        {
            EnsureArgument.NotNullOrWhiteSpace(ownerId, nameof(ownerId));

            var filter = new PdmsApiModelsV2.AssetGroupFilterCriteria()
            {
                OwnerId = ownerId
            };
            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.AssetGroupExpandOptions.None,
                filter
            )).Response;

            return Json(result.Select(ag => ConvertToAssetGroupModel(ag)));
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetGroupsCountByOwnerId(string ownerId)
        {
            EnsureArgument.NotNullOrWhiteSpace(ownerId, nameof(ownerId));

            // ReadByFiltersAsync supports paging, passing Count = 0 returns zero records as we only care about Total Count for this call
            var filter = new PdmsApiModelsV2.AssetGroupFilterCriteria()
            {
                OwnerId = ownerId,
                Count = 0
            };
            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.ReadByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.AssetGroupExpandOptions.None,
                filter
            )).Response;

            return Json(result.Total);
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetGroupsByDeleteAgentId(string deleteAgentId)
        {
            EnsureArgument.NotNullOrWhiteSpace(deleteAgentId, nameof(deleteAgentId));

            var filter = new PdmsApiModelsV2.AssetGroupFilterCriteria()
            {
                DeleteAgentId = deleteAgentId
            };
            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.AssetGroupExpandOptions.None,
                filter
            )).Response;

            return Json(result.Select(ag => ConvertToAssetGroupModel(ag)));
        }

        [HttpGet]
        public async Task<IActionResult> GetAssetGroupsByAgentId(string agentId)
        {
            EnsureArgument.NotNullOrWhiteSpace(agentId, nameof(agentId));

            var filter = new PdmsApiModelsV2.AssetGroupFilterCriteria()
            {
                DeleteAgentId = agentId,
                Or = new PdmsApiModelsV2.AssetGroupFilterCriteria()
                {
                    ExportAgentId = agentId
                }
            };
            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.AssetGroupExpandOptions.None,
                filter
            )).Response;

            return Json(result.OrderBy(ag => ag.OwnerId).Select(ag => ConvertToAssetGroupModel(ag)));
        }

        [HttpGet]
        //TODO: Move this method to VariantApiController
        public async Task<IActionResult> GetVariantById(string variantId)
        {
            EnsureArgument.NotNullOrWhiteSpace(variantId, nameof(variantId));

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.VariantDefinitions.ReadAsync(
                variantId, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            return Json(VariantConverters.ToVariantDefinitionModel(result));
        }

        [HttpGet]
        public async Task<IActionResult> GetDeleteAgentById(string id)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.ReadAsync<PdmsApiModelsV2.DeleteAgent>(
                    id,
                    await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                    PdmsApiModelsV2.DataAgentExpandOptions.HasSharingRequests
                )).Response;

            return Json(ConvertToDataAgentModel(result));
        }

        [HttpGet]
        public async Task<IActionResult> GetDataAgentsByOwnerId(string ownerId)
        {
            EnsureArgument.NotNullOrWhiteSpace(ownerId, nameof(ownerId));

            var filter = new PdmsApiModelsV2.DeleteAgentFilterCriteria()
            {
                OwnerId = ownerId,
            };

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.DataAgentExpandOptions.HasSharingRequests,
                filter
            )).Response;

            return Json(result.OfType<PdmsApiModelsV2.DeleteAgent>().Select(da => ConvertToDataAgentModel(da)));
        }

        [HttpGet]
        public async Task<IActionResult> GetDataAgentsCountByOwnerId(string ownerId)
        {
            EnsureArgument.NotNullOrWhiteSpace(ownerId, nameof(ownerId));

            var filter = new PdmsApiModelsV2.DeleteAgentFilterCriteria()
            {
                OwnerId = ownerId,
                Count = 0
            };

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.ReadByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.DataAgentExpandOptions.None,
                filter
            )).Response;

            return Json(result.Total);
        }

        [HttpGet]
        public async Task<IActionResult> GetSharedDataAgentsByOwnerId(string ownerId)
        {
            EnsureArgument.NotNullOrWhiteSpace(ownerId, nameof(ownerId));

            var filter = new PdmsApiModelsV2.DeleteAgentFilterCriteria()
            {
                OwnerId = ownerId,
                Or = new PdmsApiModelsV2.DeleteAgentFilterCriteria()
                {
                    SharingEnabled = true
                }
            };

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.DataAgentExpandOptions.None,
                filter
            )).Response;

            return Json(result.OfType<PdmsApiModelsV2.DeleteAgent>()
                .OrderBy(ShowSharedDataAgentsLast)
                .ThenBy(da => da.Name)
                .Select(da => ConvertToDataAgentModel(da)));

            int ShowSharedDataAgentsLast(PdmsApiModelsV2.DeleteAgent agent)
            {
                return agent.SharingEnabled ? 1 : 0;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSharedDataAgents()
        {
            var filter = new PdmsApiModelsV2.DeleteAgentFilterCriteria()
            {
                SharingEnabled = true,
            };

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.DataAgentExpandOptions.None,
                filter
            )).Response;

            return Json(result.OfType<PdmsApiModelsV2.DeleteAgent>().Select(da => ConvertToDataAgentModel(da)));
        }

        [HttpGet]
        public async Task<IActionResult> GetDataAssetsByAssetGroupQualifier(string assetGroupQualifierJson)
        {
            // This is necessary because the 'assetGroupQualifierJson' is a complex object. It is passed as a Json string.
            PdmsModels.AssetGroupQualifier assetGroupQualifier = JsonConvert.DeserializeObject<PdmsModels.AssetGroupQualifier>(assetGroupQualifierJson);

            var qualifier = PdmsIdentityModels.AssetQualifier.CreateFromDictionary(assetGroupQualifier.Properties);

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAssets.FindByQualifierAsync(
                qualifier,
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext()
            )).Response;

            var dataGridSearch = PdmsIdentityModels.DataGridSearch.CreateFromAssetQualifier(qualifier, "", "");

            return Json(new
            {
                DataAssets = result.Value.Select(da => ConvertToDataAssetModel(da)),
                DataGridSearch = new
                {
                    Search = dataGridSearch.DataGridSearchUri.ToString(),
                    SearchNext = dataGridSearch.DataGridNextSearchUri.ToString()
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetDataOwnerByName(string ownerName)
        {
            var filter = new PdmsApiModelsV2.DataOwnerFilterCriteria()
            {
                Name = new DataManagement.Client.Filters.StringFilter(ownerName, DataManagement.Client.Filters.StringComparisonType.Equals)
            };
            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.DataOwnerExpandOptions.None,
                filter
            )).Response;

            if (!result.Any())
            {
                return NotFound();
            }

            return Json(result.First());
        }

        [HttpGet]
        public async Task<IActionResult> GetDataOwnersBySubstring(string ownerSubstring)
        {
            var filter = new PdmsApiModelsV2.DataOwnerFilterCriteria()
            {
                Name = new DataManagement.Client.Filters.StringFilter(ownerSubstring, DataManagement.Client.Filters.StringComparisonType.Contains)
            };
            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataOwners.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.DataOwnerExpandOptions.None,
                filter
            )).Response;

            return Json(result.Select(owner => ConvertToDataOwnerModel(owner)));
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDeleteAgent([FromBody] PdmsModels.DeleteAgent deleteAgent)
        {
            var pdmsDeleteAgent = new PdmsApiModelsV2.DeleteAgent()
            {
                Name = deleteAgent.Name,
                Description = deleteAgent.Description,
                OwnerId = deleteAgent.OwnerId,
                SharingEnabled = deleteAgent.SharingEnabled,
                IsThirdPartyAgent = deleteAgent.IsThirdPartyAgent,
                HasSharingRequests = deleteAgent.HasSharingRequests,
                Icm = ConvertIcmInformation(deleteAgent, initialIcmConfig: null),
                ConnectionDetails = deleteAgent.ConnectionDetails.ToDictionary(item => item.Key, item =>
                {
                    var connectionDetail = item.Value;
                    List<Guid> aadAppIds = new List<Guid>();
                    if (connectionDetail.AadAppIds != null)
                    {
                        for (int i = 0; i < connectionDetail.AadAppIds.Count(); i++)
                        {
                            var gid = Guid.Parse(connectionDetail.AadAppIds.ElementAt(i));
                            aadAppIds.Add(gid);
                        }
                    }
                    // check if deleteAgent is a V2 agent or not
                    bool isV2Agent = connectionDetail.Protocol == "PCFV2Batch" || connectionDetail.Protocol == "CommandFeedV2";
                    if(isV2Agent)
                    {
                        return new PdmsApiModelsV2.ConnectionDetail()
                        {
                            Protocol = privacyPolicy.Get().Protocols.Set.ToDictionary(protocol => protocol.Id.Value)[connectionDetail.Protocol].Id,
                            AuthenticationType = connectionDetail.AuthenticationType,
                            MsaSiteId = null, // For a V2 agent, MsaSiteId is not supported
                            AadAppId = null, // For a V2 agent, AadAppId is not supported
                            AadAppIds = PdmsApiModelsV2.AuthenticationType.AadAppBasedAuth == connectionDetail.AuthenticationType && aadAppIds.Count() != 0 ? aadAppIds : null,
                            ReleaseState = connectionDetail.ReleaseState,
                            AgentReadiness = connectionDetail.AgentReadiness,
                        };
                    }
                    else
                    {
                        return new PdmsApiModelsV2.ConnectionDetail()
                        {
                            Protocol = privacyPolicy.Get().Protocols.Set.ToDictionary(protocol => protocol.Id.Value)[connectionDetail.Protocol].Id,
                            AuthenticationType = connectionDetail.AuthenticationType,
                            MsaSiteId = connectionDetail.MsaSiteId,
                            AadAppId = (PdmsApiModelsV2.AuthenticationType.AadAppBasedAuth == connectionDetail.AuthenticationType) ?
                                        Guid.Parse(connectionDetail.AadAppId) : (Guid?)null,
                            AadAppIds = null, // For a non V2 agent, AadAppIds is not applicable and hence should be null.
                            ReleaseState = connectionDetail.ReleaseState,
                            AgentReadiness = connectionDetail.AgentReadiness,
                        };
                    }
                }),
                DeploymentLocation = deleteAgent.DeploymentLocation,
                DataResidencyBoundary = deleteAgent.DataResidencyBoundary,
                SupportedClouds = deleteAgent.SupportedClouds
            };

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.CreateAsync(
                pdmsDeleteAgent, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(ConvertToDeleteAgentModel(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDeleteAgent([FromBody] PdmsModels.DeleteAgent deleteAgent)
        {
            var pdmsDeleteAgent =
                (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.ReadAsync<PdmsApiModelsV2.DeleteAgent>(
                    deleteAgent.Id, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            pdmsDeleteAgent.Name = deleteAgent.Name;
            pdmsDeleteAgent.Description = deleteAgent.Description;
            pdmsDeleteAgent.OwnerId = deleteAgent.OwnerId;
            pdmsDeleteAgent.SharingEnabled = deleteAgent.SharingEnabled;
            pdmsDeleteAgent.IsThirdPartyAgent = deleteAgent.IsThirdPartyAgent;
            pdmsDeleteAgent.HasSharingRequests = deleteAgent.HasSharingRequests;
            pdmsDeleteAgent.Icm = ConvertIcmInformation(deleteAgent, initialIcmConfig: pdmsDeleteAgent.Icm);
            pdmsDeleteAgent.ConnectionDetails = deleteAgent.ConnectionDetails.ToDictionary(item => item.Key, item =>
            {
                var connectionDetail = item.Value;
                List<Guid> aadAppIds = new List<Guid>();
                if (connectionDetail.AadAppIds != null)
                {
                    for (int i = 0; i < connectionDetail.AadAppIds.Count(); i++)
                    {
                        var gid = Guid.Parse(connectionDetail.AadAppIds.ElementAt(i));
                        aadAppIds.Add(gid);
                    }
                }
                // check if deleteAgent is a V2 agent or not
                bool isV2Agent = connectionDetail.Protocol == "PCFV2Batch" || connectionDetail.Protocol == "CommandFeedV2";
                if (isV2Agent)
                {
                    return new PdmsApiModelsV2.ConnectionDetail()
                    {
                        Protocol = privacyPolicy.Get().Protocols.Set.ToDictionary(protocol => protocol.Id.Value)[connectionDetail.Protocol].Id,
                        AuthenticationType = connectionDetail.AuthenticationType,
                        MsaSiteId = null, // For a V2 agent, MsaSiteId is not supported
                        AadAppId = null, // For a V2 agent, AadAppId is not supported
                        AadAppIds = PdmsApiModelsV2.AuthenticationType.AadAppBasedAuth == connectionDetail.AuthenticationType && aadAppIds.Count() != 0 ? aadAppIds : null,
                        ReleaseState = connectionDetail.ReleaseState,
                        AgentReadiness = connectionDetail.AgentReadiness,
                    };
                }
                else
                {
                    return new PdmsApiModelsV2.ConnectionDetail()
                    {
                        Protocol = privacyPolicy.Get().Protocols.Set.ToDictionary(protocol => protocol.Id.Value)[connectionDetail.Protocol].Id,
                        AuthenticationType = connectionDetail.AuthenticationType,
                        MsaSiteId = connectionDetail.MsaSiteId,
                        AadAppId = (PdmsApiModelsV2.AuthenticationType.AadAppBasedAuth == connectionDetail.AuthenticationType) ?
                                    Guid.Parse(connectionDetail.AadAppId) : (Guid?)null,
                        AadAppIds = null, // For a non V2 agent, AadAppIds is not applicable and hence should be null.
                        ReleaseState = connectionDetail.ReleaseState,
                        AgentReadiness = connectionDetail.AgentReadiness,
                    };
                }
            });
            pdmsDeleteAgent.DeploymentLocation = deleteAgent.DeploymentLocation;
            pdmsDeleteAgent.DataResidencyBoundary = deleteAgent.DataResidencyBoundary;
            pdmsDeleteAgent.SupportedClouds = deleteAgent.SupportedClouds;

            //  Set the agent operational readiness properties. This does not perform a save.
            pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.SetOperationalReadiness(
                pdmsDeleteAgent, deleteAgent.OperationalReadiness.GetPdmsOpReadinessChecklist());

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.UpdateAsync(
                pdmsDeleteAgent, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;
            return Json(ConvertToDeleteAgentModel(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task SetAgentRelationships([FromBody] PdmsModels.SetAgentRelationshipRequest setAgentRelationshipRequest)
        {
            var request = ConvertToRelationshipModelRequest(setAgentRelationshipRequest);

            await pdmsClientProviderAccessor.ProviderInstance.Instance.AssetGroups.SetAgentRelationshipsAsync(
                request, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext());
        }

        [HttpGet]
        public IActionResult GetAssetTypeMetadata()
        {
            var result = PdmsIdentityModels.Metadata.Manifest.Current
                .AssetTypes
                .Where(at => at.IsSupported)
                .Select(assetType => new
                {
                    id = assetType.Id.ToString(),
                    label = assetType.Name,
                    props = assetType
                        .Properties
                        .OrderBy(prop => prop.Level)
                        .Select((prop, idx) => new
                        {
                            id = prop.Id,
                            label = prop.Name,
                            description = prop.Description,
                            required = prop.Level <= assetType.MinimumRequiredLevel
                        })
                });

            return Json(result);
        }


        [HttpGet]
        public async Task<IActionResult> GetSharingRequestById(string id)
        {
            EnsureArgument.NotNullOrWhiteSpace(id, nameof(id));

            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.SharingRequests.ReadAsync(
                id,
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.SharingRequestExpandOptions.None
            )).Response;

            return Json(ConvertToSharingRequest(result));
        }

        [HttpGet]
        public async Task<IActionResult> GetSharingRequestsByAgentId(string agentId)
        {
            EnsureArgument.NotNullOrWhiteSpace(agentId, nameof(agentId));

            var filter = new PdmsApiModelsV2.SharingRequestFilterCriteria()
            {
                DeleteAgentId = agentId,

            };
            var result = (await pdmsClientProviderAccessor.ProviderInstance.Instance.SharingRequests.ReadAllByFiltersAsync(
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext(),
                PdmsApiModelsV2.SharingRequestExpandOptions.None,
                filter
            )).Response;

            return Json(result.Select(sr => ConvertToSharingRequest(sr)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task ApproveSharingRequests([FromBody] string[] sharingRequestIds)
        {
            foreach (string sharingRequestId in sharingRequestIds)
            {
                var pdmsSharingRequest = (await pdmsClientProviderAccessor.ProviderInstance.Instance.SharingRequests.ReadAsync(
                    sharingRequestId, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

                await pdmsClientProviderAccessor.ProviderInstance.Instance.SharingRequests.ApproveAsync(
                    sharingRequestId, pdmsSharingRequest.ETag, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext());
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task DenySharingRequests(string[] sharingRequestIds)
        {
            foreach (string sharingRequestId in sharingRequestIds)
            {
                var pdmsSharingRequest = (await pdmsClientProviderAccessor.ProviderInstance.Instance.SharingRequests.ReadAsync(
                    sharingRequestId, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

                await pdmsClientProviderAccessor.ProviderInstance.Instance.SharingRequests.DeleteAsync(
                    sharingRequestId, pdmsSharingRequest.ETag, await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIcmIncident([FromBody] PdmsModels.Incident incident)
        {
            var pdmsIncident = new PdmsApiModelsV2.Incident()
            {
                Severity = incident.Severity,
                Title = incident.Title,
                Body = incident.Body,
                Routing = new PdmsApiModelsV2.RouteData()
                {
                    AgentId = incident.Routing.AgentId,
                    OwnerId = incident.Routing.OwnerId
                },
                Keywords = incident.Keywords
            };
            var response = (await pdmsClientProviderAccessor.ProviderInstance.Instance.Incidents.CreateAsync(pdmsIncident,
                await pdmsClientProviderAccessor.ProviderInstance.CreateNewRequestContext())).Response;

            return Json(ConvertToIncidentModel(response));
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.Client, Duration = 30 * 60 /* Seconds */)]
        public Task<IEnumerable<string>> GetUserFlights()
        {
            return groundControlProviderAccessor.ProviderInstance.Instance.GetUserFlights();
        }

        public static PdmsModels.AssetGroupQualifier ConvertToAssetGroupQualifierModel(PdmsIdentityModels.AssetQualifier assetQualifier) =>
            new PdmsModels.AssetGroupQualifier(assetQualifier.Properties);

        private PdmsModels.DataAsset ConvertToDataAssetModel(PdmsApiModelsV2.DataAsset dataAsset) => new PdmsModels.DataAsset()
        {
            Id = dataAsset.Id.ToString(),
            Qualifier = ConvertToAssetGroupQualifierModel(dataAsset.Qualifier)
        };

        public static PdmsModels.AssetGroup ConvertToAssetGroupModel(PdmsApiModelsV2.AssetGroup assetGroup) => new PdmsModels.AssetGroup()
        {
            Id = assetGroup.Id.ToString(),
            DeleteAgentId = assetGroup.DeleteAgentId,
            ExportAgentId = assetGroup.ExportAgentId,
            DeleteSharingRequestId = assetGroup.DeleteSharingRequestId,
            ExportSharingRequestId = assetGroup.ExportSharingRequestId,
            OwnerId = assetGroup.OwnerId,
            HasPendingVariantRequests = assetGroup.HasPendingVariantRequests,
            IsDeleteAgentInheritanceBlocked = assetGroup.IsDeleteAgentInheritanceBlocked,
            IsExportAgentInheritanceBlocked = assetGroup.IsExportAgentInheritanceBlocked,
            IsVariantsInheritanceBlocked = assetGroup.IsVariantsInheritanceBlocked,
            Qualifier = ConvertToAssetGroupQualifierModel(assetGroup.Qualifier),
            ETag = assetGroup.ETag,
            Variants = assetGroup.Variants.Select(v => VariantConverters.ToAssetGroupVariantModel(v)),
            HasPendingTransferRequest = assetGroup.HasPendingTransferRequest,
            PendingTransferRequestTargetOwnerId = assetGroup.PendingTransferRequestTargetOwnerId,
            PendingTransferRequestTargetOwnerName = assetGroup.PendingTransferRequestTargetOwnerName,
            OptionalFeatures = assetGroup.OptionalFeatures
        };

        private PdmsModels.DataAgent ConvertToDataAgentModel(PdmsApiModelsV2.DataAgent dataAgent)
        {
            //  Our notion of Data Agent equates to Delete Agent always. 
            if (dataAgent is PdmsApiModelsV2.DeleteAgent)
            {
                return ConvertToDeleteAgentModel((PdmsApiModelsV2.DeleteAgent)dataAgent);
            }
            return null;
        }

        private ServiceTreeModels.ServiceTreeEntityDetails ConvertToServiceTree(PdmsApiModelsV2.ServiceTree serviceTreeRecord)
        {
            var result = new ServiceTreeModels.ServiceTreeEntityDetails()
            {
                ServiceAdmins = serviceTreeRecord.ServiceAdmins,
                OrganizationId = serviceTreeRecord.OrganizationId,
                DivisionId = serviceTreeRecord.DivisionId
            };

            switch (serviceTreeRecord.Level)
            {
                case PdmsApiModelsV2.ServiceTreeLevel.Service:
                    result.Id = serviceTreeRecord.ServiceId;
                    result.Kind = ServiceTreeModels.ServiceTreeEntityKind.Service;
                    break;

                case PdmsApiModelsV2.ServiceTreeLevel.TeamGroup:
                    result.Id = serviceTreeRecord.TeamGroupId;
                    result.Kind = ServiceTreeModels.ServiceTreeEntityKind.TeamGroup;
                    break;

                case PdmsApiModelsV2.ServiceTreeLevel.ServiceGroup:
                    result.Id = serviceTreeRecord.ServiceGroupId;
                    result.Kind = ServiceTreeModels.ServiceTreeEntityKind.ServiceGroup;
                    break;

                default:
                    throw new InvalidOperationException($"Service tree level {serviceTreeRecord.Level} is not supported.");
            }

            return result;
        }

        private PdmsModels.DataOwner ConvertToDataOwnerModel(PdmsApiModelsV2.DataOwner dataOwner) => new PdmsModels.DataOwner()
        {
            Id = dataOwner.Id.ToString(),
            Name = dataOwner.Name,
            Description = dataOwner.Description,
            AlertContacts = dataOwner.AlertContacts,
            AnnouncementContacts = dataOwner.AnnouncementContacts,
            SharingRequestContacts = dataOwner.SharingRequestContacts,
            AssetGroups = dataOwner.AssetGroups?.Select(sg => ConvertToAssetGroupModel(sg)),
            DataAgents = dataOwner.DataAgents?.Select(da => ConvertToDataAgentModel(da)),
            WriteSecurityGroups = dataOwner.WriteSecurityGroups,
            TagSecurityGroups = dataOwner.TagSecurityGroups,
            TagApplicationIds = dataOwner.TagApplicationIds,
            ServiceTree = dataOwner.ServiceTree != null ? ConvertToServiceTree(dataOwner.ServiceTree) : null,
            IcmConnectorId = dataOwner.Icm?.ConnectorId.ToString(),
            HasPendingTransferRequests = dataOwner.HasPendingTransferRequests
        };

        private PdmsModels.ConnectionDetail ConvertToConnectionDetailsModel(PdmsApiModelsV2.ConnectionDetail connectionDetail) =>

            new PdmsModels.ConnectionDetail()
            {
                Protocol = connectionDetail.Protocol.Value,
                AuthenticationType = connectionDetail.AuthenticationType,
                AadAppId = connectionDetail.AadAppId?.ToString() ?? string.Empty,
                AadAppIds = connectionDetail.AadAppIds.Select(x => x.ToString()).ToList(),
                MsaSiteId = connectionDetail.MsaSiteId,
                ReleaseState = connectionDetail.ReleaseState,
                AgentReadiness = connectionDetail.AgentReadiness,
            };

        private PdmsModels.DeleteAgent ConvertToDeleteAgentModel(PdmsApiModelsV2.DeleteAgent deleteAgent) => new PdmsModels.DeleteAgent()
        {
            Id = deleteAgent.Id.ToString(),
            Name = deleteAgent.Name,
            Description = deleteAgent.Description,
            OwnerId = deleteAgent.OwnerId,
            IcmConnectorId = deleteAgent.Icm?.ConnectorId.ToString(),
            SharingEnabled = deleteAgent.SharingEnabled,
            IsThirdPartyAgent = deleteAgent.IsThirdPartyAgent,
            HasSharingRequests = deleteAgent.HasSharingRequests,
            ConnectionDetails = deleteAgent.ConnectionDetails.ToDictionary(item => item.Key, item => ConvertToConnectionDetailsModel(item.Value)),
            OperationalReadiness = new PdmsModels.OperationalReadiness(
                pdmsClientProviderAccessor.ProviderInstance.Instance.DataAgents.GetOperationalReadinessBooleanArray(deleteAgent)),
            AssetGroups = deleteAgent.AssetGroups?.Select(sg => ConvertToAssetGroupModel(sg)),
            DeploymentLocation = deleteAgent.DeploymentLocation,
            DataResidencyBoundary =deleteAgent.DataResidencyBoundary,
            SupportedClouds = deleteAgent.SupportedClouds,
        };

        private ServiceTreeModels.ServiceSearchResult ConvertToServiceTreeSearchResult(ServiceTreeApiModels.Hierarchy hierarchy) =>
            new ServiceTreeModels.ServiceSearchResult()
            {
                Id = hierarchy.Id.ToString(),
                Name = hierarchy.Name,
                Kind = ConvertToServiceIdKind(hierarchy.Level)
            };

        private PdmsModels.Incident ConvertToIncidentModel(PdmsApiModelsV2.Incident incident) => new PdmsModels.Incident()
        {
            Id = incident.Id.ToString()
        };

        private ServiceTreeModels.ServiceTreeEntityDetails ConvertToServiceTreeEntityDetails(ServiceTreeApiModels.ServiceTreeNode hierarchy) =>
            new ServiceTreeModels.ServiceTreeEntityDetails()
            {
                Id = hierarchy.Id.ToString(),
                Name = hierarchy.Name,
                Kind = ConvertToServiceIdKind(hierarchy.Level),
                Description = hierarchy.Description,
                ServiceAdmins = hierarchy.AdminUserNames
            };

        private ServiceTreeModels.ServiceTreeEntityKind ConvertToServiceIdKind(ServiceTreeApiModels.ServiceTreeLevel serviceTreeLevel)
        {
            switch (serviceTreeLevel)
            {
                case ServiceTreeApiModels.ServiceTreeLevel.Service:
                    return ServiceTreeModels.ServiceTreeEntityKind.Service;

                case ServiceTreeApiModels.ServiceTreeLevel.ServiceGroup:
                    return ServiceTreeModels.ServiceTreeEntityKind.ServiceGroup;

                case ServiceTreeApiModels.ServiceTreeLevel.TeamGroup:
                    return ServiceTreeModels.ServiceTreeEntityKind.TeamGroup;

                default:
                    throw new NotSupportedException($"Service tree level {serviceTreeLevel} is not supported.");
            }
        }

        private void AddServiceTreeInformation(PdmsApiModelsV2.DataOwner pdmsDataOwner,
            string serviceTreeId, ServiceTreeModels.ServiceTreeEntityKind serviceTreeIdKind)
        {
            pdmsDataOwner.ServiceTree = new PdmsApiModelsV2.ServiceTree();

            switch (serviceTreeIdKind)
            {
                case ServiceTreeModels.ServiceTreeEntityKind.Service:
                    pdmsDataOwner.ServiceTree.ServiceId = serviceTreeId;
                    break;

                case ServiceTreeModels.ServiceTreeEntityKind.TeamGroup:
                    pdmsDataOwner.ServiceTree.TeamGroupId = serviceTreeId;
                    break;

                case ServiceTreeModels.ServiceTreeEntityKind.ServiceGroup:
                    pdmsDataOwner.ServiceTree.ServiceGroupId = serviceTreeId;
                    break;

                default:
                    throw new NotSupportedException($"Service tree ID kind {serviceTreeIdKind} is not supported.");
            }
        }

        private void AddServiceTreeInformation(PdmsApiModelsV2.DataOwner pdmsDataOwner, PdmsModels.ServiceTreeOwner serviceTreeOwner)
        {
            AddServiceTreeInformation(pdmsDataOwner, serviceTreeOwner.ServiceTreeId, serviceTreeOwner.ServiceTreeIdKind);
        }

        internal static PdmsApiModelsV2.Icm ConvertIcmInformation(PdmsModels.IEntityWithIcmInformation entityWithIcm, PdmsApiModelsV2.Icm initialIcmConfig)
        {
            if (string.IsNullOrEmpty(entityWithIcm.IcmConnectorId))
            {
                return null;
            }

            var icmConnectorIdCandidate = Guid.Parse(entityWithIcm.IcmConnectorId);

            if (null == initialIcmConfig || icmConnectorIdCandidate != initialIcmConfig.ConnectorId)
            {
                //  Override connector ID only if user changes it, to prevent breakage of auto-sync with 
                //  service tree (see Icm.Source values for more).
                return new PdmsApiModelsV2.Icm
                {
                    ConnectorId = Guid.Parse(entityWithIcm.IcmConnectorId),
                    Source = PdmsApiModelsV2.IcmSource.Manual
                };
            }

            return initialIcmConfig;
        }

        private IEnumerable<ServiceTreeApiModels.Hierarchy> FilterOnlySupportedServiceTreeLevels(IEnumerable<ServiceTreeApiModels.Hierarchy> queryResults)
        {
            return queryResults.Where(r => ServiceTreeApiModels.ServiceTreeLevel.Service == r.Level
                || ServiceTreeApiModels.ServiceTreeLevel.ServiceGroup == r.Level
                || ServiceTreeApiModels.ServiceTreeLevel.TeamGroup == r.Level);
        }

        private PdmsModels.SharingRequest ConvertToSharingRequest(PdmsApiModelsV2.SharingRequest sharingRequest) => new PdmsModels.SharingRequest()
        {
            Id = sharingRequest.Id,
            OwnerId = sharingRequest.OwnerId,
            AgentId = sharingRequest.DeleteAgentId,
            OwnerName = sharingRequest.OwnerName,
            Relationships = sharingRequest.Relationships.Select(item => ConvertToSharingRelationship(item))
        };

        private PdmsModels.SharingRelationship ConvertToSharingRelationship(PdmsApiModelsV2.SharingRelationship sharingRelationship) =>
            new PdmsModels.SharingRelationship()
            {
                AssetGroupId = sharingRelationship.AssetGroupId,
                AssetGroupQualifier = ConvertToAssetGroupQualifierModel(sharingRelationship.AssetQualifier),
                Capabilities = sharingRelationship.Capabilities
            };

        internal static PdmsModels.TrackingDetails ConvertToTrackingDetails(PdmsApiModelsV2.TrackingDetails trackingDetails) =>
            new PdmsModels.TrackingDetails()
            {
                CreatedOn = trackingDetails.CreatedOn
            };

        private PdmsApiModelsV2.SetAgentRelationshipParameters ConvertToRelationshipModelRequest(
            PdmsModels.SetAgentRelationshipRequest setAgentRelationshipRequest)
        {
            return new PdmsApiModelsV2.SetAgentRelationshipParameters
            {
                Relationships = setAgentRelationshipRequest.Relationships.Select(agentRelationship =>
                    new PdmsApiModelsV2.SetAgentRelationshipParameters.Relationship()
                    {
                        AssetGroupId = agentRelationship.AssetGroupId,
                        ETag = agentRelationship.AssetGroupETag,
                        Actions = agentRelationship.Actions.Select(action => new PdmsApiModelsV2.SetAgentRelationshipParameters.Action
                        {
                            CapabilityId = capabilitiesUxToPdmsMapping[action.Capability],
                            DeleteAgentId = action.AgentId,
                            Verb = actionTypeUxToPdmsMapping[action.Verb]
                        })
                    }
                )
            };
        }

    }
}
