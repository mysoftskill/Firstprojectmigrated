// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.FileSystem.Cosmos
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Moq.Language;


    [TestClass]
    public class CosmosChunkedReadStreamTests
    {
        private class CosmosChunkedReadStreamTestException : Exception { }

        private const int ChunkSize = 10;

        private readonly Mock<ICosmosClient> mockClient = new Mock<ICosmosClient>();

        private readonly CosmosStreamInfo info = new CosmosStreamInfo
        {
            StreamName = "STREAM",
        };

        private CosmosChunkedReadStream testObj;

        private void SetupReadStreamsAndCreateClient(
            int? lengthOverride,
            int? maxRetries,
            params Stream[] streams)
        {
            if (streams.Length > 0)
            {
                ISetupSequentialResult<Task<DataInfo>> setup = this.mockClient
                    .SetupSequence(
                        o => o.ReadStreamAsync(
                            It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()));

                setup = streams.Aggregate(setup, (current, s) => {
                                byte[] allDataInStream = new byte[s.Length];
                                s.Read(allDataInStream, 0, (int)s.Length);
                                DataInfo info = new DataInfo(allDataInStream, allDataInStream.Length);
                                return current.ReturnsAsync(info);
                        });

                setup.Throws(new CosmosChunkedReadStreamTestException());

                this.info.Length = lengthOverride ?? streams.Sum(o => o.Length);
            }

            this.mockClient
                .Setup(o => o.GetStreamInfoAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.info);

            this.testObj = new CosmosChunkedReadStream(
                this.info,
                this.mockClient.Object,
                CosmosChunkedReadStreamTests.ChunkSize,
                TimeSpan.Zero,
                TimeSpan.Zero,
                maxRetries);
        }

        private void SetupReadStreamsAndCreateClient(params Stream[] streams)
        {
            this.SetupReadStreamsAndCreateClient(null, null, streams);
        }

        [TestMethod]
        public async Task ReadCorrectlyFetchesRequestedSizeIfRequestedSizeExceedsChunkSize()
        {
            byte[] dataResult = new byte[20];
            int result;

            this.SetupReadStreamsAndCreateClient(
                new MemoryStream(Encoding.UTF8.GetBytes("0123456789abcde")),
                new MemoryStream(Encoding.UTF8.GetBytes("fghij")),
                new MemoryStream());

            // test
            result = await this.testObj.ReadAsync(dataResult, 0, 15, CancellationToken.None).ConfigureAwait(false);

            // verify
            Assert.AreEqual(15, result);
            Assert.AreEqual("0123456789abcde", Encoding.UTF8.GetString(dataResult, 0, result));

            this.mockClient.Verify(
                o => o.ReadStreamAsync(
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()),
                Times.Exactly(1));

            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 0, 15, false), Times.Once);
        }

        [TestMethod]
        public async Task ReadCorrectlyFetchesRequestedSizeIfRequestedSizeEqualsEachChunkSize()
        {
            byte[] dataResult0 = new byte[20];
            byte[] dataResult1 = new byte[10];
            byte[] dataResult2 = new byte[10];
            int result0;
            int result1;
            int result2;

            this.SetupReadStreamsAndCreateClient(
                new MemoryStream(Encoding.UTF8.GetBytes("0123456789")),
                new MemoryStream(Encoding.UTF8.GetBytes("abcde")),
                new MemoryStream(Encoding.UTF8.GetBytes("fghijkl")),
                new MemoryStream());

            // test
            result0 = await this.testObj.ReadAsync(dataResult0, 0, 10, CancellationToken.None).ConfigureAwait(false);
            result1 = await this.testObj.ReadAsync(dataResult1, 0, 5, CancellationToken.None).ConfigureAwait(false);
            result2 = await this.testObj.ReadAsync(dataResult2, 0, 7, CancellationToken.None).ConfigureAwait(false);

            // verify
            Assert.AreEqual(10, result0);
            Assert.AreEqual(5, result1);
            Assert.AreEqual(7, result2);
            Assert.AreEqual("0123456789", Encoding.UTF8.GetString(dataResult0, 0, result0));
            Assert.AreEqual("abcde", Encoding.UTF8.GetString(dataResult1, 0, result1));
            Assert.AreEqual("fghijkl", Encoding.UTF8.GetString(dataResult2, 0, result2));

            this.mockClient.Verify(
                o => o.ReadStreamAsync(
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()),
                Times.Exactly(3));

            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 0, 10, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 10, 10, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 15, 10, false), Times.Once);
        }

        [TestMethod]
        public async Task ReadCorrectlyMergesMultipleChunksIntoASingleResult()
        {
            byte[] dataResult1 = new byte[20];
            byte[] dataResult2 = new byte[20];
            int result1;
            int result2;

            this.SetupReadStreamsAndCreateClient(
                new MemoryStream(Encoding.UTF8.GetBytes("0123456789")),
                new MemoryStream(Encoding.UTF8.GetBytes("abcde")),
                new MemoryStream());

            // test
            result1 = await this.testObj.ReadAsync(dataResult1, 0, 5, CancellationToken.None).ConfigureAwait(false);
            result2 = await this.testObj.ReadAsync(dataResult2, 0, 10, CancellationToken.None).ConfigureAwait(false);

            // verify
            Assert.AreEqual(5, result1);
            Assert.AreEqual(10, result2);
            Assert.AreEqual("01234", Encoding.UTF8.GetString(dataResult1, 0, result1));
            Assert.AreEqual("56789abcde", Encoding.UTF8.GetString(dataResult2, 0, result2));

            this.mockClient.Verify(
                o => o.ReadStreamAsync(
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()),
                Times.Exactly(2));

            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 0, 10, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 10, 10, false), Times.Once);
        }

        [TestMethod]
        public async Task ReadCorrectlyFetchesDataIfRequestedSizeExceedsTotalSizeAndChunkSize()
        {
            byte[] dataResult = new byte[30];
            int result;

            this.SetupReadStreamsAndCreateClient(
                new MemoryStream(Encoding.UTF8.GetBytes("0123456789abcde")),
                new MemoryStream());

            // test
            result = await this.testObj.ReadAsync(dataResult, 0, 30, CancellationToken.None).ConfigureAwait(false);

            // verify
            Assert.AreEqual(15, result);
            Assert.AreEqual("0123456789abcde", Encoding.UTF8.GetString(dataResult, 0, result));

            this.mockClient.Verify(
                o => o.ReadStreamAsync(
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()),
                Times.Exactly(2));

            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 0, 30, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 15, 15, false), Times.Once);
        }

        [TestMethod]
        public async Task ReadCorrectlyFetchesDataIfRequestedSizeExceedsTotalSizeButNotChunkSize()
        {
            byte[] dataResult = new byte[30];
            int result;

            this.SetupReadStreamsAndCreateClient(
                new MemoryStream(Encoding.UTF8.GetBytes("01234")),
                new MemoryStream());

            // test
            result = await this.testObj.ReadAsync(dataResult, 0, 9, CancellationToken.None).ConfigureAwait(false);

            // verify
            Assert.AreEqual(5, result);
            Assert.AreEqual("01234", Encoding.UTF8.GetString(dataResult, 0, result));

            this.mockClient.Verify(
                o => o.ReadStreamAsync(
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()),
                Times.Exactly(2));

            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 0, 10, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 5, 10, false), Times.Once);
        }

        [TestMethod]
        public async Task ReadReturnsNoDataIfHitEndOfStreamAndAttemptToReadAgain()
        {
            byte[] dataResult = new byte[30];
            int result;

            this.SetupReadStreamsAndCreateClient(
                new MemoryStream(Encoding.UTF8.GetBytes("01234")),
                new MemoryStream());

            await this.testObj.ReadAsync(dataResult, 0, 9, CancellationToken.None).ConfigureAwait(false);

            this.mockClient.Invocations.Clear();

            // test
            result = await this.testObj.ReadAsync(dataResult, 0, 1, CancellationToken.None).ConfigureAwait(false);

            // verify
            Assert.AreEqual(0, result);

            this.mockClient.Verify(
                o => o.ReadStreamAsync(
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ReadCorrectlyMergesMultipleChunksIntoASingleResultWhenMaxBufferSizeIsReached()
        {
            byte[] dataResult1 = new byte[25];
            byte[] dataResult2 = new byte[25];
            byte[] dataResult3 = new byte[40];
            int result1;
            int result2;
            int result3;

            this.SetupReadStreamsAndCreateClient(
                new MemoryStream(Encoding.UTF8.GetBytes("0123456789")),
                new MemoryStream(Encoding.UTF8.GetBytes("abcdefghij")),
                new MemoryStream(Encoding.UTF8.GetBytes("9876543210")),
                new MemoryStream(Encoding.UTF8.GetBytes("jihgfedcba")),
                new MemoryStream(Encoding.UTF8.GetBytes("0918273645")),
                new MemoryStream(Encoding.UTF8.GetBytes("ajbichdgef")),
                new MemoryStream(Encoding.UTF8.GetBytes("9081726354")),
                new MemoryStream(Encoding.UTF8.GetBytes("jaibhcgdfe")),
                new MemoryStream());

            // test
            result1 = await this.testObj.ReadAsync(dataResult1, 0, 25, CancellationToken.None).ConfigureAwait(false);
            result2 = await this.testObj.ReadAsync(dataResult2, 0, 25, CancellationToken.None).ConfigureAwait(false);
            result3 = await this.testObj.ReadAsync(dataResult3, 0, 40, CancellationToken.None).ConfigureAwait(false);
            this.testObj.Close();

            // verify
            Assert.AreEqual(25, result1);
            Assert.AreEqual(25, result2);
            Assert.AreEqual(30, result3);
            Assert.AreEqual("0123456789abcdefghij98765", Encoding.UTF8.GetString(dataResult1, 0, result1));
            Assert.AreEqual("43210jihgfedcba0918273645", Encoding.UTF8.GetString(dataResult2, 0, result2));
            Assert.AreEqual("ajbichdgef9081726354jaibhcgdfe", Encoding.UTF8.GetString(dataResult3, 0, result3));

            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 0, 25, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 10, 15, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 20, 10, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 30, 20, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 40, 10, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 50, 40, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 60, 30, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 70, 20, false), Times.Once);
            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 80, 10, false), Times.Once);

            this.mockClient.Verify(
                o => o.ReadStreamAsync(
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()),
                Times.Exactly(9));
        }

        [TestMethod]
        public async Task ReadCorrectlyReturnsEmptyWhenCosmosClientReturnsEmptyOnTheFirstReadAndFileSizeIsEmpty()
        {
            byte[] dataResult1 = new byte[25];
            int result1;

            this.SetupReadStreamsAndCreateClient(new MemoryStream());

            // test
            result1 = await this.testObj.ReadAsync(dataResult1, 0, 25, CancellationToken.None).ConfigureAwait(false);
            this.testObj.Close();

            // verify
            Assert.AreEqual(0, result1);

            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 0, 25, false), Times.Once);

            this.mockClient.Verify(
                o => o.ReadStreamAsync(
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()), 
                Times.Once);
        }
        
        [TestMethod]
        public async Task ReadReattemptsCallToCosmosWhenStreamReturnEmptyButTotalReadIsLessThanExpected()
        {
            byte[] dataResult1 = new byte[25];
            int result1;

            this.SetupReadStreamsAndCreateClient(
                10, 
                3,
                new MemoryStream(),
                new MemoryStream(Encoding.UTF8.GetBytes("0123456789")));

            // test
            result1 = await this.testObj.ReadAsync(dataResult1, 0, 10, CancellationToken.None).ConfigureAwait(false);
            this.testObj.Close();

            // verify
            Assert.AreEqual(10, result1);

            this.mockClient.Verify(o => o.ReadStreamAsync(this.info.StreamName, 0, 10, false), Times.Exactly(2));

            this.mockClient.Verify(
                o => o.ReadStreamAsync(
                    It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()),
                Times.Exactly(2));

            this.mockClient.Verify(
                o => o.GetStreamInfoAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()),
                Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ChunkedReadException))]
        public async Task ReadThrowsWhenHitEndOfStreamAndHaveReadPastExpectedFileSize()
        {
            byte[] dataResult1 = new byte[25];

            this.SetupReadStreamsAndCreateClient(
                1,
                3,
                new MemoryStream(Encoding.UTF8.GetBytes("0123456789")),
                new MemoryStream(),
                new MemoryStream(Encoding.UTF8.GetBytes("abcdefghjk")));

            // test
            try
            {
                await this.testObj.ReadAsync(dataResult1, 0, 20, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ChunkedReadException e)
            {
                Assert.AreEqual(ChunkedReadErrorCode.ExtendedStreamLength, e.ErrorCode);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ChunkedReadException))]
        public async Task ReadThrowsWhenRetryLimitExceeded()
        {
            byte[] dataResult1 = new byte[25];

            this.SetupReadStreamsAndCreateClient(
                10,
                3,
                new MemoryStream(),
                new MemoryStream(),
                new MemoryStream(),
                new MemoryStream(),
                new MemoryStream(),
                new MemoryStream(Encoding.UTF8.GetBytes("abcdefghjk")));

            // test
            try
            {
                await this.testObj.ReadAsync(dataResult1, 0, 10, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ChunkedReadException e)
            {
                Assert.AreEqual(ChunkedReadErrorCode.EarlyStreamEnd, e.ErrorCode);
                throw;
            }
        }


        [TestMethod]
        [ExpectedException(typeof(CosmosChunkedReadStreamTestException))]
        public async Task ReadEmitsExceptionForAnyOtherExceptionType()
        {
            this.mockClient
                .Setup(
                    o => o.ReadStreamAsync(
                        It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()))
                .Throws(new CosmosChunkedReadStreamTestException());

            this.SetupReadStreamsAndCreateClient();

            // test
            await this.testObj.ReadAsync(new byte[30], 0, 10, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
