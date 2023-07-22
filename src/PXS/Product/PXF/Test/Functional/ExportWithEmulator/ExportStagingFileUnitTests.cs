// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class ExportStagingFileUnitTests : StorageEmulatorBase
    {
        private const string TestStagingFileContainerName = "teststagingfiles";

        [TestInitialize]
        public void Init()
        {
            this.mockAzureStorageConfiguration.SetupGet(c => c.UseEmulator).Returns(true);
            this.StartEmulator();
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_SimpleCreate()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            string content = "abc";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            await stagingFile.AddBlockAsync(content);
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            string readContent = await this.GetStringFromBlobAsync(containerName, fileName);
            Assert.AreEqual(content, readContent);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_MultipleDispose()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            string content = "abc";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            await stagingFile.AddBlockAsync(content);
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            stagingFile.Dispose();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            string readContent = await this.GetStringFromBlobAsync(containerName, fileName);
            Assert.AreEqual(content, readContent);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_ManyTextBlocks()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            var sb = new StringBuilder();
            for (int i = 0; i < 50000; i++)
            {
                string content = Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
                sb.Append(content);
                await stagingFile.AddBlockAsync(content);
            }
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            string readContent = await this.GetStringFromBlobAsync(containerName, fileName);
            Assert.AreEqual(sb.ToString(), readContent);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_LargeTextBlocks()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            var sb = new StringBuilder();
            for (int j = 0; j < 5; j++)
            {
                var sb2 = new StringBuilder();
                for (int i = 0; i < 15000; i++)
                {
                    string content = Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
                    sb2.Append(content);
                    sb.Append(content);
                }
                await stagingFile.AddBlockAsync(sb2.ToString());
            }
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            string readContent = await this.GetStringFromBlobAsync(containerName, fileName);
            Assert.AreEqual(sb.ToString(), readContent);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_BinaryBlocks()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            var memStream = new MemoryStream();
            for (int i = 0; i < 100; i++)
            {
                var content = Guid.NewGuid();
                await memStream.WriteAsync(content.ToByteArray(), 0, 16);
                await stagingFile.AddBlockAsync(content.ToByteArray());
            }
            memStream.Position = 0;
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            Stream readStream = await this.GetStreamFromBlobAsync(containerName, fileName);
            byte[] writtenContent = memStream.ToArray();
            Assert.IsTrue(await this.CompareStreamsOfGuids(readStream, memStream));
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_ManyBinaryBlocks()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            var memStream = new MemoryStream();
            for (int i = 0; i < 300000; i++)
            {
                var content = Guid.NewGuid();
                await memStream.WriteAsync(content.ToByteArray(), 0, 16);
                await stagingFile.AddBlockAsync(content.ToByteArray());
            }
            memStream.Position = 0;
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            Stream readStream = await this.GetStreamFromBlobAsync(containerName, fileName);
            byte[] writtenContent = memStream.ToArray();
            Assert.IsTrue(await this.CompareStreamsOfGuids(readStream, memStream));
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_LargeBinaryBlocks()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            var memStream = new MemoryStream();
            var memStream2 = new MemoryStream();
            for (int j = 0; j < 12; j++)
            {
                memStream2.Position = 0;
                for (int i = 0; i < 160000; i++)
                {
                    var content = Guid.NewGuid();
                    await memStream.WriteAsync(content.ToByteArray(), 0, 16);
                    await memStream2.WriteAsync(content.ToByteArray(), 0, 16);
                }
                var buf = new byte[2560000];
                memStream2.Position = 0;
                await memStream2.ReadAsync(buf, 0, 2560000);
                await stagingFile.AddBlockAsync(buf);
            }
            memStream.Position = 0;
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            Stream readStream = await this.GetStreamFromBlobAsync(containerName, fileName);
            byte[] writtenContent = memStream.ToArray();
            Assert.IsTrue(await this.CompareStreamsOfGuids(readStream, memStream));
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_FailWhenMixingBinaryBlocksWithTextBlock()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            string content = "abc";
            bool gotException = false;
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            try
            {
                await stagingFile.AddBlockAsync(content);
                await stagingFile.AddBlockAsync(Guid.NewGuid().ToByteArray());
                await stagingFile.CommitAsync();
            }
            catch (InvalidOperationException)
            {
                gotException = true;
            }
            stagingFile.Dispose();
            Assert.IsTrue(gotException);
        }


        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_FailWhenMixingTextBlocksWithBinaryBlock()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            string content = "abc";
            bool gotException = false;
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            try
            {
                await stagingFile.AddBlockAsync(Guid.NewGuid().ToByteArray());
                await stagingFile.AddBlockAsync(content);
                await stagingFile.CommitAsync();
            }
            catch (InvalidOperationException)
            {
                gotException = true;
            }
            stagingFile.Dispose();
            Assert.IsTrue(gotException);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_ExtraLargeTextBlocks()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            var sb = new StringBuilder();
            for (int j = 0; j < 3; j++)
            {
                var sb2 = new StringBuilder();
                for (int i = 0; i < 65000; i++)
                {
                    string content = Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
                    sb2.Append(content);
                    sb.Append(content);
                }
                await stagingFile.AddBlockAsync(sb2.ToString());
            }
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            string readContent = await this.GetStringFromBlobAsync(containerName, fileName);
            Assert.AreEqual(sb.ToString(), readContent);
        }


        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_ExtraLargeTextBlocksWithExpandableChars()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            var sb = new StringBuilder();
            for (int j = 0; j < 2; j++)
            {
                var sb2 = new StringBuilder();
                for (int i = 0; i < 10000; i++)
                {
                    string content = @"ᚠᛇᚻ᛫ᛒᛦᚦ᛫ᚠᚱᚩᚠᚢᚱ᛫ᚠᛁᚱᚪ᛫ᚷᛖᚻᚹᛦᛚᚳᚢᛗᛋᚳᛖᚪᛚ᛫ᚦᛖᚪᚻ᛫ᛗᚪᚾᚾᚪ᛫ᚷᛖᚻᚹᛦᛚᚳ᛫ᛗᛁᚳᛚᚢᚾ᛫ᚻᛦᛏ᛫ᛞᚫᛚᚪᚾᚷᛁᚠ᛫ᚻᛖ᛫ᚹᛁᛚᛖ᛫ᚠᚩᚱ᛫ᛞᚱᛁᚻᛏᚾᛖ᛫ᛞᚩᛗᛖᛋ᛫ᚻᛚᛇᛏᚪᚾ᛬";
                    sb2.Append(content);
                    sb.Append(content);
                    content = @"Τὴ γλῶσσα μοῦ ἔδωσαν ἑλληνικὴτὸ σπίτι φτωχικὸ στὶς ἀμμουδιὲς τοῦ Ὁμήρου.Μονάχη ἔγνοια ἡ γλῶσσα μου στὶς ἀμμουδιὲς τοῦ Ὁμήρου.ἀπὸ τὸ Ἄξιον ἐστίτοῦ Ὀδυσσέα Ἐλύτη";
                    sb2.Append(content);
                    sb.Append(content);
                    content = @"ვეპხის ტყაოსანი შოთა რუსთაველი ღმერთსი შემვედრე, ნუთუ კვლა დამხსნას სოფლისა შრომასა, ცეცხლს, წყალსა და მიწასა, ჰაერთა თანა მრომასა; მომცნეს ფრთენი და აღვფრინდე, მივჰხვდე მას ჩემსა ნდომასა, დღისით და ღამით ვჰხედვიდე მზისა ელვათა კრთომაასა.";
                    sb2.Append(content);
                    sb.Append(content);
                    content = @"யாமறிந்த மொழிகளிலே தமிழ்மொழி போல் இனிதாவது எங்கும் காணோம், பாமரராய் விலங்குகளாய், உலகனைத்தும் இகழ்ச்சிசொலப் பான்மை கெட்டு, நாமமது தமிழரெனக் கொண்டு இங்கு வாழ்ந்திடுதல் நன்றோ? சொல்லீர்! தேமதுரத் தமிழோசை உலகமெலாம் பரவும்வகை செய்தல் வேண்டும். ";
                    sb2.Append(content);
                    sb.Append(content);
                    content = @"ಬಾ ಇಲ್ಲಿ ಸಂಭವಿಸು ಇಂದೆನ್ನ ಹೃದಯದಲಿ ನಿತ್ಯವೂ ಅವತರಿಪ ಸತ್ಯಾವತಾರ ಮಣ್ಣಾಗಿ ಮರವಾಗಿ ಮಿಗವಾಗಿ ಕಗವಾಗೀ... ಮಣ್ಣಾಗಿ ಮರವಾಗಿ ಮಿಗವಾಗಿ ಕಗವಾಗಿ ಭವ ಭವದಿ ಭತಿಸಿಹೇ ಭವತಿ ದೂರ ನಿತ್ಯವೂ ಅವತರಿಪ ಸತ್ಯಾವತಾರ || ಬಾ ಇಲ್ಲಿ || ";
                    sb2.Append(content);
                    sb.Append(content);
                }
                await stagingFile.AddBlockAsync(sb2.ToString());
            }
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            string readContent = await this.GetStringFromBlobAsync(containerName, fileName);
            Assert.AreEqual(sb.ToString(), readContent);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStagingFile_ExtraLargeBinaryBlocks()
        {
            string containerName = RecordCreator.GetRandomAzureBlobName(TestStagingFileContainerName);
            string fileName = "Test1.txt";
            IExportStagingFile stagingFile = await this.GetStagingFile(containerName, fileName);
            var memStream = new MemoryStream();
            var memStream2 = new MemoryStream();
            for (int j = 0; j < 12; j++)
            {
                memStream2.Position = 0;
                for (int i = 0; i < 640000; i++)
                {
                    var content = Guid.NewGuid();
                    await memStream.WriteAsync(content.ToByteArray(), 0, 16);
                    await memStream2.WriteAsync(content.ToByteArray(), 0, 16);
                }
                var buf = new byte[10240000];
                memStream2.Position = 0;
                await memStream2.ReadAsync(buf, 0, 10240000);
                await stagingFile.AddBlockAsync(buf);
            }
            memStream.Position = 0;
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            Assert.IsTrue(await this.FileExistsAsync(containerName, fileName));
            Stream readStream = await this.GetStreamFromBlobAsync(containerName, fileName);
            Assert.IsTrue(await this.CompareStreamsOfGuids(readStream, memStream));
        }

        private async Task<bool> CompareStreamsOfGuids(Stream readContent, Stream writtenContent)
        {
            if (readContent == null)
            {
                throw new ArgumentNullException(nameof(readContent));
            }
            if (writtenContent == null)
            {
                throw new ArgumentNullException(nameof(writtenContent));
            }
            if (readContent.Length != writtenContent.Length)
            {
                return false;
            }
            do
            {
                var buffer1 = new byte[16];
                var buffer2 = new byte[16];
                int read = await readContent.ReadAsync(buffer1, 0, 16);
                if (read == 0)
                {
                    break;
                }
                if (read != 16)
                {
                    return false;
                }
                read = await writtenContent.ReadAsync(buffer2, 0, 16);
                if (read != 16)
                {
                    return false;
                }
                if (new Guid(buffer1) != new Guid(buffer2))
                {
                    return false;
                }
            } while (true);
            return true;
        }

        private async Task<IExportStagingFile> GetStagingFile(string containerName, string relativePath)
        {
            var container = await AzureBlobWriter.GetContainerAsync(this.BlobClient, containerName);
            return new AzureBlobWriter(container, this.Logger, true, relativePath);
        }
    }
}
