// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Azure.Storage.Queue;

    using Moq;
    using System.IO.Compression;
    using System.Text;

    using Microsoft.PrivacyServices.Common.Azure;
    public class StorageEmulatorBase
    {
        public ILogger Logger { get; }

        public AzureStorageProvider StorageProvider { get; set; }

        public CloudBlobClient BlobClient { get; set; }

        public CloudQueueClient QueueClient { get; set; }
        
        protected readonly Mock<IAzureStorageConfiguration> mockAzureStorageConfiguration = new Mock<IAzureStorageConfiguration>();

        protected readonly Mock<ISecretStoreReader> mockSecretStoreReader = new Mock<ISecretStoreReader>(MockBehavior.Strict);

        public StorageEmulatorBase()
        {
            Trace.Listeners.Add("ConsoleListener", new ConsoleTraceListener());
            this.Logger = IfxTraceLogger.Instance;
            TraceLogger.TraceSwitch.Level = TraceLevel.Verbose;

        }

        public void StartEmulator()
        {
            TryStartEmulatorProcess();

            for (int i=0; i<2; i++)
            {
                try
                {
                    this.StorageProvider = new AzureStorageProvider(this.Logger, this.mockSecretStoreReader.Object);
                    this.StorageProvider.InitializeAsync(this.mockAzureStorageConfiguration.Object).GetAwaiter().GetResult();
                    this.BlobClient = this.StorageProvider.CreateCloudBlobClient();
                    this.QueueClient = this.StorageProvider.CreateCloudQueueClient();
                    var properties = this.BlobClient.GetServiceProperties();
                    Console.WriteLine("Azure service version is " + properties.DefaultServiceVersion);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("unable to properly initialize storage provider");
                    Console.WriteLine(e.ToString());
                    TryStopEmulatorProcess();
                    TryStartEmulatorProcess();
                }
            }
        }

        public async Task<bool> ContainerExistsAsync(string containerName)
        {
            var stagingContainer = this.BlobClient.GetContainerReference(containerName);
            return await stagingContainer.ExistsAsync();
        }

        public async Task<bool> FileExistsAsync(string containerName, string uriString)
        {
            var stagingContainer = this.BlobClient.GetContainerReference(containerName);
            bool exists = await stagingContainer.ExistsAsync();
            if (!exists)
            {
                return false;
            }
            var beforeList = stagingContainer.ListBlobs(null, true);
            return beforeList != null && beforeList.Any(i => i.Uri.ToString() == RecordCreator.DevStoreUriPrefix + containerName + "/" + uriString);
        }

        public async Task<string> GetStringFromBlobAsync(string containerName, string blobName)
        {
            var stagingContainer = this.BlobClient.GetContainerReference(containerName);
            bool exists = await stagingContainer.ExistsAsync();
            if (!exists)
            {
                return null;
            }

            var blob = stagingContainer.GetBlobReference(blobName);
            exists = await blob.ExistsAsync();
            if (!exists)
            {
                return null;
            }
            using (Stream readStream = await blob.OpenReadAsync())
            using (var reader = new StreamReader(readStream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public async Task<string> GetStringFromZipBlobAsync(string containerName, string blobName, bool contentOnly=false)
        {
            var stagingContainer = this.BlobClient.GetContainerReference(containerName);
            bool exists = await stagingContainer.ExistsAsync();
            if (!exists)
            {
                return null;
            }

            var blob = stagingContainer.GetBlobReference(blobName);
            exists = await blob.ExistsAsync();
            if (!exists)
            {
                return null;
            }
            using (Stream zipStream = await blob.OpenReadAsync())
            {
                return this.ExtractStringFromZipStream(zipStream, contentOnly);
            }
        }

        public string ExtractStringFromZipStream(Stream zipStream, bool contentOnly)
        {
            var sb = new StringBuilder();
            using (var outStream = new MemoryStream())
            {
                zipStream.CopyTo(outStream);
                using (var archive = new ZipArchive(outStream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (!contentOnly)
                        {
                            sb.AppendLine("Zip Entry: " + entry.FullName + " " + entry.Length + " bytes");
                        }

                        if (entry.FullName.EndsWith("json", StringComparison.OrdinalIgnoreCase)
                            || entry.FullName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var reader = new StreamReader(entry.Open()))
                            {
                                sb.Append(reader.ReadToEnd());
                            }
                        }

                        if (!contentOnly)
                        {
                            sb.Append("\r\n");
                        }
                    }
                }
            }
            return sb.ToString();

        }

        public async Task<Stream> GetStreamFromBlobAsync(string containerName, string blobName)
        {
            var stagingContainer = this.BlobClient.GetContainerReference(containerName);
            bool exists = await stagingContainer.ExistsAsync();
            if (!exists)
            {
                return null;
            }

            var blob = stagingContainer.GetBlobReference(blobName);
            exists = await blob.ExistsAsync();
            if (!exists)
            {
                return null;
            }
            return await blob.OpenReadAsync();
        }

        private static void TryStartEmulatorProcess()
        {
            var start = new ProcessStartInfo
            {
                Arguments = "Start",
                FileName = @"c:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe"
            };

            for (int i = 0; i < 3; i++)
            {
                Process[] processes = Process.GetProcessesByName("AzureStorageEmulator");
                if (processes.Length == 0)
                {
                    var exitCode = ExecuteProcess(start);
                    if (exitCode == 0)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
                // give it a couple seconds
                Thread.Sleep(2000);
            }
        }

        private static void TryStopEmulatorProcess()
        {
            var stopCmd = new ProcessStartInfo
            {
                Arguments = "stop",
                FileName = @"c:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe"
            };

            for (int i = 0; i < 3; i++)
            {
                Process[] processes = Process.GetProcessesByName("AzureStorageEmulator");
                if (processes.Length != 0)
                {
                    var exitCode = ExecuteProcess(stopCmd);
                    if (exitCode == 0)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
                // give it a couple seconds
                Thread.Sleep(2000);
            }
        }

        private static int ExecuteProcess(ProcessStartInfo start)
        {
            int exitCode;
            using (var proc = new Process { StartInfo = start })
            {
                proc.Start();
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }
            return exitCode;
        }

    }
}
