// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.Tables
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Cosmos.Table;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class AzureTableTests
    {
        private const string Name = "tobydog";

        private readonly Mock<IAzureStorageProvider> mockProv = new Mock<IAzureStorageProvider>();
        private readonly Mock<ICloudTable> mockTable = new Mock<ICloudTable>();
        private readonly Mock<ILogger> mockLog = new Mock<ILogger>();

        private class DataPlain :
            TableEntity, 
            ITableEntityInitializer
        {
            public object RawResult { get; set; }

            public void Initialize(object rawTableObject)
            {
                this.RawResult = rawTableObject;
            }
        };

        private class DataExtract :
            TableEntity,
            ITableEntityInitializer,
            ITableEntityStorageExtractor
        {
            public ITableEntity ExtractResult { get; set; }

            public object RawResult { get; set; }

            public void Initialize(object rawTableObject)
            {
                this.RawResult = rawTableObject;
            }

            public ITableEntity ExtractStorageRepresentation()
            {
                return this.ExtractResult;
            }
        };

        private AzureTable<DataExtract> testobjExtractable;
        private AzureTable<DataPlain> testobj;

        [TestInitialize]
        public void Init()
        {
            this.mockProv.Setup(o => o.GetCloudTableAsync(It.IsAny<string>())).ReturnsAsync(this.mockTable.Object);

            this.testobjExtractable = new AzureTable<DataExtract>(this.mockProv.Object, this.mockLog.Object, AzureTableTests.Name);
            this.testobj = new AzureTable<DataPlain>(this.mockProv.Object, this.mockLog.Object, AzureTableTests.Name);
        }

        [TestMethod]
        public async Task FirstMethodCallOnObjectGetTable()
        {
            // test
            await this.testobj.QueryAsync("query");

            // verify
            this.mockProv.Verify(o => o.GetCloudTableAsync(AzureTableTests.Name), Times.Once);
        }

        [TestMethod]
        public async Task GetCallsIntoUnderlyingTableAndReturnsResultsWhenResultIsOfGenericType()
        {
            const string PartKey = "part";
            const string RowKey = "row";

            DataPlain expected = new DataPlain();
            DataPlain result;

            this.mockTable
                .Setup(o => o.QuerySingleRowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { Result = expected, HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobj.GetItemAsync(PartKey, RowKey);

            // verify
            Assert.AreSame(result, expected);
            Assert.IsNull(result.RawResult);
            this.mockTable.Verify(o => o.QuerySingleRowAsync(PartKey, RowKey, true), Times.Once);
        }

        [TestMethod]
        public async Task GetCallsIntoUnderlyingTableAndReturnsResultsWhenResultIsNotOfGenericType()
        {
            const string PartKey = "part";
            const string RowKey = "row";

            DynamicTableEntity expected = new DynamicTableEntity();
            DataPlain result;

            this.mockTable
                .Setup(o => o.QuerySingleRowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { Result = expected, HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobj.GetItemAsync(PartKey, RowKey);

            // verify
            Assert.AreSame(result.RawResult, expected);
            this.mockTable.Verify(o => o.QuerySingleRowAsync(PartKey, RowKey, true), Times.Once);
        }

        [TestMethod]
        public async Task GetCallsIntoUnderlyingTableAndReturnsNullWhenResultIsNotHttpOk()
        {
            const string PartKey = "part";
            const string RowKey = "row";

            DataPlain result;

            this.mockTable
                .Setup(o => o.QuerySingleRowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.NotFound });

            // test
            result = await this.testobj.GetItemAsync(PartKey, RowKey);

            // verify
            Assert.IsNull(result);
            this.mockTable.Verify(o => o.QuerySingleRowAsync(PartKey, RowKey, true), Times.Once);
        }

        [TestMethod]
        public async Task QueryCallsIntoUnderlyingTableAndReturnsResult()
        {
            const string Query = "query";

            ICollection<DataPlain> expected = new List<DataPlain>();
            ICollection<DataPlain> result;

            this.mockTable
                .Setup(o => o.QueryAsync<DataPlain>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(expected);

            // test
            result = await this.testobj.QueryAsync(Query);

            // verify
            Assert.AreSame(result, expected);
            this.mockTable.Verify(o => o.QueryAsync<DataPlain>(Query, null, null), Times.Once);
        }

        [TestMethod]
        public async Task QueryCallsIntoUnderlyingTableAndReturnsResultWhenPassedLimitAndColumns()
        {
            const string Query = "query";
            const int Limit = 5;

            List<string> columns = new List<string>();

            ICollection<DataPlain> expected = new List<DataPlain>();
            ICollection<DataPlain> result;

            this.mockTable
                .Setup(o => o.QueryAsync<DataPlain>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(expected);

            // test
            result = await this.testobj.QueryAsync(Query, Limit, columns);

            // verify
            Assert.AreSame(result, expected);
            this.mockTable.Verify(o => o.QueryAsync<DataPlain>(Query, Limit, columns), Times.Once);
        }

        [TestMethod]
        public async Task InsertCallsIntoUnderlyingTableAndReturnsTrueWhenTableReturnsOkAndTypeIsNotExtractable()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.InsertAsync(It.IsAny<DataPlain>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobj.InsertAsync(input);

            // verify
            Assert.IsTrue(result);
            this.mockTable.Verify(o => o.InsertAsync(input, true), Times.Once);
        }

        [TestMethod]
        public async Task InsertCallsIntoUnderlyingTableAndReturnsFalseWhenTableReturnsNotOkAndTypeIsNotExtractable()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.InsertAsync(It.IsAny<DataPlain>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.Forbidden });

            // test
            result = await this.testobj.InsertAsync(input);

            // verify
            Assert.IsFalse(result);
            this.mockTable.Verify(o => o.InsertAsync(input, true), Times.Once);
        }

        [TestMethod]
        public async Task InsertCallsIntoUnderlyingTableAndReturnsTrueWhenTableReturnsOkAndTypeIsExtractable()
        {
            DataPlain inner = new DataPlain();
            DataExtract input = new DataExtract { ExtractResult = inner };
            bool result;

            this.mockTable
                .Setup(o => o.InsertAsync(It.IsAny<DataPlain>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobjExtractable.InsertAsync(input);

            // verify
            Assert.IsTrue(result);
            this.mockTable.Verify(o => o.InsertAsync(inner, true), Times.Once);
        }

        [TestMethod]
        public async Task ReplaceCallsIntoUnderlyingTableAndReturnsTrueWhenTableReturnsOkAndTypeIsNotExtractable()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.ReplaceAsync(It.IsAny<DataPlain>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobj.ReplaceAsync(input);

            // verify
            Assert.IsTrue(result);
            this.mockTable.Verify(o => o.ReplaceAsync(input, true), Times.Once);
        }

        [TestMethod]
        public async Task ReplaceCallsIntoUnderlyingTableAndReturnsFalseWhenTableReturnsNotOkAndTypeIsNotExtractable()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.ReplaceAsync(It.IsAny<DataPlain>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.Forbidden });

            // test
            result = await this.testobj.ReplaceAsync(input);

            // verify
            Assert.IsFalse(result);
            this.mockTable.Verify(o => o.ReplaceAsync(input, true), Times.Once);
        }

        [TestMethod]
        public async Task ReplaceCallsIntoUnderlyingTableAndReturnsTrueWhenTableReturnsOkAndTypeIsExtractable()
        {
            DataPlain inner = new DataPlain();
            DataExtract input = new DataExtract { ExtractResult = inner };
            bool result;

            this.mockTable
                .Setup(o => o.ReplaceAsync(It.IsAny<DataPlain>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobjExtractable.ReplaceAsync(input);

            // verify
            Assert.IsTrue(result);
            this.mockTable.Verify(o => o.ReplaceAsync(inner, true), Times.Once);
        }

        [TestMethod]
        public async Task InsertBatchCallsIntoUnderlyingTableAndReturnsTrueWhenTableReturnsOkAndTypeIsNotExtractable()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.InsertBatchAsync(It.IsAny<ICollection<ITableEntity>>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobj.InsertBatchAsync(new[] { input });

            // verify
            Assert.IsTrue(result);
            this.mockTable
                .Verify(
                    o => o.InsertBatchAsync(
                        It.Is<ICollection<ITableEntity>>(p => object.ReferenceEquals(p.First(), input)),
                        true),
                    Times.Once);
        }

        [TestMethod]
        public async Task InsertBatchCallsIntoUnderlyingTableAndReturnsFalseWhenTableReturnsNotOkAndTypeIsNotExtractable()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.InsertBatchAsync(It.IsAny<ICollection<ITableEntity>>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.Forbidden });

            // test
            result = await this.testobj.InsertBatchAsync(new[] { input });

            // verify
            Assert.IsFalse(result);
            this.mockTable
                .Verify(
                    o => o.InsertBatchAsync(
                        It.Is<ICollection<ITableEntity>>(p => object.ReferenceEquals(p.First(), input)),
                        true),
                    Times.Once);
        }

        [TestMethod]
        public async Task InsertBatchCallsIntoUnderlyingTableAndReturnsTrueWhenTableReturnsOkAndTypeIsExtractable()
        {
            DataPlain inner = new DataPlain();
            DataExtract input = new DataExtract { ExtractResult = inner };
            bool result;

            this.mockTable
                .Setup(o => o.InsertBatchAsync(It.IsAny<ICollection<ITableEntity>>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobjExtractable.InsertBatchAsync(new[] { input });

            // verify
            Assert.IsTrue(result);
            this.mockTable
                .Verify(
                    o => o.InsertBatchAsync(
                        It.Is<ICollection<ITableEntity>>(p => object.ReferenceEquals(p.First(), inner)),
                        true),
                    Times.Once);
        }

        [TestMethod]
        public async Task DeleteCallsIntoUnderlyingTableAndReturnsTrueWhenTableReturnsOk()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.DeleteAsync(It.IsAny<DataPlain>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobj.DeleteItemAsync(input);

            // verify
            Assert.IsTrue(result);
            this.mockTable.Verify(o => o.DeleteAsync(input, true), Times.Once);
        }

        [TestMethod]
        public async Task DeleteCallsIntoUnderlyingTableAndReturnsFalseWhenTableReturnsNotOk()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.DeleteAsync(It.IsAny<DataPlain>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.Forbidden });

            // test
            result = await this.testobj.DeleteItemAsync(input);

            // verify
            Assert.IsFalse(result);
            this.mockTable.Verify(o => o.DeleteAsync(input, true), Times.Once);
        }

        [TestMethod]
        public async Task DeleteBatchCallsIntoUnderlyingTableAndReturnsTrueWhenTableReturnsOk()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.DeleteBatchAsync(It.IsAny<ICollection<ITableEntity>>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.OK });

            // test
            result = await this.testobj.DeleteBatchAsync(new[] { input });

            // verify
            Assert.IsTrue(result);
            this.mockTable
                .Verify(
                    o => o.DeleteBatchAsync(
                        It.Is<ICollection<ITableEntity>>(p => object.ReferenceEquals(p.First(), input)), 
                            true), 
                    Times.Once);
        }

        [TestMethod]
        public async Task DeleteBatchCallsIntoUnderlyingTableAndReturnsFalseWhenTableReturnsNotOk()
        {
            DataPlain input = new DataPlain();
            bool result;

            this.mockTable
                .Setup(o => o.DeleteBatchAsync(It.IsAny<ICollection<ITableEntity>>(), It.IsAny<bool>()))
                .ReturnsAsync(new TableResult { HttpStatusCode = (int)HttpStatusCode.Forbidden });

            // test
            result = await this.testobj.DeleteBatchAsync(new[] { input });

            // verify
            Assert.IsFalse(result);
            this.mockTable
                .Verify(
                    o => o.DeleteBatchAsync(
                        It.Is<ICollection<ITableEntity>>(p => object.ReferenceEquals(p.First(), input)),
                        true),
                    Times.Once);
        }


    }
}
