namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Enumerates types of subjects of Privacy Commands.
    /// </summary>
    /// <remarks>
    /// These values are meaningful, since the enum is serialized as an integer in the lease receipt. Don't reassign or reorder.
    /// </remarks>
    public enum SubjectType : int
    {
        /// <summary>
        /// The MSA (Consumer) subject.
        /// </summary>
        Msa = 0,

        /// <summary>
        /// The AAD (enterprise) subject.
        /// </summary>
        Aad = 1,

        /// <summary>
        /// The device subject.
        /// </summary>
        Device = 2,

        /// <summary>
        /// The demographic subject.
        /// </summary>
        Demographic = 3,

        /// <summary>
        /// Non-Windows device subject.
        /// </summary>
        NonWindowsDevice = 4,

        /// <summary>
        /// Edge Browser device subject.
        /// </summary>
        EdgeBrowser = 5,

        /// <summary>
        /// The AAD subject supports Multi-tenant collaboration.
        /// </summary>
        Aad2 = 6,

        /// <summary>
        /// The MicrosoftEmployee Subject
        /// </summary>
        MicrosoftEmployee = 7,
    }
}
