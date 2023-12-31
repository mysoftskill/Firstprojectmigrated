namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using System;

    /// <summary>
    /// Extension methods for the <see cref="ICommandVisitor{TCommand, TReturn}"/> interface.
    /// </summary>
    public static class CommandVisitorExtensions
    {
        /// <summary>
        /// A unified classify-then-visit implementation. Eases the process of adding new command types in the future by centralizing the switch statement.
        /// </summary>
        public static TReturn Process<TCommand, TReturn>(this ICommandVisitor<TCommand, TReturn> visitor, TCommand command)
        {
            PrivacyCommandType type = visitor.Classify(command);

            switch (visitor.Classify(command))
            {
                case PrivacyCommandType.AccountClose:
                    return visitor.VisitAccountClose(command);

                case PrivacyCommandType.AgeOut:
                    return visitor.VisitAgeOut(command);

                case PrivacyCommandType.Delete:
                    return visitor.VisitDelete(command);

                case PrivacyCommandType.Export:
                    return visitor.VisitExport(command);

                default:
                    throw new InvalidOperationException($"Unable to visit PrivacyCommandType = {type}, TCommandType = {typeof(TCommand)}, VisitorType = {visitor.GetType()}");
            }
        }
    }
}
