// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace PCF.UnitTests
{
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Newtonsoft.Json.Linq;

    internal class CoreCommandDocumentBuilder : TestDataBuilder<CoreCommandDocument>, INeedDataBuilders
    {
        protected override CoreCommandDocument CreateNewObject()
        {
            AgeOutRequest pxsCommand = this.AnAgeOutPxsCommand().Build();
            return new CoreCommandDocument(new CommandHistoryCoreRecord(new CommandId(pxsCommand.RequestId))) { PxsCommand = JObject.FromObject(pxsCommand) };
        }
    }
}
