// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.GenevaTest
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;

    using static System.FormattableString;

    public class ConsoleOperation : ITraceOperation
    {
        public readonly string EventName;

        public string Message;

        public ConsoleOperation(string eventName)
        {
            this.EventName = eventName;
        }

        public void Dispose()
        {
            Console.WriteLine(Invariant($"Event Name: {this.EventName} with message {this.Message}"));
        }

        public void SetResult(TraceResult traceResult, string resultSignature, string resultDetails,bool isIncomingCall)
        {
            this.Message = Invariant($"Trace Result is: {traceResult}. Result Signature is: {resultSignature}. Result Details is: {resultDetails}. IsIncomingCall: {isIncomingCall}");
        }
    }
}
