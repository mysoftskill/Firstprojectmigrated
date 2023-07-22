// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models
{
    /// <summary>
    ///     Aad Rvs Scopes.
    ///     https://microsoft.sharepoint.com/:w:/t/DataScienceEngineering/ETThJvxBjyhPkpZjxbH8JuUBRoWCSekpQaB9GT-J6Tj7Pg?e=Hlxbic
    /// </summary>
    public class AadRvsScope
    {
        /// <summary>
        ///     Export scope for company administrator.
        /// </summary>
        public static string UserProcesscorExportAll = "User.Processor.Export.All";

        /// <summary>
        ///     Delete scope for company administrator.
        /// </summary>
        public static string UserProcesscorDeleteAll = "User.Processor.Delete.All";

        /// <summary>
        ///     Account close scope for company administrator.
        /// </summary>
        public static string UserProcesscorAccountCloseAll = "User.Processor.AccountClose.All";

        /// <summary>
        ///     Reserved for CELA initiated/OBO scenario.
        /// </summary>
        public static string UserControllerExportAll = "User.Controller.Export.All";

        /// <summary>
        ///     Reserved for CELA initiated/OBO scenario.
        /// </summary>
        public static string UserControllerDeleteAll = "User.Controller.Delete.All";

        /// <summary>
        ///     Export scope for viral user.
        /// </summary>
        public static string UserProcessorExport = "User.Processor.Export";

        /// <summary>
        ///     Delete scope for viral user.
        /// </summary>
        public static string UserProcessorDelete = "User.Processor.Delete";

        /// <summary>
        ///     Account close scope for viral user.
        /// </summary>
        public static string UserProcessorAccountClose = "User.Processor.AccountClose";

        /// <summary>
        ///     Export scope for viral user, AAD user, Company Administrator.
        /// </summary>
        public static string UserControllerExport = "User.Controller.Export";
    }
}
