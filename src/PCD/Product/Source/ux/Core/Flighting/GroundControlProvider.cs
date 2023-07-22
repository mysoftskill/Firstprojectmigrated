using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Flighting
{
    public class GroundControlProvider : IGroundControlProvider
    {
        public GroundControlProvider(IGroundControl instance)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public IGroundControl Instance
        {
            get;
        }
    }
}
