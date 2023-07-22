// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    /// <summary>
    /// Implementation for validator visitor
    /// </summary>
    public class ValidatorVisitor : ICommandVisitor<ValidOperation>
    {
        private readonly CommandFeedLogger logger;

        /// <summary>
        /// Prevents a default instance of <see cref="ValidatorVisitor"/> from being created.
        /// </summary>
        private ValidatorVisitor()
        {
        }

        /// <summary>
        /// Constructor for <see cref="ValidatorVisitor"/>
        /// </summary>
        /// <param name="logger">The logger</param>
        public ValidatorVisitor(CommandFeedLogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public ValidOperation Visit(DeleteCommand command)
        {
            return ValidOperation.Delete;
        }

        /// <inheritdoc />
        public ValidOperation Visit(ExportCommand command)
        {
            return ValidOperation.Export;
        }

        /// <inheritdoc />
        public ValidOperation Visit(AccountCloseCommand command)
        {
            if (command.Subject is AadSubject2 aadSubject2)
            {
                if (aadSubject2.TenantIdType == TenantIdType.Resource)
                {
                    return ValidOperation.AccountCleanup;
                }
            }

            return ValidOperation.AccountClose;
        }

        /// <inheritdoc />
        public ValidOperation Visit(AgeOutCommand command)
        {
            if (command.Subject is MsaSubject)
            {
                return ValidOperation.AccountClose;
            }

            this.logger.UnrecognizedCommandType(command.CorrelationVector, command.CommandId, command.GetType().ToString());
            return ValidOperation.None;
        }
    }
}
