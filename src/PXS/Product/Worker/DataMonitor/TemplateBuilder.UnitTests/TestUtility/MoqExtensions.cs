// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.TestUtility
{
    using System.Reflection;

    using Moq.Language;
    using Moq.Language.Flow;

    // derived from a StackOverflow answer
    public static class MoqExtensions
    {
        public delegate void OutAction<TOut>(out TOut outVal);
        public delegate void OutAction<in T1, TOut>(T1 arg1, out TOut outVal);
        public delegate void OutAction<in T1, in T2, TOut>(T1 arg1, T2 arg2, out TOut outVal);
        public delegate void OutAction<in T1, in T2, in T3, TOut>(T1 arg1, T2 arg2, T3 arg3, out TOut outVal);

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, TOut>(
            this ICallback<TMock, TReturn> mock, 
            OutAction<TOut> action)
            where TMock : class
        {
            return mock.Callback(action);
        }

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, T1, TOut>(
            this ICallback<TMock, TReturn> mock, 
            OutAction<T1, TOut> action)
            where TMock : class
        {
            return mock.Callback(action);
        }

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, T1, T2, TOut>(
            this ICallback<TMock, TReturn> mock,
            OutAction<T1, T2, TOut> action)
            where TMock : class
        {
            return mock.Callback(action);
        }

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, T1, T2, T3, TOut>(
            this ICallback<TMock, TReturn> mock,
            OutAction<T1, T2, T3, TOut> action)
            where TMock : class
        {
            return mock.Callback(action);
        }
    }
}
