namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;

    /// <summary>
    /// Defines the set of status values for a session.
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>
        /// The session was successful.
        /// </summary>
        Success,

        /// <summary>
        /// The session had an expected failure due to user error (or the equivalent).
        /// </summary>
        Error,

        /// <summary>
        /// The session has an unexpected failure due to an exception (or the equivalent).
        /// </summary>
        Fault
    }

    /// <summary>
    /// A session object encapsulates an operation.
    /// When the object is created, it must begin a timer.
    /// When the Done functions are called, the timer must cease.
    /// At that time, an event is logged using an ISessionWriter.
    /// The event will include the state of the operation (success/failure).
    /// </summary>
    public interface ISession
    {
        /// <summary>
        /// Gets the snapped correlation vector value for this session instance.
        /// </summary>
        string CorrelationVector { get; }

        /// <summary>
        /// Completes the session and logs the status.
        /// </summary>
        /// <param name="status">The session status.</param>
        void Done(SessionStatus status);

        /// <summary>
        /// Completes the session, logs the status and additional metadata.
        /// </summary>
        /// <typeparam name="TResult">The type that corresponds to a matching registered ISessionWriter.</typeparam>
        /// <param name="status">The session status.</param>
        /// <param name="result">Additional metadata.</param>
        void Done<TResult>(SessionStatus status, TResult result);

        /// <summary>
        /// Changes the name of the session. This is necessary in places where the name is not known until after some code has started to execute.
        /// </summary>
        /// <param name="name">The new value for the session name.</param>
        void SetName(string name);

        /// <summary>
        /// Stops the session and returns the elapsed duration.
        /// </summary>
        /// <returns>The elapsed duration.</returns>
        long Stop();
    }

    /// <summary>
    /// Extension functions for the session classes.
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// Completes the session and logs success.
        /// </summary>
        /// <param name="session">The session object.</param>
        public static void Success(this ISession session)
        {
            session.Done(SessionStatus.Success);
        }

        /// <summary>
        /// Completes the session, logs success and additional metadata.
        /// </summary>
        /// <typeparam name="TResult">The type that corresponds to a matching registered ISessionWriter.</typeparam>
        /// <param name="session">The session object.</param>
        /// <param name="result">Additional metadata.</param>
        public static void Success<TResult>(this ISession session, TResult result)
        {
            session.Done(SessionStatus.Success, result);
        }

        /// <summary>
        /// Completes the session and logs error.
        /// </summary>
        /// <param name="session">The session object.</param>
        public static void Error(this ISession session)
        {
            session.Done(SessionStatus.Error);
        }

        /// <summary>
        /// Completes the session, logs error and additional metadata.
        /// </summary>
        /// <typeparam name="TResult">The type that corresponds to a matching registered ISessionWriter.</typeparam>
        /// <param name="session">The session object.</param>
        /// <param name="result">Additional metadata.</param>
        public static void Error<TResult>(this ISession session, TResult result)
        {
            session.Done(SessionStatus.Error, result);
        }

        /// <summary>
        /// Completes the session and logs fault.
        /// </summary>
        /// <param name="session">The session object.</param>
        public static void Fault(this ISession session)
        {
            session.Done(SessionStatus.Fault);
        }

        /// <summary>
        /// Completes the session, logs fault and additional exception data.
        /// </summary>
        /// <param name="session">The session object.</param>
        /// <param name="exception">The exception data.</param>
        public static void Fault(this ISession session, Exception exception)
        {
            session.Done(SessionStatus.Fault, exception);
        }

        /// <summary>
        /// Completes the session, logs fault and additional metadata.
        /// </summary>
        /// <typeparam name="TResult">The type that corresponds to a matching registered ISessionWriter.</typeparam>
        /// <param name="session">The session object.</param>
        /// <param name="result">Additional metadata.</param>
        public static void Fault<TResult>(this ISession session, TResult result)
        {
            session.Done(SessionStatus.Fault, result);
        }
    }
}
