// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System.Threading.Tasks;

    public static class TaskUtilities
    {
        /// <summary>
        /// Similar to Task.CompletedTask from .NET 4.6
        /// https://msdn.microsoft.com/en-us/library/system.threading.tasks.task.completedtask(v=vs.110).aspx
        /// </summary>
        public static Task CompletedTask
        {
            get
            {
                return Task.FromResult((object)null);
            }
        }
    }
}
