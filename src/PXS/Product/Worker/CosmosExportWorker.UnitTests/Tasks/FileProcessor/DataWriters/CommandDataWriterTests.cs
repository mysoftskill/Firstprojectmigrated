// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks.FileProcessor.DataWriters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage;

    using Moq;

    [TestClass]
    public class CommandDataWriterTests
    {
        private readonly Mock<IExportPipeline> mockExport = new Mock<IExportPipeline>();

        private StorageException errorResult;

        [TestInitialize]
        public void TestInit()
        {
            const BindingFlags Flags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase;

            StorageExtendedErrorInformation info = new StorageExtendedErrorInformation();
            StorageException exception = new StorageException();
            RequestResult result = new RequestResult();
            PropertyInfo prop;
            Type type;

            type = exception.GetType();
            prop = type.GetProperty("RequestInformation", Flags);
            prop.SetValue(exception, result);

            type = result.GetType();
            prop = type.GetProperty("ExtendedErrorInformation", Flags);
            prop.SetValue(result, info);

            this.errorResult = exception;

            this.SetExceptionErrorCodeAndMessage("test", "test");
        }

        private void SetExceptionErrorCodeAndMessage(
            string code,
            string message)
        {
            const BindingFlags Flags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase;

            StorageExtendedErrorInformation info = this.errorResult.RequestInformation.ExtendedErrorInformation;
            PropertyInfo prop;
            Type type;

            type = info.GetType();

            prop = type.GetProperty("ErrorCode", Flags);
            prop.SetValue(info, code);

            prop = type.GetProperty("ErrorMessage", Flags);
            prop.SetValue(info, message);
        }

        private void SetupWebExceptionExceptionErrorCodeAndMessage(WebExceptionStatus status)
        {
            this.errorResult = new StorageException("Failed to resolve", new WebException("DNS failure", status));
        }

        [TestMethod]
        public async Task WriteSendsToPipelineWhenPendingSizeMoreThanThreshold()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);
            long result;

            // test
            result = await testObj.WriteAsync(PidText, Json, 0);

            // verify
            Assert.AreEqual(0, result);
            Assert.AreEqual(Json.Length, testObj.Size);
            Assert.AreEqual(0, testObj.PendingSize);
            Assert.AreEqual(1, testObj.RowCount);

            this.mockExport.Verify(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.mockExport.Verify(o => o.ExportAsync(PidText, File, Json), Times.Once);
        }

        [TestMethod]
        public async Task WriteThrowsNonTransientExceptionAndSetsErrorStateIfNonTransientErrorOccurs()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";
            const string ErrorCode = "ContainerNotFound";
            const string ErrorMsg = "ErrorMessage";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);

            this.SetExceptionErrorCodeAndMessage(ErrorCode, ErrorMsg);

            this.mockExport
                .Setup(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromException(this.errorResult));

            // test
            try
            {
                await testObj.WriteAsync(PidText, Json, 0);
                Assert.Fail("No exception thrown");
            }
            catch (NonTransientStorageException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception thrown");
            }

            // verify
            Assert.IsTrue(testObj.Statuses.HasFlag(WriterStatuses.AbandonedNoStorage));
            Assert.IsTrue(testObj.LastErrorDetails.StartsWith("Storage.StorageExtendedErrorInformation." + ErrorCode));
            Assert.IsTrue(testObj.LastErrorDetails.EndsWith(ErrorMsg));
        }

        [TestMethod]
        public async Task WriteThrowsIoExceptionAndSetsErrorStateIfTransientErrorOccurs()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";
            const string ErrorCode = "SomethingRandomNotFound";
            const string ErrorMsg = "ErrorMessage";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);

            this.SetExceptionErrorCodeAndMessage(ErrorCode, ErrorMsg);

            this.mockExport
                .Setup(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromException(this.errorResult));

            // test
            try
            {
                await testObj.WriteAsync(PidText, Json, 0);
                Assert.Fail("No exception thrown");
            }
            catch (NonTransientStorageException)
            {
                Assert.Fail("Incorrect exception thrown");
            }
            catch (IOException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception thrown");
            }

            // verify
            Assert.IsTrue(testObj.LastErrorDetails.StartsWith("Storage.StorageExtendedErrorInformation." + ErrorCode));
            Assert.IsTrue(testObj.LastErrorDetails.EndsWith(ErrorMsg));
        }

        [TestMethod]
        public async Task WriteThrowsNonTransientExceptionAndSetsErrorStateIfWebExceptionOccursAndInAssumeNonTransientMode()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);
            testObj.TransientFailureMode = TransientFailureMode.AssumeNonTransient;

            this.SetupWebExceptionExceptionErrorCodeAndMessage(WebExceptionStatus.NameResolutionFailure);

            this.mockExport
                .Setup(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromException(this.errorResult));

            // test
            try
            {
                await testObj.WriteAsync(PidText, Json, 0);
                Assert.Fail("No exception thrown");
            }
            catch (NonTransientStorageException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception thrown");
            }

            // verify
            Assert.IsTrue(testObj.Statuses.HasFlag(WriterStatuses.AbandonedNoStorage));
            Assert.IsTrue(
                testObj.LastErrorDetails.StartsWith(
                    "WebEx.WebException." + WebExceptionStatus.NameResolutionFailure.ToString()));
        }

        [TestMethod]
        public async Task WriteThrowsIoExceptionAndSetsErrorStateIfWebExceptionErrorOccursThatIsNotSemiTransient()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);
            testObj.TransientFailureMode = TransientFailureMode.AssumeNonTransient;

            this.SetupWebExceptionExceptionErrorCodeAndMessage(WebExceptionStatus.ConnectFailure);

            this.mockExport
                .Setup(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromException(this.errorResult));

            // test
            try
            {
                await testObj.WriteAsync(PidText, Json, 0);
                Assert.Fail("No exception thrown");
            }
            catch (NonTransientStorageException)
            {
                Assert.Fail("Incorrect exception thrown");
            }
            catch (IOException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception thrown");
            }

            // verify
            Assert.IsTrue(
                testObj.LastErrorDetails.StartsWith(
                    "WebEx.WebException." + WebExceptionStatus.ConnectFailure.ToString()));
        }

        [TestMethod]
        public async Task WriteThrowsIoExceptionAndSetsErrorStateIfSemiTransientErrorOccursAndInAssumeTransientMode()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);

            this.SetupWebExceptionExceptionErrorCodeAndMessage(WebExceptionStatus.NameResolutionFailure);

            this.mockExport
                .Setup(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromException(this.errorResult));

            // test
            try
            {
                await testObj.WriteAsync(PidText, Json, 0);
                Assert.Fail("No exception thrown");
            }
            catch (NonTransientStorageException)
            {
                Assert.Fail("Incorrect exception thrown");
            }
            catch (IOException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception thrown");
            }

            // verify
            Assert.IsTrue(
                testObj.LastErrorDetails.StartsWith(
                    "WebEx.WebException." + WebExceptionStatus.NameResolutionFailure.ToString()));
        }

        [TestMethod]
        public async Task WriteDoesNotSendToPipelineWhenPendingSizeLessThanThreshold()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);
            long result;

            // test
            result = await testObj.WriteAsync(PidText, Json, Json.Length + 1);

            // verify
            Assert.AreEqual(Json.Length, result);
            Assert.AreEqual(Json.Length, testObj.Size);
            Assert.AreEqual(Json.Length, testObj.PendingSize);
            Assert.AreEqual(1, testObj.RowCount);

            this.mockExport.Verify(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task WriteSendsToPipelineWhenAccumulatedPendingSizeMoreThanThreshold()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json1 = "JSONDATA1";
            const string Json2 = "JSONDATA2";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);
            long result1;
            long result2;

            // test
            result1 = await testObj.WriteAsync(PidText, Json1, Json1.Length + 1);
            result2 = await testObj.WriteAsync(PidText, Json2, Json1.Length + 1);

            // verify
            Assert.AreEqual(Json1.Length, result1);
            Assert.AreEqual(Json1.Length * -1, result2);
            Assert.AreEqual(Json1.Length + Json2.Length, testObj.Size);
            Assert.AreEqual(0, testObj.PendingSize);
            Assert.AreEqual(2, testObj.RowCount);

            this.mockExport.Verify(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.mockExport.Verify(o => o.ExportAsync(PidText, File, Json1 + "," + Json2), Times.Once);
        }

        [TestMethod]
        public async Task WriteSendsToPipelineWhenAccumulatedPendingSizeMoreThanThresholdOverMultipleCalls()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json1 = "JSONDATA1";
            const string Json2 = "JSONDATA2";
            const string Json3 = "JSONDATA3";
            const string Json4 = "JSONDATA4";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);
            long result1;
            long result2;
            long result3;
            long result4;

            // test
            result1 = await testObj.WriteAsync(PidText, Json1, Json1.Length + 1);
            result2 = await testObj.WriteAsync(PidText, Json2, Json1.Length + 1);
            result3 = await testObj.WriteAsync(PidText, Json3, Json3.Length + 1);
            result4 = await testObj.WriteAsync(PidText, Json4, Json3.Length + 1);

            // verify
            Assert.AreEqual(Json1.Length, result1);
            Assert.AreEqual(Json1.Length * -1, result2);
            Assert.AreEqual(Json3.Length, result3);
            Assert.AreEqual(Json3.Length * -1, result4);
            Assert.AreEqual(Json1.Length + Json2.Length + Json3.Length + Json4.Length, testObj.Size);
            Assert.AreEqual(0, testObj.PendingSize);
            Assert.AreEqual(4, testObj.RowCount);

            this.mockExport.Verify(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            this.mockExport.Verify(o => o.ExportAsync(PidText, File, Json1 + "," + Json2), Times.Once);
            this.mockExport.Verify(o => o.ExportAsync(PidText, File, Json3 + "," + Json4), Times.Once);
        }

        [TestMethod]
        public async Task CloseCallsPipelineDispose()
        {
            await new CommandDataWriter("id", "file", this.mockExport.Object).CloseAsync();

            this.mockExport.Verify(o => o.Dispose(), Times.Once);
        }

        [TestMethod]
        public async Task CloseFlushesPendingDataToPipeline()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);

            await testObj.WriteAsync(PidText, Json, Json.Length + 1);

            this.mockExport.Verify(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // test
            await testObj.CloseAsync();

            // verify
            Assert.AreEqual(Json.Length, testObj.Size);
            Assert.AreEqual(0, testObj.PendingSize);
            Assert.AreEqual(1, testObj.RowCount);

            this.mockExport.Verify(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task CloseThrowsNonTransientExceptionAndSetsErrorStateIfNonTransientErrorOccurs()
        {
            const string ErrorCode = "ContainerNotFound";
            const string ErrorMsg = "ErrorMessage";
            const string File = "File";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);

            this.SetExceptionErrorCodeAndMessage(ErrorCode, ErrorMsg);

            this.mockExport.Setup(o => o.Dispose()).Throws(this.errorResult);

            // test
            try
            {
                await testObj.CloseAsync();
                Assert.Fail("No exception thrown");
            }
            catch (NonTransientStorageException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception thrown");
            }

            // verify
            Assert.IsTrue(testObj.LastErrorDetails.StartsWith("Storage.StorageExtendedErrorInformation." + ErrorCode));
            Assert.IsTrue(testObj.LastErrorDetails.EndsWith(ErrorMsg));
        }

        [TestMethod]
        public async Task CloseThrowsIoExceptionAndSetsErrorStateIfTransientErrorOccurs()
        {
            const string ErrorCode = "SomethingRandomNotFound";
            const string ErrorMsg = "ErrorMessage";
            const string File = "File";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);

            this.SetExceptionErrorCodeAndMessage(ErrorCode, ErrorMsg);

            this.mockExport.Setup(o => o.Dispose()).Throws(this.errorResult);

            // test
            try
            {
                await testObj.CloseAsync();
                Assert.Fail("No exception thrown");
            }
            catch (NonTransientStorageException)
            {
                Assert.Fail("Incorrect exception thrown");
            }
            catch (IOException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception thrown");
            }

            // verify
            Assert.IsTrue(testObj.LastErrorDetails.StartsWith("Storage.StorageExtendedErrorInformation." + ErrorCode));
            Assert.IsTrue(testObj.LastErrorDetails.EndsWith(ErrorMsg));
        }
        
        [TestMethod]
        public async Task FlushFlushesPendingDataToPipeline()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);
            long result;

            await testObj.WriteAsync(PidText, Json, Json.Length + 1);

            this.mockExport.Verify(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // test
            result = await testObj.FlushAsync();

            // verify
            Assert.AreEqual(Json.Length, result);
            Assert.AreEqual(Json.Length, testObj.Size);
            Assert.AreEqual(0, testObj.PendingSize);
            Assert.AreEqual(1, testObj.RowCount);

            this.mockExport.Verify(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task FlushThrowsNonTransientExceptionAndSetsErrorStateIfTimeoutErrorOccurs()
        {
            const string PidText = "PID";
            const string File = "File";
            const string Json = "JSONDATA";

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);

            this.mockExport
                .Setup(o => o.ExportAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromException<int>(new TimeoutException()));

            await testObj.WriteAsync(PidText, Json, Json.Length + 1);

            // test
            try
            {
                await testObj.FlushAsync();
                Assert.Fail("No exception thrown");
            }
            catch (NonTransientStorageException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception thrown");
            }

            // verify
            Assert.IsTrue(testObj.Statuses.HasFlag(WriterStatuses.AbandonedNoStorage));
            Assert.IsTrue(testObj.LastErrorDetails.StartsWith("Timeout:"));
        }
        
        [TestMethod]
        public async Task WriteCorrectlyKeepsTrackOfPerFileStats()
        {
            const int Threshold = 1000000;
            const string File = "FILE";

            const string Product1 = "02006";
            const string Product2 = "10415";
            const string Data11 = "DATA1";
            const string Data12 = "NewData2";
            const string Data21 = "2DataHere";
            const string Data22 = "TobyTheDogIsDaBest";
            const string Data23 = "BailsTheDogIsNice";
            const string Data24 = "LuluTheNiceDogWasHere";

            List<IFileDetails> result;

            CommandDataWriter testObj = new CommandDataWriter("id", File, this.mockExport.Object);

            // test
            await testObj.WriteAsync(Product1, Data11, Threshold);
            await testObj.WriteAsync(Product1, Data12, Threshold);
            await testObj.WriteAsync(Product2, Data21, Threshold);
            await testObj.WriteAsync(Product2, Data22, Threshold);
            await testObj.WriteAsync(Product2, Data23, Threshold);
            await testObj.WriteAsync(Product2, Data24, Threshold);

            // verify
            result = testObj.FileDetails.OrderBy(o => o.ProductId).ToList();

            Assert.AreEqual(2, result.Count);

            Assert.AreEqual(File, result[0].FileName);
            Assert.AreEqual(Product1, result[0].ProductId);
            Assert.AreEqual(2, result[0].RowCount);
            Assert.AreEqual(Data11.Length + Data12.Length, result[0].Size);

            Assert.AreEqual(File, result[1].FileName);
            Assert.AreEqual(Product2, result[1].ProductId);
            Assert.AreEqual(4, result[1].RowCount);
            Assert.AreEqual(Data21.Length + Data22.Length + Data23.Length + Data24.Length, result[1].Size);
        }
    }
}
