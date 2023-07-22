// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.PerfCounters
{
    using System;

    public class NoOpCounter : ICounter
    {
        public void Decrement()
        {
        }

        public void Decrement(string instanceName)
        {
        }

        public void DecrementBy(ulong counterValue)
        {
            throw new NotImplementedException();
        }

        public void DecrementBy(ulong counterValue, string instanceName)
        {
        }

        public ulong GetValue()
        {
            return 0;
        }

        public ulong GetValue(string instanceName)
        {
            return 0;
        }

        public void Increment()
        {
        }

        public void Increment(string instanceName)
        {
        }

        public void IncrementBy(ulong counterValue)
        {
        }

        public void IncrementBy(ulong counterValue, string instanceName)
        {
        }

        public void SetValue(ulong counterValue)
        {
        }

        public void SetValue(ulong counterValue, string instanceName)
        {
        }
    }
}
