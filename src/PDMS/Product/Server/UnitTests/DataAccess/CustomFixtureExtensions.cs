namespace Microsoft.PrivacyServices.DataManagement.DataAccess.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AutoMapper;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Ploeh.AutoFixture;

    public static class CustomFixtureExtensions
    {
        public static IMapper FreezeMapper(this IFixture fixture)
        {
            var mapConfig = new MapperConfiguration(cfg => 
                {
                    cfg.AddProfile(new MappingProfile());
                });

            var mapper = new Mapper(mapConfig);
            fixture.Inject<IMapper>(mapper);

            return mapper;
        }

        public static void RegisterAuthorizationClasses(
                this IFixture fixture,
                IEnumerable<Guid> userSecurityGroups,
                string applicationId = null,
                bool treatAsAdmin = false,
                bool treatAsVariantEditor = false,
                bool treatAsIncidentManager = false,
                bool treatAsVariantEditorApplication = false)
        {
            var adminGroups = treatAsAdmin ? userSecurityGroups : fixture.CreateMany<Guid>();
            var variantGroups = treatAsVariantEditor ? userSecurityGroups : fixture.CreateMany<Guid>();
            var incidentGroups = treatAsIncidentManager ? userSecurityGroups : fixture.CreateMany<Guid>();
            var variantEditorApplicationId = treatAsVariantEditorApplication ? applicationId : fixture.Create<Guid>().ToString();

            fixture.Customize<Mock<ICoreConfiguration>>(entity =>
                entity
                .Do(x => x.SetupGet(m => m.MaxPageSize).Returns(5))
                .Do(x => x.SetupGet(m => m.ServiceAdminSecurityGroups).Returns(adminGroups.Select(g => g.ToString()).ToList()))
                .Do(x => x.SetupGet(m => m.VariantEditorSecurityGroups).Returns(variantGroups.Select(g => g.ToString()).ToList()))
                .Do(x => x.SetupGet(m => m.IncidentManagerSecurityGroups).Returns(incidentGroups.Select(g => g.ToString()).ToList()))
                .Do(x => x.SetupGet(m => m.VariantEditorApplicationId).Returns(variantEditorApplicationId)));

            fixture = fixture.EnableAutoMoq();
            var cachedActiveDirectory = fixture.Freeze<Mock<ICachedActiveDirectory>>();
            cachedActiveDirectory.Setup(m => m.GetSecurityGroupIdsAsync(It.IsAny<AuthenticatedPrincipal>())).ReturnsAsync(userSecurityGroups);

            var activeDirectory = fixture.Freeze<Mock<IActiveDirectory>>();
            activeDirectory.Setup(m => m.SecurityGroupIdExistsAsync(It.IsAny<AuthenticatedPrincipal>(), It.IsAny<Guid>())).ReturnsAsync(true);
            activeDirectory.Setup(m => m.GetSecurityGroupIdsAsync(It.IsAny<AuthenticatedPrincipal>())).ReturnsAsync(userSecurityGroups);

            fixture.Register<IAuthorizationProvider>(fixture.Create<AuthorizationProvider>);
        }
    }
}