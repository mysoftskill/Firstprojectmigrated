namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     File system processor to access the files.
    /// </summary>
    public class FileSystemProcessor : IFileSystemProcessor
    {
        private string agentsWithNoConnectorsItemDesc;

        public FileSystemProcessor(
            IFileSystemProcessorConfig config,
            string appPath)
        {
            ArgumentCheck.ThrowIfNull(config, nameof(config));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(appPath, nameof(appPath));

            this.LoadConfigFiles(appPath.Trim(), config);
        }

        /// <summary>
        ///     Get description to use for Work item creation
        /// </summary>
        /// <returns></returns>
        public string GetAgentsWithNoConnectorsItemDesc()
        {
            return this.agentsWithNoConnectorsItemDesc;
        }

        private void LoadConfigFiles(string appPath, IFileSystemProcessorConfig config)
        {
            var readTasks = new List<Task>();
            using (Task<string> taskWorkItemDescriptionPath = this.ReadFileAsync(Path.Combine(appPath, config.WorkItemDescriptionPath)))
            {
                Task.Run(() => taskWorkItemDescriptionPath).Wait();
                this.agentsWithNoConnectorsItemDesc = taskWorkItemDescriptionPath.Result;
            }
        }

        /// <summary>
        ///     Reads a file as a string and returns the contents
        /// </summary>
        /// <param name="path">file to read</param>
        /// <returns>resulting value</returns>
        private async Task<string> ReadFileAsync(string path)
        {
            using (
                Stream stream =
                    new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan))
            using (TextReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}
