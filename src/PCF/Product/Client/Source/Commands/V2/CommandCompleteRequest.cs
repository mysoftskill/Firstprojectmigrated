namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// The Command completion request body which contains the parameters for commandCompletion API.
    /// </summary>
    public class CommandCompleteRequest
    {
        /// <summary>
        /// [Required] The status of the command complete request from agent. Currently it is set to complete.
        /// </summary>
        [Required]
        public string Status { get; set; }

        /// <summary>
        /// [Required] The completion token of the command complete request from agent. This value should be fetched from the response of command pages api and feed it here.
        /// </summary>
        public string CompletionToken { get; set; }

        /// <summary>
        /// [Required] [Export Only] The staging container which is required only for export case.
        /// </summary>
        public Uri StagingContainer { get; set; }

        /// <summary>
        /// [Required] [Export Only] The staging root folder for all export data in one batch.
        /// </summary>
        public string StagingRootFolder { get; set; }

        /// <summary>
        /// [Optional] The command id is required only in case the status is validationFailure.
        /// </summary>
        public string CommandId { get; set; }

        /// <summary>
        /// [Optional] [Delete Only] The AssetUris that have been successfully completed.
        /// </summary>
        public string[] SucceededAssetUris { get; set; }

        /// <summary>
        /// [Optional] [Delete Only] The AssetUris that failed to complete.
        /// </summary>
        public string[] FailedAssetUris { get; set; }
    }
}
