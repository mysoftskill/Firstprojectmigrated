using System.Diagnostics;
using System.Text;
using Microsoft.CommonSchema.Services.Logging;
using Microsoft.PrivacyServices.UX.Monitoring.Events;

namespace Microsoft.PrivacyServices.UX.Core.Logging
{
    /// <summary>
    /// Logs trace event using SLL.
    /// </summary>
    public sealed class SllTraceListener : TraceListener
    {
        #region TraceListener Members

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            TraceEvent(eventCache, source, eventType, id, string.Empty);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string message)
        {
            if (null != Filter && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                return;
            }

            var eventPayload = new StringBuilder(message.Length + 128);
            eventPayload.AppendFormat("{0} {1}: {2} : ", source, eventType, id);
            eventPayload.Append(message);

            WriteImpl(eventPayload.ToString());
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string format, params object[] args)
        {
            TraceEvent(eventCache, source, eventType, id, string.Format(format, args));
        }

        public override void Write(string message)
        {
            //  Do nothing - consumers call TraceEvent.
        }

        public override void WriteLine(string message)
        {
            //  Do nothing - consumers call TraceEvent.
        }

        #endregion

        private void WriteImpl(string message)
        {
            var @event = new TraceEvent()
            {
                message = message
            };

            //  NOTE: LogAlways is by design. If you don't want something to be logged, adjust log level in app.config.
            @event.LogAlways();
        }
    }
}
