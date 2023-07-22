namespace PCF.UnitTests
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Xunit;
    using Xunit.Abstractions;

    [Trait("Category", "UnitTest")]
    public class Base64StreamTests
    {
        [Fact]
        public void Base64Tests()
        {
            foreach (int dataLength in new[] { 1, 2, 3, 4, 10, 50, 100, 250, 500, 1000, 10000 })
            {
                foreach (int blockSize in new[] { 1, 2, 3, 4, 10, 50, 500, 10000, 1000000 })
                {
                    this.Base64RoundTripTest(dataLength, blockSize);
                }
            }
        }

        private void Base64RoundTripTest(int dataLength, int blockCopySize)
        {
            byte[] data = new byte[dataLength];
            new Random().NextBytes(data);

            string expectedBase64 = Convert.ToBase64String(data);

            using (Base64Stream toB64Stream = new Base64Stream())
            {
                int index = 0;
                while (index < dataLength)
                {
                    toB64Stream.Write(data, index, Math.Min(data.Length - index, blockCopySize));
                    index += blockCopySize;
                }

                toB64Stream.Close();

                string encodedOutput = toB64Stream.EncodedOutput.ToString();
                Assert.Equal(expectedBase64, encodedOutput);
            }

            using (Base64Stream fromB64Stream = new Base64Stream(expectedBase64))
            using (MemoryStream destination = new MemoryStream())
            {
                byte[] buffer = new byte[blockCopySize];
                while (true)
                {
                    int bytesRead = fromB64Stream.Read(buffer, 0, blockCopySize);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    destination.Write(buffer, 0, bytesRead);
                }
                
                byte[] result = destination.ToArray();
                Assert.Equal(data, result);
            }
        }
    }
}
