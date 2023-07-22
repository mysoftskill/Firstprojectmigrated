// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks.FileProcessor.DataWriters
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestClass = CosmosExport.Tasks.CosmosExportPipelineFactory.ExportPipelineWrapper;

    [TestClass]
    public class CosmosExportPipelineFactoryTests
    {
        [TestMethod]
        public void TranslateProductIdReturnsUnknownForNonIntValue()
        {
            ExportProductId result = TestClass.TranslateProductId("notAnInt");

            Assert.AreSame(ExportProductId.Unknown, result);
        }

        [TestMethod]
        public void TranslateProductIdReturnsExistingValueForKnownProductId()
        {
            ExportProductId result = TestClass.TranslateProductId(ExportProductId.Azure.Id.ToString());

            Assert.AreSame(ExportProductId.Azure, result);
        }

        [TestMethod]
        public void TranslateProductIdReturnsNewObjectForUnknownProductId()
        {
            const int TestId = int.MaxValue - 5;

            ExportProductId result = TestClass.TranslateProductId(TestId.ToString());

            Assert.IsNotNull(result);
            Assert.AreEqual(TestId, result.Id);
            Assert.AreEqual(TestClass.MissingProductName, result.Path);
        }
    }
}
