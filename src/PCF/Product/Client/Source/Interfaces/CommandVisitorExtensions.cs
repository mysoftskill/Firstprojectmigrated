// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;

    /// <summary>
    /// Extensions for <see cref="ICommandVisitor{T}"/>
    /// </summary>
    public static class CommandVisitorExtensions
    {
        /// <summary>
        /// Visit the command
        /// </summary>
        /// <param name="visitor">The command visitor</param>
        /// <param name="command">The command</param>
        /// <typeparam name="T">The type to visit</typeparam>
        public static T Visit<T>(this ICommandVisitor<T> visitor, IPrivacyCommand command)
        {
            switch (command)
            {
                case AgeOutCommand ageOutCommand:
                    return visitor.Visit(ageOutCommand);

                case DeleteCommand deleteCommand:
                    return visitor.Visit(deleteCommand);

                case ExportCommand exportCommand:
                    return visitor.Visit(exportCommand);

                case AccountCloseCommand accountCloseCommand:
                    return visitor.Visit(accountCloseCommand);

                default:
                    throw new InvalidOperationException($"Unexpected command type: {command.GetType()}");
            }
        }
    }
}
