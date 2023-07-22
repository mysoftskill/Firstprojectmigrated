// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.MsaAccountClose
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.Blob;

    [TestClass]
    public class DistributedIdTests : StorageEmulatorBase
    {
        private CloudBlobContainer blobContainer;

        [TestInitialize]
        public void Initialize()
        {
            this.mockAzureStorageConfiguration.SetupGet(c => c.UseEmulator).Returns(true);
            this.StartEmulator();
            this.blobContainer = this.BlobClient.GetContainerReference("ditributedids");
        }

        [DataTestMethod, TestCategory("FCT"), Ignore]
        [DataRow(5)]
        [DataRow(10)]
        [DataRow(200)]
        public async Task ShouldGetDifferentIds(int maxIds)
        {
            await this.blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            var idCreator = new DistributedIdFactory(this.blobContainer, maxIds);
            var actualIds = new List<IDistributedId>(maxIds);
            Parallel.For(0, maxIds, async index => { actualIds[index] = await idCreator.AcquireIdAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false); });
            long expectedIndex = 0;
            foreach (IDistributedId id in actualIds.OrderBy(id => id.Id))
            {
                Assert.AreEqual(expectedIndex++, id.Id);
            }

            await Task.WhenAll(actualIds.Select(id => id.ReleaseAsync())).ConfigureAwait(false);
        }
    }
}
