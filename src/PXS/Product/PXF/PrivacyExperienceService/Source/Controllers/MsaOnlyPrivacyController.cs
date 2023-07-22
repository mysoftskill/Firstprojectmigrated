// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using System.Web.Http;

    /// <summary>
    ///     MsaOnlyPrivacyController.
    /// </summary>
    [Authorize]
    [PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.Default)]
    public class MsaOnlyPrivacyController : PrivacyController
    {
    }
}
