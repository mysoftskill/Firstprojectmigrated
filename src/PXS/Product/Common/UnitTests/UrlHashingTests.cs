// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.UnitTests
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UrlHashingTests
    {
        private static readonly string[][] Tests =
        {
            // With no scheme or otherwise failing to parse an absolute uri, uri parsing doesn't happen
            new[] { "www.bing.com", "www.bing.com", "5f44044454ef448148dfbb951dc486368bf530c870c5059054da5f7138927c7e" }, // which means no scheme is added, no trailing slash is added
            new[] { "www.Bing.com", "www.Bing.com", "8ac451d5402b882307fce70d6475dd578514ca6b0cfb9dc5d672b18e0fb5cbac" }, // which means casing isn't normalized
            new[] { "www.bing.com:80", "www.bing.com:80", "40a97b6d2ba15b28ef7b72d4dfec93cff45353212419716f1ff2496f81dfbcd7" }, // which means default ports are not normalized
            new[] { "xn--80aafi6cg.xn--p1ai", "xn--80aafi6cg.xn--p1ai", "34baab95b671ca73c8fbada038de0c9d20bb9d0d4f343fb0670986ab5a3e290d" }, // which means IDN hosts are not resolved

            // Scheme casing
            new[] { "hTTps://www.bing.com/", "https://www.bing.com/", "53c485f5b860b0f35f45a47d699b9517ed09d4337384e631601a5e245db8c3c0" },
            new[] { "Https://www.bing.com/", "https://www.bing.com/", "53c485f5b860b0f35f45a47d699b9517ed09d4337384e631601a5e245db8c3c0" },
            new[] { "https://www.bing.com/", "https://www.bing.com/", "53c485f5b860b0f35f45a47d699b9517ed09d4337384e631601a5e245db8c3c0" },
            
            // Host casing
            new[] { "https://www.biNG.com/", "https://www.bing.com/", "53c485f5b860b0f35f45a47d699b9517ed09d4337384e631601a5e245db8c3c0" },
            new[] { "https://www.BIng.com/", "https://www.bing.com/", "53c485f5b860b0f35f45a47d699b9517ed09d4337384e631601a5e245db8c3c0" },
            new[] { "https://www.Bing.com/", "https://www.bing.com/", "53c485f5b860b0f35f45a47d699b9517ed09d4337384e631601a5e245db8c3c0" },
            
            // Url decoding
            new[] { "https://www.bing.com/foo+bar", "https://www.bing.com/foo bar", "15079071493853cb1be481e46af97f1b843119d740982f44671c3b2b1cd76942" },
            new[] { "https://www.bing.com/foo%20bar", "https://www.bing.com/foo bar", "15079071493853cb1be481e46af97f1b843119d740982f44671c3b2b1cd76942" },
            new[] { "https://www.bing.com/foo bar", "https://www.bing.com/foo bar", "15079071493853cb1be481e46af97f1b843119d740982f44671c3b2b1cd76942" },
            
            // Default ports
            new[] { "https://www.bing.com:443/", "https://www.bing.com/", "53c485f5b860b0f35f45a47d699b9517ed09d4337384e631601a5e245db8c3c0" },
            
            // Trailing slashes without paths
            new[] { "https://www.bing.com/", "https://www.bing.com/", "53c485f5b860b0f35f45a47d699b9517ed09d4337384e631601a5e245db8c3c0" },
            new[] { "https://www.bing.com", "https://www.bing.com/", "53c485f5b860b0f35f45a47d699b9517ed09d4337384e631601a5e245db8c3c0" },
            
            // IDN
            new[] { "https://правда.рф", "https://правда.рф/", "f312a1af9ddce1b52ad731e7c6a43958a83ae54eaca3b464490320797d9c1732" },
            new[] { "https://xn--80aafi6cg.xn--p1ai", "https://правда.рф/", "f312a1af9ddce1b52ad731e7c6a43958a83ae54eaca3b464490320797d9c1732" }
        };

        [TestMethod]
        public void TestUrlHashing()
        {
            foreach (var test in Tests)
            {
                Assert.AreEqual(test[1], UrlHashing.NormalizeUrl(test[0]));
                Assert.AreEqual(test[2], UrlHashing.HashUrl(test[0]));
            }
        }
    }
}