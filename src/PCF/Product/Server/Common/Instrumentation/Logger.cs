namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Static logger class.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Gets or sets the current logger.
        /// </summary>
        public static ILogger Instance
        {
            get;
            set;
        }
        
        /// <summary>
        /// Logs a latency-based event that has no return value.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="callback">The callback to instrument.</param>
        public static async Task InstrumentAsync<TEvent>(TEvent @event, Func<TEvent, Task> callback) where TEvent : OperationEvent
        {
            await InstrumentAsync<TEvent, bool>(
                @event,
                async ev =>
                {
                    await callback(ev);
                    return true;
                });
        }

        /// <summary>
        /// Logs a latency-based event that returns a value.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="callback">The callback to instrument.</param>
        public static async Task<TResult> InstrumentAsync<TEvent, TResult>(
            TEvent @event, 
            Func<TEvent, Task<TResult>> callback) where TEvent : OperationEvent
        {
            var instance = Logger.Instance;
            instance?.EnsureCorrelationVector();

            @event.OperationStatus = OperationStatus.Succeeded;
            try
            {
                return await callback(@event);
            }
            catch (CommandFeedException ex)
            {
                @event.OperationStatus = ex.IsExpected ? OperationStatus.ExpectedFailure : OperationStatus.UnexpectedFailure;
                @event.SetException(ex);
                throw;
            }
            catch (Exception ex)
            {
                @event.OperationStatus = OperationStatus.UnexpectedFailure;
                @event.SetException(ex);

                throw;
            }
            finally
            {
                if (instance != null)
                {
                    @event.Log(instance);
                }
            }
        }

        /// <summary>
        /// Instruments a synchronous callback that does not return a value.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="callback">The callback to instrument.</param>
        public static void InstrumentSynchronous<TEvent>(
            TEvent @event,
            Action<TEvent> callback) where TEvent : OperationEvent
        {
            InstrumentSynchronous<TEvent, bool>(
                @event,
                ev =>
                {
                    callback(ev);
                    return true;
                });
        }

        /// <summary>
        /// Instruments a synchronous callback that returns a value.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="callback">The callback to instrument.</param>
        public static TResult InstrumentSynchronous<TEvent, TResult>(
            TEvent @event,
            Func<TEvent, TResult> callback) where TEvent : OperationEvent
        {
            var instance = Logger.Instance;
            instance?.EnsureCorrelationVector();

            @event.OperationStatus = OperationStatus.Succeeded;
            try
            {
                return callback(@event);
            }
            catch (CommandFeedException ex)
            {
                @event.OperationStatus = ex.IsExpected ? OperationStatus.ExpectedFailure : OperationStatus.UnexpectedFailure;
                @event.SetException(ex);
                throw;
            }
            catch (Exception ex)
            {
                @event.OperationStatus = OperationStatus.UnexpectedFailure;
                @event.SetException(ex);

                throw;
            }
            finally
            {
                if (instance != null)
                {
                    @event.Log(instance);
                }
            }
        }
    }
}
