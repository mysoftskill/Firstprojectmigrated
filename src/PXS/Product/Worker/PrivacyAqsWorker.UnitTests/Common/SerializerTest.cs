// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.Common
{
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SerializerTest
    {
        private const string Xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <Test xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                                      <Id>1</Id>
                                      <Name>Microsoft</Name>
                                    </Test>
                                           ";

        [TestMethod]
        [DataRow(Xml, 1, "Microsoft")]
        public void DeserializeXmlStringToTType(string xml, int expectedId, string expectedName)
        {
            var result = Serializer.Deserialize<Test>(xml);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedId, result.Id);
            Assert.AreEqual(expectedName, result.Name);
        }


        public class Test
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

    }
}
