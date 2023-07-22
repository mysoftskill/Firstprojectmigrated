// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System.Diagnostics;

    public static class TraceListenerCollectionExtensions
    {
        /// <summary>
        /// Adds a new named trace listener to the beginning of the collection. Naming a listener allows later removal by the same name.
        /// </summary>
        /// <param name="collection">The collection of listeners to add to.</param>
        /// <param name="name">The friendly name for the new listener. Does not have to be unique.</param>
        /// <param name="listener">The listener to add.</param>
        public static void Add(this TraceListenerCollection collection, string name, TraceListener listener)
        {
            collection.Insert(0, listener);
            collection[0].Name = name;
        }
    }
}
