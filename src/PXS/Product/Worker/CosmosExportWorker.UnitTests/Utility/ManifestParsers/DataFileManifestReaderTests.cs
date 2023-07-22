// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Utility.ManifestParsers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataFileManifestReaderTests
    {
        private readonly TemplateParseResult parseResult = new TemplateParseResult
        {
            Day = "1",
            Hour = "2",
            Month = "3",
            Year = "4",
            PatternIndex = 2
        };

        private void LogNull(string a, object[] b) {  }

        [TestMethod]
        public async Task ExtractCommandIdsReturnsACollectionOfTheNamesInTheFile()
        {
            const string File1 = "File1";
            const string File2 = "File2";

            
            MemoryStream stream = new MemoryStream();
            Mock<IFile> file = new Mock<IFile>();

            ICollection<ManifestDataFile> result;

            TestUtilities.PopulateStreamWithString($"{File1}\n{File2}\n", stream);
            file.Setup(o => o.GetDataReader()).Returns(stream);

            // test
            result = await DataFileManifestReader.GetDataFileNamesAsync(file.Object, this.parseResult, this.LogNull);

            // verify
            Assert.AreEqual(2, result.Count);
            Assert.IsFalse(result.Any(o => o.Invalid));
            Assert.AreEqual(1, result.Count(o => o.RawName.EqualsIgnoreCase(File1)));
            Assert.AreEqual(1, result.Count(o => o.RawName.EqualsIgnoreCase(File2)));
        }

        [TestMethod]
        public async Task ExtractCommandRemovesDuplicatesFromReturnedList()
        {
            const string File1 = "File1";
            const string File2 = "File2";

            MemoryStream stream = new MemoryStream();
            Mock<IFile> file = new Mock<IFile>();

            ICollection<ManifestDataFile> result;

            TestUtilities.PopulateStreamWithString($"{File1}\n{File1}\n{File2}\n", stream);
            file.Setup(o => o.GetDataReader()).Returns(stream);

            // test
            result = await DataFileManifestReader.GetDataFileNamesAsync(file.Object, this.parseResult, this.LogNull);

            // verify
            Assert.AreEqual(2, result.Count);
            Assert.IsFalse(result.Any(o => o.Invalid));
            Assert.AreEqual(1, result.Count(o => o.RawName.EqualsIgnoreCase(File1)));
            Assert.AreEqual(1, result.Count(o => o.RawName.EqualsIgnoreCase(File2)));
            Assert.AreEqual(2, result.First(o => o.RawName.EqualsIgnoreCase(File1)).CountFound);
            Assert.AreEqual(1, result.First(o => o.RawName.EqualsIgnoreCase(File2)).CountFound);
        }


        [TestMethod]
        public async Task ExtractCommandMarksInvalidFileNamesAsInvalid()
        {
            const string File1 = "File%1";
            const string File2 = "File2";

            MemoryStream stream = new MemoryStream();
            Mock<IFile> file = new Mock<IFile>();

            ICollection<ManifestDataFile> result;

            TestUtilities.PopulateStreamWithString($"{File1}\n{File2}\n", stream);
            file.Setup(o => o.GetDataReader()).Returns(stream);

            // test
            result = await DataFileManifestReader.GetDataFileNamesAsync(file.Object, this.parseResult, this.LogNull);

            // verify
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result.Count(o => o.Invalid));
            Assert.AreEqual(1, result.Count(o => o.RawName.EqualsIgnoreCase(File1)));
            Assert.AreEqual(1, result.Count(o => o.RawName.EqualsIgnoreCase(File2)));
            Assert.IsTrue(result.First(o => o.RawName.EqualsIgnoreCase(File1)).Invalid);
            Assert.IsFalse(result.First(o => o.RawName.EqualsIgnoreCase(File2)).Invalid);
        }
    }
}
