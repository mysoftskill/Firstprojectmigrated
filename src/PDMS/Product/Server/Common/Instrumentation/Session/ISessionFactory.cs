namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A factory to create and activate new session instances.
    /// </summary>
    public interface ISessionFactory
    {
        /// <summary>
        /// Given a specific operation name and session type,
        /// create a new session and begin calculating duration.
        /// </summary>
        /// <param name="name">The name of the operation.</param>
        /// <param name="sessionType">The type of session.</param>
        /// <returns>A session object that has already begun counting duration.</returns>
        ISession StartSession(string name, SessionType sessionType);
    }

    /// <summary>
    /// Extension functions for the session factory classes.
    /// </summary>
    public static class SessionFactoryExtensions
    {
        /// <summary>
        /// Automatically instrument a specific function.
        /// Any exception that is thrown will be logged using ISessionWriter{Exception}.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function. This is passed to the success function for additional logging.</typeparam>
        /// <param name="sessionFactory">A factory to create the new session.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="sessionType">The type of session.</param>
        /// <param name="function">The code that should be instrumented.</param>
        /// <returns>The result of the "function".</returns>
        public static TResult Instrument<TResult>(this ISessionFactory sessionFactory, string name, SessionType sessionType, Func<TResult> function)
        {
            return Instrument<TResult, Exception>(sessionFactory, name, sessionType, function);
        }

        /// <summary>
        /// Automatically instrument a specific function.
        /// Any exception that is thrown will be logged using ISessionWriter{TException}.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function. This is passed to the success function for additional logging.</typeparam>
        /// /// <typeparam name="TException">The exception type that is expected.</typeparam>
        /// <param name="sessionFactory">A factory to create the new session.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="sessionType">The type of session.</param>
        /// <param name="function">The code that should be instrumented.</param>
        /// <returns>The result of the "function".</returns>
        public static TResult Instrument<TResult, TException>(this ISessionFactory sessionFactory, string name, SessionType sessionType, Func<TResult> function)
            where TException : Exception
        {
            return Instrument<TResult, Exception>(sessionFactory, name, sessionType, _ => function());
        }

        /// <summary>
        /// Automatically instrument a specific function.
        /// Any exception that is thrown will be logged using ISessionWriter{TException}.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function. This is passed to the success function for additional logging.</typeparam>
        /// /// <typeparam name="TException">The exception type that is expected.</typeparam>
        /// <param name="sessionFactory">A factory to create the new session.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="sessionType">The type of session.</param>
        /// <param name="function">The code that should be instrumented.</param>
        /// <returns>The result of the "function".</returns>
        public static TResult Instrument<TResult, TException>(this ISessionFactory sessionFactory, string name, SessionType sessionType, Func<ISession, TResult> function)
            where TException : Exception
        {
            var session = sessionFactory.StartSession(name, sessionType);
            try
            {
                var result = function(session);
                session.Success(result);
                return result;
            }
            catch (Exception ex)
            {
                var expectedException = ex as TException;

                if (expectedException != null)
                {
                    session.Fault(expectedException);
                }
                else
                {
                    session.Fault(ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Automatically instrument an async function.
        /// Any exception that is thrown will be logged using ISessionWriter{Exception}.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function. This is passed to the success function for additional logging.</typeparam>        
        /// <param name="sessionFactory">A factory to create the new session.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="sessionType">The type of session.</param>
        /// <param name="function">The code that should be instrumented.</param>
        /// <returns>The result of the "function".</returns>
        public static Task<TResult> InstrumentAsync<TResult>(this ISessionFactory sessionFactory, string name, SessionType sessionType, Func<Task<TResult>> function)
        {
            return InstrumentAsync<TResult, Exception>(sessionFactory, name, sessionType, function);
        }

        /// <summary>
        /// Automatically instrument an async function.
        /// Any exception that is thrown will be logged using ISessionWriter{TException}.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function. This is passed to the success function for additional logging.</typeparam>
        /// <param name="sessionFactory">A factory to create the new session.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="sessionType">The type of session.</param>
        /// <param name="function">The code that should be instrumented.</param>
        /// <returns>The result of the "function".</returns>
        public static Task<TResult> InstrumentAsync<TResult>(this ISessionFactory sessionFactory, string name, SessionType sessionType, Func<ISession, Task<TResult>> function)
        {
            return InstrumentAsync<TResult, Exception>(sessionFactory, name, sessionType, function);
        }

        /// <summary>
        /// Automatically instrument an async function.
        /// Any exception that is thrown will be logged using ISessionWriter{TException}.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function. This is passed to the success function for additional logging.</typeparam>
        /// <typeparam name="TException">The exception type that is expected.</typeparam>
        /// <param name="sessionFactory">A factory to create the new session.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="sessionType">The type of session.</param>
        /// <param name="function">The code that should be instrumented.</param>
        /// <returns>The result of the "function".</returns>
        public static Task<TResult> InstrumentAsync<TResult, TException>(this ISessionFactory sessionFactory, string name, SessionType sessionType, Func<Task<TResult>> function)
            where TException : Exception
        {
            return InstrumentAsync<TResult, TException>(sessionFactory, name, sessionType, _ => function());
        }

        /// <summary>
        /// Automatically instrument an async function.
        /// Any exception that is thrown will be logged using ISessionWriter{TException}.
        /// </summary>fs
        /// <typeparam name="TResult">The return type of the function. This is passed to the success function for additional logging.</typeparam>
        /// <typeparam name="TException">The exception type that is expected.</typeparam>
        /// <param name="sessionFactory">A factory to create the new session.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="sessionType">The type of session.</param>
        /// <param name="function">The code that should be instrumented.</param>
        /// <returns>The result of the "function".</returns>
        public static async Task<TResult> InstrumentAsync<TResult, TException>(this ISessionFactory sessionFactory, string name, SessionType sessionType, Func<ISession, Task<TResult>> function)
            where TException : Exception
        {
            var session = sessionFactory.StartSession(name, sessionType);
            try
            {
                var result = await function(session).ConfigureAwait(false);
                session.Success(result);
                return result;
            }
            catch (Exception ex)
            {
                var expectedException = ex as TException;

                if (expectedException != null)
                {
                    session.Fault(expectedException);
                }
                else
                {
                    session.Fault(ex);
                }

                throw;
            }
        }
    }
}
