// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks.FileProcessor.DataWriters
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataWriterTests
    {
        private class TestWriter : DataWriter
        {
            public TestWriter(
                string fileName, 
                string commandId) : 
                base(fileName, commandId)
            {
            }
        }
        
        [TestMethod]
        public async Task WriteCorrectlyKeepsTrackOfPerFileStats()
        {
            const int Threshold = 1000000;
            const string File = "FILE";
            const string Cmd = "COMMAND";

            const string Product1 = "02006";
            const string Product2 = "10415";
            const string Data11 = "DATA1";
            const string Data12 = "NewData2";
            const string Data21 = "2DataHere";
            const string Data22 = "TobyTheDogIsDaBest";
            const string Data23 = "BailsTheDogIsNice";
            const string Data24 = "LuluTheNiceDogWasHere";

            List<IFileDetails> result;

            TestWriter testObj = new TestWriter(File, Cmd);

            // test
            await testObj.WriteAsync(Product1, Data11, Threshold);
            await testObj.WriteAsync(Product1, Data12, Threshold);
            await testObj.WriteAsync(Product2, Data21, Threshold);
            await testObj.WriteAsync(Product2, Data22, Threshold);
            await testObj.WriteAsync(Product2, Data23, Threshold);
            await testObj.WriteAsync(Product2, Data24, Threshold);

            // verify
            result = testObj.FileDetails.OrderBy(o => o.ProductId).ToList();

            Assert.AreEqual(2, result.Count);

            Assert.AreEqual(File, result[0].FileName);
            Assert.AreEqual(Product1, result[0].ProductId);
            Assert.AreEqual(2, result[0].RowCount);
            Assert.AreEqual(Data11.Length + Data12.Length, result[0].Size);

            Assert.AreEqual(File, result[1].FileName);
            Assert.AreEqual(Product2, result[1].ProductId);
            Assert.AreEqual(4, result[1].RowCount);
            Assert.AreEqual(Data21.Length + Data22.Length + Data23.Length + Data24.Length, result[1].Size);
        }
    }
}
