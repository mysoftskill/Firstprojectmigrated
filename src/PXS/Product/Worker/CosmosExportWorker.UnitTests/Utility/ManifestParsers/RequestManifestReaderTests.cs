// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Utility
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class RequestManifestReaderTests
    {
        //////////////////////////////////////////////////////////////////////////////////////////////
        // ExtractCommandIds 

        [TestMethod]
        public async Task ExtractCommandIdsReturnsACollectionOfCommandIdsInTheFile()
        {
            const string Command1 = "command1";
            const string Command2 = "command2";

            MemoryStream stream = new MemoryStream();
            Mock<IFile> file = new Mock<IFile>();

            ICollection<string> result;
            long count;

            TestUtilities.PopulateStreamWithString($"{Command1}\n{Command2}\n", stream);
            file.Setup(o => o.GetDataReader()).Returns(stream);

            // test
            (result, count) = 
                await RequestManifestReader.ExtractCommandIdsFromManifestFileAsync(file.Object, null, 0, s => { });

            // verify
            Assert.AreEqual(2, count);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result.Count(o => Command1.Equals(o)));
            Assert.AreEqual(1, result.Count(o => Command2.Equals(o)));
        }

        [TestMethod]
        public async Task ExtractCommandIdsReturnsACollectionOfCommandIdsInTheFileAndRemovesDuplicates()
        {
            const string Command1 = "command1";
            const string Command2 = "command2";
            const string Command3 = "command3";

            MemoryStream stream = new MemoryStream();
            Mock<IFile> file = new Mock<IFile>();

            ICollection<string> result;
            long count;

            TestUtilities.PopulateStreamWithString($"{Command1}\n{Command2}\n{Command1}\n{Command3}\n{Command2}\n", stream);
            file.Setup(o => o.GetDataReader()).Returns(stream);

            // test
            (result, count) = 
                await RequestManifestReader.ExtractCommandIdsFromManifestFileAsync(file.Object, null, 0, s => { });

            // verify
            Assert.AreEqual(5, count);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(1, result.Count(o => Command1.Equals(o)));
            Assert.AreEqual(1, result.Count(o => Command2.Equals(o)));
            Assert.AreEqual(1, result.Count(o => Command3.Equals(o)));
        }

        [TestMethod]
        public async Task ExtractCommandIdsCallsRefreshMethodEveryCountRows()
        {
            const string Command1 = "command1";
            const string Command2 = "command2";
            const string Command3 = "command3";

            MemoryStream stream = new MemoryStream();
            Mock<IFile> file = new Mock<IFile>();
            int countRefresh = 0;

            Task Refresher()
            {
                ++countRefresh;
                return Task.CompletedTask;
            }

            TestUtilities.PopulateStreamWithString($"{Command1}\n{Command2}\n{Command1}\n{Command3}\n{Command2}\n", stream);
            file.Setup(o => o.GetDataReader()).Returns(stream);

            // test
            await RequestManifestReader
                .ExtractCommandIdsFromManifestFileAsync(file.Object, Refresher, 2, s => { })
                .ConfigureAwait(false);

            // verify
            Assert.AreEqual(2, countRefresh);
        }
    }
}
