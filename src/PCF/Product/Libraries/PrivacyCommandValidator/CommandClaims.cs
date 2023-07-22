namespace Microsoft.PrivacyServices.CommandFeed.Validator
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Contains the claims from the command to validate.
    /// </summary>
    public class CommandClaims
    {
        /// <summary>
        /// Id of the command which should be same as request Id.
        /// </summary>
        public string CommandId { get; set; }

        /// <summary>
        /// PrivacyCommand subject, AAD, MSA, Device, Demographic, or MicrosoftEmployee.
        /// </summary>
        public IPrivacySubject Subject { get; set; }

        /// <summary>
        /// CloudInstance, in the case of AAD 
        /// Public, or any one of the sovereign cloud instances
        /// </summary>
        public string CloudInstance { get; set; } = Configuration.CloudInstance.Public;

        /// <summary>
        /// Command operation.
        /// </summary>
        public ValidOperation Operation { get; set; }

        /// <summary>
        /// The Azure blob container uri for Export commands.
        /// </summary>
        public Uri AzureBlobContainerTargetUri { get; set; }

        /// <summary>
        /// Indicates if this command applies to processor data.
        /// </summary>
        public bool ProcessorApplicable { get; set; }

        /// <summary>
        /// Indicates if this command applies to controller data.
        /// </summary>
        public bool ControllerApplicable { get; set; }

        /// <summary>
        /// The Delete command data type.
        /// </summary>
        public DataTypeId DataType { get; set; }
    }
}
