namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.PrivacyServices.CommandFeed.Client;

    /// <summary>
    /// Implementation of <see cref="ICommandVisitor{TCommand, TReturn}"/> for the general PCF notion of a command.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    public abstract class PrivacyCommandVisitor<T> : ICommandVisitor<PrivacyCommand, T>
    {
        /// <summary>
        /// Classifies according to the command's self-reported type.
        /// </summary>
        public PrivacyCommandType Classify(PrivacyCommand command) => command.CommandType;

        /// <inheritdoc />
        public T VisitAccountClose(PrivacyCommand accountCloseCommand) => this.Visit((AccountCloseCommand)accountCloseCommand);

        /// <inheritdoc />
        public T VisitAgeOut(PrivacyCommand ageOutCommand) => this.Visit((AgeOutCommand)ageOutCommand);

        /// <inheritdoc />
        public T VisitDelete(PrivacyCommand deleteCommand) => this.Visit((DeleteCommand)deleteCommand);

        /// <inheritdoc />
        public T VisitExport(PrivacyCommand exportCommand) => this.Visit((ExportCommand)exportCommand);

        protected abstract T Visit(AccountCloseCommand command);

        protected abstract T Visit(DeleteCommand command);

        protected abstract T Visit(ExportCommand command);

        protected abstract T Visit(AgeOutCommand command);
    }
}
