// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.PrivacyServices.KustoHelpers.UnitTests
{
    using System.Data;
    using System.Threading.Tasks;

    using Kusto.Data.Common;

    using Microsoft.Membership.PrivacyServices.KustoHelpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class KustoClientTests
    {
        private readonly Mock<ICslQueryProvider> mockInner = new Mock<ICslQueryProvider>();
        private readonly Mock<IDataReader> mockReader = new Mock<IDataReader>();

        private KustoClient testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockInner
                .Setup(o => o.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClientRequestProperties>()))
                .ReturnsAsync(this.mockReader.Object);

            this.testObj = new KustoClient(this.mockInner.Object);
        }

        [TestMethod]
        public async Task ExecuteQueryCallsInnerObjectWithNullDbAndPropsIfInputOptionsNull()
        {
            const string Query = "QUERY";

            IDataReader result;

            // test
            result = await this.testObj.ExecuteQueryAsync(Query, null);

            // valiate
            Assert.AreEqual(this.mockReader.Object, result);
            this.mockInner.Verify(o => o.ExecuteQueryAsync(null, Query, null), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteQueryCallsInnerObjectWithNonNullPropsIfInputOptionsNotNull()
        {
            const string Query = "QUERY";

            KustoQueryOptions options = new KustoQueryOptions();
            IDataReader result;

            // test
            result = await this.testObj.ExecuteQueryAsync(Query, options);

            // valiate
            Assert.AreEqual(this.mockReader.Object, result);
            this.mockInner
                .Verify(
                    o => o.ExecuteQueryAsync(null, Query, It.Is<ClientRequestProperties>(p => p != null)), 
                    Times.Once);
        }

        [TestMethod]
        public async Task ExecuteQueryCallsInnerObjectWithNonDbIfInputOptionsNotNullAndContainsNonNullDb()
        {
            const string Database = "DB";
            const string Query = "QUERY";

            KustoQueryOptions options = new KustoQueryOptions { DefaultDatabase = Database };
            IDataReader result;

            // test
            result = await this.testObj.ExecuteQueryAsync(Query, options);

            // valiate
            Assert.AreEqual(this.mockReader.Object, result);
            this.mockInner
                .Verify(
                    o => o.ExecuteQueryAsync(Database, Query, It.Is<ClientRequestProperties>(p => p != null)),
                    Times.Once);
        }
    }
}
