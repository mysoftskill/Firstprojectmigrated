// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace PCF.UnitTests
{
    using System;
    using System.Linq;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ExportPathTransformerTests
    {
        [Fact]
        public void TransformerTests()
        {
            var transformer = new ExportPathTransformer(i => $"ProductId_{i}");
            var agent1 = new AgentId(Guid.NewGuid());
            var agent2 = new AgentId(Guid.NewGuid());
            var assetGroup1 = new AssetGroupId(Guid.NewGuid());
            var assetGroup2 = new AssetGroupId(Guid.NewGuid());

            Assert.Equal("ProductId_10/001/foo.txt", transformer.TransformPath("10/foo.txt", agent1, assetGroup1));
            Assert.Equal("ProductId_10/002/foo/bar.txt", transformer.TransformPath("10/foo/bar.txt", agent1, assetGroup2));
            Assert.Equal("ProductId_12/001/foo/bar.txt", transformer.TransformPath("12/foo/bar.txt", agent1, assetGroup2));
            Assert.Equal("ProductId_12/002/foo.txt", transformer.TransformPath("12/foo.txt", agent2, assetGroup1));
            Assert.Equal("ProductId_12/003/foo.txt", transformer.TransformPath("12/foo.txt", agent2, assetGroup2));

            var paths = transformer.EnumeratePaths().ToList();

            Assert.Equal(5, paths.Count);

            Assert.Equal("ProductId_10/001", paths[0].Path);
            Assert.Equal("ProductId_10/002", paths[1].Path);
            Assert.Equal("ProductId_12/001", paths[2].Path);
            Assert.Equal("ProductId_12/002", paths[3].Path);
            Assert.Equal("ProductId_12/003", paths[4].Path);

            Assert.Equal(agent1, paths[0].AgentId);
            Assert.Equal(agent1, paths[1].AgentId);
            Assert.Equal(agent1, paths[2].AgentId);
            Assert.Equal(agent2, paths[3].AgentId);
            Assert.Equal(agent2, paths[4].AgentId);

            Assert.Equal(assetGroup1, paths[0].AssetGroupId);
            Assert.Equal(assetGroup2, paths[1].AssetGroupId);
            Assert.Equal(assetGroup2, paths[2].AssetGroupId);
            Assert.Equal(assetGroup1, paths[3].AssetGroupId);
            Assert.Equal(assetGroup2, paths[4].AssetGroupId);
        }
    }
}
