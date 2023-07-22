// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Net.Http;

    public class ConsoleCommandFeedLogger : CommandFeedLogger
    {
        public override void BeginServiceToServiceAuthRefresh(string targetSiteName, long siteId)
        {
            Console.WriteLine($"{nameof(this.BeginServiceToServiceAuthRefresh)}({targetSiteName}, {siteId})");
        }

        public override void HttpResponseReceived(HttpRequestMessage request, HttpResponseMessage response)
        {
            Console.WriteLine($"{nameof(this.HttpResponseReceived)}([{request.RequestUri}], [{response.StatusCode} {response.ReasonPhrase}])");
        }

        public override void UnhandledException(Exception ex)
        {
            Console.WriteLine($"{ex}");
        }

        public override void UnrecognizedDataType(string cv, string commandId, string dataType)
        {
            Console.WriteLine($"{nameof(this.UnrecognizedDataType)}({cv}, {commandId}, {dataType})");
        }

        public override void UnrecognizedCommandType(string cv, string commandId, string commandType)
        {
            Console.WriteLine($"{nameof(this.UnrecognizedCommandType)}({cv}, {commandId}, {commandType})");
        }
    }
}
