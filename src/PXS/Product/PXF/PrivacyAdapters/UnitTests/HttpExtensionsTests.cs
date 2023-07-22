// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    using System;
    using System.Linq;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpExtensionsTests
    {
        [DataTestMethod]
        [DataRow("https://www.hello.me/endpoint", "https://www.hello.me/endpoint?code=awesome", "code=awesome")]
        [DataRow("https://www.hello.me/endpoint", "https://www.hello.me/endpoint?code=awe%3dsome", "code=awe=some")]
        [DataRow("https://www.hello.me/endpoint", "https://www.hello.me/endpoint?code=awesome&food=yum", "code=awesome|food=yum")]
        [DataRow("https://www.hello.me/endpoint", "https://www.hello.me/endpoint?code=awesome&code=readable&food=yum", "code=awesome|food=yum|code=readable")]
        [DataRow("https://www.hello.me/endpoint?sleep=nice", "https://www.hello.me/endpoint?sleep=nice&code=awesome&code=readable&food=yum", "code=awesome|food=yum|code=readable")]
        [DataRow("https://www.hello.me/endpoint?sleep=nice", "https://www.hello.me/endpoint?sleep=nice", null)]
        public void AddQueryParametersTest(string uri, string expectedUri, string queryParams) => Assert.AreEqual(expectedUri, new Uri(uri).AddQueryParameters(queryParams?.Split('|')?.ToList()).ToString());

        [TestMethod]
        public void ExpandUriTest()
        {
            const string ExpectedUri = "https://tosawick/privacy/v1/my/locationhistory";
            const string RelativePath = "v1/my/locationhistory";
            var qsc = new QueryStringCollection();

            // With trailing slash in base
            var uri = "https://tosawick/privacy/".ExpandUri(RelativePath, qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy/".ExpandUri("/" + RelativePath, qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy/".ExpandUri("/" + RelativePath + "/", qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy/".ExpandUri("    /" + RelativePath + "/", qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy/".ExpandUri("/" + RelativePath + "/    ", qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);

            // Without trailing slash in base
            uri = "https://tosawick/privacy".ExpandUri(RelativePath, qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy     ".ExpandUri(RelativePath, qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy\t".ExpandUri(RelativePath, qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy".ExpandUri("/" + RelativePath, qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy".ExpandUri("/" + RelativePath + "/", qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy".ExpandUri("/" + RelativePath + "/    ", qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);
            uri = "https://tosawick/privacy".ExpandUri("/" + RelativePath + "/\t", qsc);
            Assert.AreEqual(ExpectedUri, uri.AbsoluteUri);

            // One case that should fail is whitespace before a trailing slash in the baseUri
            uri = "https://tosawick/privacy    /".ExpandUri(RelativePath, qsc);
            Assert.AreNotEqual(ExpectedUri, uri.AbsoluteUri, "We expect this to give a weird result");
        }
    }
}
