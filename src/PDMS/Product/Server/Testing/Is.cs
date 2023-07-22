namespace Microsoft.PrivacyServices.Testing
{
    using System;
    using System.Linq.Expressions;
    using Moq;

    /// <summary>
    /// Provides a more useful comparison approach for <c>Moq</c>.
    /// </summary>
    public static class Is
    {
        /// <summary>
        /// Determines if an invocation used the expected value based on the verification function.
        /// The given action should use Assert to verify.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <param name="verify">A validation function that uses Assert to fail.</param>
        /// <returns>The value for <c>Moq</c>.</returns>
        public static T Value<T>(Action<T> verify)
        {
            Func<T, bool> verifyFunc = value => 
            {
                verify(value);
                return true;
            };

            Expression<Func<T, bool>> verifyExp = value => verifyFunc(value);

            return It.Is<T>(verifyExp);
        }
    }
}