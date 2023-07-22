// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    /// <summary>
    /// Defines an interface to visit commands based on command type
    /// </summary>
    /// <typeparam name="T">The </typeparam>
    public interface ICommandVisitor<T>
    {
        /// <summary>
        /// Defines a visit method for <see cref="DeleteCommand"/>
        /// </summary>
        /// <param name="command">The command</param>
        T Visit(DeleteCommand command);

        /// <summary>
        /// Defines a visit method for <see cref="ExportCommand"/>
        /// </summary>
        /// <param name="command">The command</param>
        T Visit(ExportCommand command);

        /// <summary>
        /// Defines a visit method for <see cref="AccountCloseCommand"/>
        /// </summary>
        /// <param name="command">The command</param>
        T Visit(AccountCloseCommand command);

        /// <summary>
        /// Defines a visit method for <see cref="AgeOutCommand"/>
        /// </summary>
        /// <param name="command">The command</param>
        T Visit(AgeOutCommand command);
    }
}
