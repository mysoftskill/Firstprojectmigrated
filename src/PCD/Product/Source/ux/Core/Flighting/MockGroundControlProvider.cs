using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.PrivacyServices.UX.Core.Flighting
{
    /// <summary>
    /// Mocked implementation of <see cref="IGroundControlProvider"/>.
    /// </summary>
    public class MockGroundControlProvider : IGroundControlProvider
    {
        public MockGroundControlProvider(IHttpContextAccessor httpContextAccessor)
        {
            Instance = new DynamicMockGroundControl(new MockGroundControlScenarioHelper(httpContextAccessor));
        }

        #region IGroundControlProvider Members

        public IGroundControl Instance
        {
            get;
        }

        #endregion
    }
}
