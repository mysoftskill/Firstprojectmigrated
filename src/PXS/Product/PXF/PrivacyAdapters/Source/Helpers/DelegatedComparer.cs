// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using System;
    using System.Collections.Generic;

    public class DelegatedComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> compareFunc;

        public DelegatedComparer(Func<T, T, int> compareFunc)
        {
            this.compareFunc = compareFunc;
        }

        public int Compare(T x, T y)
        {
            return this.compareFunc(x, y);
        }
    }
}
