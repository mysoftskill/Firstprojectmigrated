// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.CommandStatus
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