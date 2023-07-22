namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    /// <summary>
    /// Encapsulates the logic for logging session state to the telemetry stream.
    /// </summary>
    /// <typeparam name="TData">The type that the concrete instance knows how to log.</typeparam>
    public interface ISessionWriter<TData>
    {
        /// <summary>
        /// Logs the result and duration of the session.
        /// </summary>
        /// <param name="status">How the state should be classified.</param>
        /// <param name="name">The name of the operation.</param>
        /// <param name="totalMilliseconds">How long it took for the operation to complete.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">The state data that may be logged for debug purposes.</param>
        void WriteDone(SessionStatus status, string name, long totalMilliseconds, string cv, TData data);
    }

    /// <summary>
    /// Extension functions for SessionWriter classes.
    /// </summary>
    public static class SessionWriterExtensions
    {
        /// <summary>
        /// Writes success to the given session writer.
        /// </summary>
        /// <typeparam name="TData">The data type of the associated session writer.</typeparam>
        /// <param name="sessionWriter">The session writer to use for logging.</param>
        /// <param name="name">The name of the session.</param>
        /// <param name="totalMilliseconds">The duration of the session.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">Additional metadata.</param>
        public static void WriteSuccess<TData>(this ISessionWriter<TData> sessionWriter, string name, long totalMilliseconds, string cv, TData data)
        {
            sessionWriter.WriteDone(SessionStatus.Success, name, totalMilliseconds, cv, data);
        }

        /// <summary>
        /// Writes error to the given session writer.
        /// </summary>
        /// <typeparam name="TData">The data type of the associated session writer.</typeparam>
        /// <param name="sessionWriter">The session writer to use for logging.</param>
        /// <param name="name">The name of the session.</param>
        /// <param name="totalMilliseconds">The duration of the session.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">Additional metadata.</param>
        public static void WriteError<TData>(this ISessionWriter<TData> sessionWriter, string name, long totalMilliseconds, string cv, TData data)
        {
            sessionWriter.WriteDone(SessionStatus.Error, name, totalMilliseconds, cv, data);
        }

        /// <summary>
        /// Writes fault to the given session writer.
        /// </summary>
        /// <typeparam name="TData">The data type of the associated session writer.</typeparam>
        /// <param name="sessionWriter">The session writer to use for logging.</param>
        /// <param name="name">The name of the session.</param>
        /// <param name="totalMilliseconds">The duration of the session.</param>
        /// <param name="cv">The snapped CV value when the session was started.</param>
        /// <param name="data">Additional metadata.</param>
        public static void WriteFault<TData>(this ISessionWriter<TData> sessionWriter, string name, long totalMilliseconds, string cv, TData data)
        {
            sessionWriter.WriteDone(SessionStatus.Fault, name, totalMilliseconds, cv, data);
        }
    }
}
