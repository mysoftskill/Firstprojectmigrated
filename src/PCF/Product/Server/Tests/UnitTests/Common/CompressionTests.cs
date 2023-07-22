namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Xunit;
    using Xunit.Abstractions;

    [Trait("Category", "UnitTest")]
    public class CompressionTests
    {
        private readonly ITestOutputHelper outputHelper;

        public CompressionTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void GzipFastCompressionRoundtrip()
        {
            this.TestAlgorithm(CompressionTools.Gzip, CompressionLevel.Fastest);
            this.TestAlgorithmJson(CompressionTools.Gzip, CompressionLevel.Fastest);
        }

        [Fact]
        public void BrotliFastCompressionRoundtrip()
        {
            this.TestAlgorithm(CompressionTools.Brotli, CompressionLevel.Fastest);
            this.TestAlgorithmJson(CompressionTools.Brotli, CompressionLevel.Fastest);
        }
        
        [Fact]
        public void GzipOptimalCompressionRoundtrip()
        {
            this.TestAlgorithm(CompressionTools.Gzip, CompressionLevel.Optimal);
            this.TestAlgorithmJson(CompressionTools.Gzip, CompressionLevel.Optimal);
        }

        [Fact]
        public void BrotliOptimalCompressionRoundtrip()
        {
            this.TestAlgorithm(CompressionTools.Brotli, CompressionLevel.Optimal);
            this.TestAlgorithmJson(CompressionTools.Brotli, CompressionLevel.Optimal);
        }
        
        [Fact]
        public void GzipNoCompressionRoundtrip()
        {
            this.TestAlgorithm(CompressionTools.Gzip, CompressionLevel.NoCompression);
            this.TestAlgorithmJson(CompressionTools.Gzip, CompressionLevel.NoCompression);
        }

        [Fact]
        public void BrotliNoCompressionRoundtrip()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this.TestAlgorithm(CompressionTools.Brotli, CompressionLevel.NoCompression));
        }
        
        private void TestAlgorithm(ICompressionAlgorithm algorithm, CompressionLevel level)
        {
            string text = string.Empty;

            for (int i = 0; i < 1000; ++i)
            {
                text += Guid.NewGuid();
            }

            string compressedData = algorithm.CompressString(text, level);
            string decompressedData = algorithm.DecompressString(compressedData);

            Assert.Equal(text, decompressedData);

            this.outputHelper.WriteLine("Compressed Size = " + compressedData.Length);
            this.outputHelper.WriteLine("Original Size = " + text.Length);
        }

        private void TestAlgorithmJson(ICompressionAlgorithm algorithm, CompressionLevel level)
        {
            List<Guid> guids = new List<Guid>();

            for (int i = 0; i < 1000; ++i)
            {
                guids.Add(Guid.NewGuid());
            }

            byte[] compressedData = algorithm.CompressJson(guids, level);
            List<Guid> decompressedGuids = algorithm.DecompressJson<List<Guid>>(compressedData);

            Assert.Equal(guids, decompressedGuids);

            this.outputHelper.WriteLine("Compressed Size = " + compressedData.Length);
        }
    }
}
