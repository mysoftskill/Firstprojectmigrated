// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    public enum ExportArchivesDeleteStatus
    {
        DeleteNotRequested = 0,
        DeleteInProgress = 1,
        DeleteCompleted = 2,
        DeleteFailed = 3,
        NotApplicable = 4,
    }
}