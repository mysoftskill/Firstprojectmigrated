// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExportServiceUnitTest : StorageEmulatorBase
    {
        [TestInitialize]
        public void Init()
        {
            this.mockAzureStorageConfiguration.SetupGet(c => c.UseEmulator).Returns(true);
            this.StartEmulator();
        }


    }
}
