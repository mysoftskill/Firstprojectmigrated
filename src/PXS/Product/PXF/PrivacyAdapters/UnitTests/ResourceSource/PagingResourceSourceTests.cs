// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PagingResourceSourceTests
    {
        [TestMethod]
        public async Task ConsumeIncreasingPeekTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    for (int i = 1; i <= itemCount; i++)
                    {
                        await source.ConsumeAsync(1).ConfigureAwait(false);
                        IList<string> results = await source.PeekAsync(itemCount).ConfigureAwait(false);
                        Assert.IsNotNull(results);
                        Assert.AreEqual(itemCount - i, results.Count);
                        for (int j = 0; j < results.Count; j++)
                        {
                            Assert.AreEqual((9 - j) - i, int.Parse(results[j]));
                        }
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Continuation2Test()
        {
            var tokens = new List<string>();
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    if (itemCount < 2)
                        return;

                    await source.ConsumeAsync(2).ConfigureAwait(false);
                    tokens.Add(source.GetNextToken()?.Serialize());
                }).ConfigureAwait(false);

            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    if (itemCount < 2)
                        return;

                    if (tokens[0] == null)
                    {
                        Assert.AreEqual(2, itemCount);
                        tokens.RemoveAt(0);
                        return;
                    }

                    source.SetNextToken(tokens[0]);
                    tokens.RemoveAt(0);

                    IList<string> results = await source.PeekAsync(1).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    if (itemCount <= 2)
                        Assert.AreEqual(0, results.Count);
                    else
                    {
                        Assert.AreEqual(1, results.Count);
                        Assert.AreEqual("7", results[0]);
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ContinuationCompleteTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    await source.ConsumeAsync(int.MaxValue).ConfigureAwait(false);
                    Assert.IsNull(source.GetNextToken());
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ContinuationLastItemTest()
        {
            var tokens = new List<Tuple<string, string>>();
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    if (itemCount <= 1)
                        return;

                    await source.ConsumeAsync(itemCount - 1).ConfigureAwait(false);
                    tokens.Add(Tuple.Create((await source.PeekAsync(1).ConfigureAwait(false)).Single(), source.GetNextToken()?.Serialize()));
                }).ConfigureAwait(false);

            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    if (itemCount <= 1)
                        return;

                    source.SetNextToken(tokens[0].Item2);

                    IList<string> results = await source.PeekAsync(1).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    Assert.AreEqual(1, results.Count);
                    Assert.AreEqual(tokens[0].Item1, results[0]);
                    tokens.RemoveAt(0);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ContinuationTest()
        {
            var token = ResourceSourceContinuationToken.Deserialize(
                "H4sIAAAAAAAEADVMPQuDMBD9L0dxEhStpwlIsXTo2KHtHurRBmISkusg4n9vpPiWx_tc4MCzJ5AwQQ4R5AK3cfB68P4R1TsF9mtMnkz_dPpFVx3ZhXnr7UOfhjbxh9lHWRTaMgWrzMmFkcJ57i-K6a4nyiKrwJvqsWoR22PXlTsysuM_qrHCRgiBbSNQlFWd_h3Icl3XH_euiTyuAAAA");
            var tokens = new List<string>();
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    if (itemCount < 1)
                        return;

                    await source.ConsumeAsync(1).ConfigureAwait(false);
                    tokens.Add(source.GetNextToken()?.Serialize());
                }).ConfigureAwait(false);

            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    if (itemCount < 1)
                        return;

                    if (tokens[0] == null)
                    {
                        Assert.AreEqual(1, itemCount);
                        tokens.RemoveAt(0);
                        return;
                    }

                    source.SetNextToken(tokens[0]);
                    tokens.RemoveAt(0);

                    IList<string> results = await source.PeekAsync(1).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    if (itemCount <= 1)
                        Assert.AreEqual(0, results.Count);
                    else
                    {
                        Assert.AreEqual(1, results.Count);
                        Assert.AreEqual("8", results[0]);
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PeekConsumePeekTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    IList<string> results1 = await source.PeekAsync(itemCount).ConfigureAwait(false);
                    Assert.IsNotNull(results1);
                    Assert.AreEqual(itemCount, results1.Count);
                    for (int i = 0; i < results1.Count; i++)
                    {
                        Assert.AreEqual(9 - i, int.Parse(results1[i]));
                    }

                    await source.ConsumeAsync(1).ConfigureAwait(false);

                    IList<string> results2 = await source.PeekAsync(itemCount).ConfigureAwait(false);
                    Assert.IsNotNull(results2);
                    Assert.AreEqual(Math.Max(itemCount - 1, 0), results2.Count);
                    for (int i = 0; i < results2.Count; i++)
                    {
                        Assert.AreEqual(8 - i, int.Parse(results2[i]));
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PeekIncreasingTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    for (int i = 0; i < itemCount; i++)
                    {
                        IList<string> results = await source.PeekAsync(i).ConfigureAwait(false);
                        Assert.IsNotNull(results);
                        Assert.AreEqual(i, results.Count);
                        for (int j = 0; j < results.Count; j++)
                        {
                            Assert.AreEqual(9 - j, int.Parse(results[j]));
                        }
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PeekMaxConsumeMaxPeekTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    IList<string> results1 = await source.PeekAsync(int.MaxValue).ConfigureAwait(false);
                    Assert.IsNotNull(results1);
                    Assert.AreEqual(itemCount, results1.Count);
                    for (int i = 0; i < results1.Count; i++)
                    {
                        Assert.AreEqual(9 - i, int.Parse(results1[i]));
                    }

                    await source.ConsumeAsync(int.MaxValue).ConfigureAwait(false);

                    IList<string> results2 = await source.PeekAsync(itemCount).ConfigureAwait(false);
                    Assert.IsNotNull(results2);
                    Assert.AreEqual(0, results2.Count);
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PeekMaxIntTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    IList<string> results = await source.PeekAsync(int.MaxValue).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    Assert.AreEqual(itemCount, results.Count);
                    for (int i = 0; i < results.Count; i++)
                    {
                        Assert.AreEqual(9 - i, int.Parse(results[i]));
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PeekOverTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    IList<string> results = await source.PeekAsync(itemCount + 1).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    Assert.AreEqual(itemCount, results.Count);
                    for (int i = 0; i < results.Count; i++)
                    {
                        Assert.AreEqual(9 - i, int.Parse(results[i]));
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PeekSingleTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    IList<string> results = await source.PeekAsync(1).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    Assert.AreEqual(Math.Min(1, itemCount), results.Count);
                    for (int i = 0; i < results.Count; i++)
                    {
                        Assert.AreEqual(9 - i, int.Parse(results[i]));
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PeekTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    IList<string> results = await source.PeekAsync(itemCount).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    Assert.AreEqual(itemCount, results.Count);
                    for (int i = 0; i < results.Count; i++)
                    {
                        Assert.AreEqual(9 - i, int.Parse(results[i]));
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PeekTwiceTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    IList<string> results = await source.PeekAsync(itemCount).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    Assert.AreEqual(itemCount, results.Count);
                    for (int i = 0; i < results.Count; i++)
                    {
                        Assert.AreEqual(9 - i, int.Parse(results[i]));
                    }

                    results = await source.PeekAsync(itemCount).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    Assert.AreEqual(itemCount, results.Count);
                    for (int i = 0; i < results.Count; i++)
                    {
                        Assert.AreEqual(9 - i, int.Parse(results[i]));
                    }
                }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task PeekUnderTest()
        {
            await this.TestSourceVariationsAsync(
                async (source, itemCount) =>
                {
                    if (itemCount < 1)
                        return;
                    IList<string> results = await source.PeekAsync(itemCount - 1).ConfigureAwait(false);
                    Assert.IsNotNull(results);
                    Assert.AreEqual(itemCount - 1, results.Count);
                    for (int i = 0; i < results.Count; i++)
                    {
                        Assert.AreEqual(9 - i, int.Parse(results[i]));
                    }
                }).ConfigureAwait(false);
        }

        private async Task TestSourceVariationsAsync(Func<PagingResourceSource<string>, int, Task> action)
        {
            // Lots of paging configurations
            // Every test has to work with all of them
            var sources = new[]
            {
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(null), 0),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new string[][] { null }), 0),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new string[] { } }), 0),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new string[] { null } }), 0),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new string[] { null, null } }), 0),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new string[] { null } }), 0),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9" } }), 1),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { null, "9" } }), 1),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9", null } }), 1),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { null, "9", null } }), 1),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { null, new[] { "9" } }), 1),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9" }, null }), 1),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { null, new[] { "9" }, null }), 1),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9", "8" } }), 2),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9" }, new[] { "8" } }), 2),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9" }, new[] { "8" }, new[] { "7" } }), 3),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9", "8" }, new[] { "7" }, new[] { "6" } }), 4),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9" }, new string[] { }, new[] { "8", "7", "6", "5" } }), 5),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9" }, null, new[] { "8", "7", "6", "5" } }), 5),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9", "8", "7", "6", "5" } }), 5),
                Tuple.Create(ResourceSourceFactory<string>.GeneratePages(new[] { new[] { "9" }, new[] { "8" }, new[] { "7" }, new[] { "6" }, new[] { "5" } }), 5)
            };

            for (int i = 0; i < sources.Length; i++)
            {
                Trace.WriteLine($"Source #{i}...");
                await action(sources[i].Item1, sources[i].Item2).ConfigureAwait(false);
            }
        }
    }
}
