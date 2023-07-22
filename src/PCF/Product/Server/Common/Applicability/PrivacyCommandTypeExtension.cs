namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Defines the <see cref="PrivacyCommandTypeExtension" />
    /// </summary>
    public static class PrivacyCommandTypeExtension
    {
        private static readonly CapabilityIdVisitor Visitor = new CapabilityIdVisitor();

        /// <summary>
        /// Converts <see cref="PrivacyCommandType"/> to <see cref="CapabilityId"/>.
        /// </summary>
        /// <param name="privacyCommandType">The privacyCommandType <see cref="PrivacyCommandType"/></param>
        /// <returns>The <see cref="CapabilityId"/></returns>
        public static CapabilityId ToCapabilityId(this PrivacyCommandType privacyCommandType)
        {
            return Visitor.Process(privacyCommandType);
        }

        private class CapabilityIdVisitor : ICommandVisitor<PrivacyCommandType, CapabilityId>
        {
            public PrivacyCommandType Classify(PrivacyCommandType command) => command;

            public CapabilityId VisitAccountClose(PrivacyCommandType accountCloseCommand) => Policies.Current.Capabilities.Ids.AccountClose;

            public CapabilityId VisitAgeOut(PrivacyCommandType ageOutCommand) => Policies.Current.Capabilities.Ids.AgeOut;

            public CapabilityId VisitDelete(PrivacyCommandType deleteCommand) => Policies.Current.Capabilities.Ids.Delete;

            public CapabilityId VisitExport(PrivacyCommandType exportCommand) => Policies.Current.Capabilities.Ids.Export;
        }
    }
}
