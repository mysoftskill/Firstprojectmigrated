// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System.Threading.Tasks;
    using Moq.Language.Flow;

    public static class MockExtensions
    {
        public static IReturnsResult<TMock> ReturnsAsync<TMock, TResult>(this ISetup<TMock, Task<TResult>> setup, TResult value)
            where TMock : class
        {
            return setup.Returns(Task.FromResult(value));
        }
    }
}