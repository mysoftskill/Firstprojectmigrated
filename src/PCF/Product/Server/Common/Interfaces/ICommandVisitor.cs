namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.PrivacyServices.CommandFeed.Client;

    /// <summary>
    /// A highly generic visitor pattern implementation for commands.
    /// </summary>
    /// <typeparam name="TCommand">The general type of command to be classified.</typeparam>
    /// <typeparam name="TReturn">The return type.</typeparam>
    public interface ICommandVisitor<in TCommand, out TReturn>
    {
        /// <summary>
        /// Classifies the command type.
        /// </summary>
        PrivacyCommandType Classify(TCommand command);

        /// <summary>
        /// Visists the delete command.
        /// </summary>
        TReturn VisitDelete(TCommand deleteCommand);

        /// <summary>
        /// Visists the export command.
        /// </summary>
        TReturn VisitExport(TCommand exportCommand);

        /// <summary>
        /// Visists the account close command.
        /// </summary>
        TReturn VisitAccountClose(TCommand accountCloseCommand);

        /// <summary>
        /// Visists the age out command.
        /// </summary>
        TReturn VisitAgeOut(TCommand ageOutCommand);
    }
}
