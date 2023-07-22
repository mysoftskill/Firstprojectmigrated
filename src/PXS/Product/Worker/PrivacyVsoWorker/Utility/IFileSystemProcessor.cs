namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility
{
    /// <summary>
    ///     File system processor to access the files.
    /// </summary>
    public interface IFileSystemProcessor
    {
        /// <summary>
        ///     Get description to use for Work item creation
        /// </summary>
        /// <returns></returns>
        string GetAgentsWithNoConnectorsItemDesc();
    }
}
