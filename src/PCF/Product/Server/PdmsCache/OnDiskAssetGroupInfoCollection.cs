namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines an interface capable of reading asset group information from an asynchronous source.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public sealed class OnDiskAssetGroupInfoCollection : IAssetGroupInfoReader
    {
        private readonly IAssetGroupInfoReader innerReader;
        private long? latestOnDiskVersion;

        static OnDiskAssetGroupInfoCollection()
        {
            Directory.CreateDirectory(Config.Instance.PdmsCache.OnDiskCache.Directory);
        }

        public OnDiskAssetGroupInfoCollection(IAssetGroupInfoReader innerReader)
        {
            this.innerReader = innerReader;

            // Find the largest file name already in the directory.
            string[] fileNames = Directory.GetFiles(Config.Instance.PdmsCache.OnDiskCache.Directory, "ondiskpdmscache_*.json");
            if (fileNames.Length > 0)
            {
                this.latestOnDiskVersion = fileNames
                    .Select(Path.GetFileNameWithoutExtension)
                    .Select(x => x.Replace("ondiskpdmscache_", string.Empty))
                    .Select(long.Parse)
                    .Max();
            }
        }

        public async Task<long> GetLatestVersionAsync()
        {
            try
            {
                return await this.innerReader.GetLatestVersionAsync();
            }
            catch
            {
                if (this.latestOnDiskVersion != null)
                {
                    return this.latestOnDiskVersion.Value;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<AssetGroupInfoCollectionReadResult> ReadAsync()
        {
            long latestVersion = await this.GetLatestVersionAsync();
            return await this.ReadVersionAsync(latestVersion);
        }

        public async Task<AssetGroupInfoCollectionReadResult> ReadVersionAsync(long version)
        {
            if (TryReadLocalVersion(version, out var result))
            {
                return result;
            }

            result = await this.innerReader.ReadVersionAsync(version);
            WriteLocalVersion(version, result);

            this.latestOnDiskVersion = result.DataVersion;
            return result;
        }

#if INCLUDE_TEST_HOOKS
        /// <summary>
        /// Unit test hook to clear data prior to running. Not safe to use in actual product code.
        /// </summary>
        internal static void ClearFiles()
        {
            string[] fileNames = Directory.GetFiles(Config.Instance.PdmsCache.OnDiskCache.Directory, "ondiskpdmscache_*.json");
            foreach (var file in fileNames)
            {
                File.Delete(file);
            }
        }
#endif

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void WriteLocalVersion(long version, AssetGroupInfoCollectionReadResult result)
        {
            try
            {
                Logger.InstrumentSynchronous(
                    new OutgoingEvent(SourceLocation.Here()),
                    ev =>
                    {
                        string finalPath = Path.Combine(Config.Instance.PdmsCache.OnDiskCache.Directory, $"ondiskpdmscache_{version}.json");
                        string tempPath = Path.GetTempFileName();

                        using (var streamWriter = File.CreateText(tempPath))
                        using (var jsonReader = new JsonTextWriter(streamWriter))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Serialize(jsonReader, result);
                        }

                        File.Copy(tempPath, finalPath, overwrite: true);
                    });
            }
            catch (Exception ex)
            {
                // This error is logged by the instrumentation, and we shouldn't block the whole operation in this case since
                // we can always reread from the docdb next time.
                DualLogger.Instance.Error(nameof(OnDiskAssetGroupInfoCollection), ex, "Error writing local PDMS data!");
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static bool TryReadLocalVersion(long version, out AssetGroupInfoCollectionReadResult result)
        {
            AssetGroupInfoCollectionReadResult localResult = null;
            bool outcome = false;

            try
            {
                Logger.InstrumentSynchronous(
                    new OutgoingEvent(SourceLocation.Here()),
                    ev =>
                    {
                        string path = Path.Combine(Config.Instance.PdmsCache.OnDiskCache.Directory, $"ondiskpdmscache_{version}.json");

                        if (File.Exists(path))
                        {
                            using (var fs = File.OpenRead(path))
                            using (var streamReader = new StreamReader(fs))
                            using (var jsonReader = new JsonTextReader(streamReader))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                localResult = serializer.Deserialize<AssetGroupInfoCollectionReadResult>(jsonReader);
                                outcome = true;
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                // An exception here isn't fatal. We can just ignore and continue on.
                localResult = null;
                outcome = false;
                DualLogger.Instance.Error(nameof(OnDiskAssetGroupInfoCollection), ex, "Unable to read local PDMS data!");
            }

            result = localResult;
            return outcome;
        }
    }
}
