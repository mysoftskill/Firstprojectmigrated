using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Windows.Services.CompassService.Client;
using Moq;
using CmsModel = Microsoft.PrivacyServices.UX.Core.Cms.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms
{
    public class MockCmsClientProvider : ICmsClientProvider
    {
        private readonly Mock<ICmsClient> instance;
        private readonly IHttpContextAccessor httpContextAccessor;

        public MockCmsClientProvider(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            instance = new Mock<ICmsClient>(MockBehavior.Strict);

            // Mocking content request Page, Component types
            // This is short term solution to make i9n tests happy on build machine
            // Long term solution would including i9n tests use of real CMS content and perform basic validation.
            CreateMocksForPages();
            CreateMocksForComponents();

            Instance = instance.Object;
        }

        private void CreateMocksForPages()
        {
            instance.Setup(i => i.GetContentItemAsync<CmsModel.Page>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null)
            ).Returns(
                Task.FromResult(GetMockedPageContent())
            );
        }

        private void CreateMocksForComponents()
        {
            instance.Setup(i => i.GetContentItemAsync<CmsModel.Component>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null)
            ).Returns(
                Task.FromResult(GetMockedPageComponent())
            );
        }

        private CmsModel.Page GetMockedPageContent()
        {
            var pageContentMock = new Mock<CmsModel.Page>();
            pageContentMock.SetupGet(p => p.Strings).Returns(new Dictionary<string, string>());
            pageContentMock.SetupGet(p => p.Breadcrumbs).Returns(new CmsModel.Breadcrumbs { });

            return pageContentMock.Object;
        }

        private CmsModel.Component GetMockedPageComponent()
        {
            var componentContentMock = new Mock<CmsModel.Component>();
            componentContentMock.SetupGet(p => p.Strings).Returns(new Dictionary<string, string>());

            return componentContentMock.Object;
        }

        #region ICmsClientProvider Members
        public ICmsClient Instance
        {
            get;
        }
        #endregion
    }
}
