namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Reflection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// A concrete implementation of the ILogger interface.
    /// It wraps an existing implementation of the ILogger interface with additional logging to the console.
    /// This should only be used in debug versions of the service.
    /// </summary>
    /// <typeparam name="TBase">The base type for instrumentation events.</typeparam>
    public class ConsoleLog<TBase> : ILogger<TBase>
    {
        /// <summary>
        /// A separator to put between events.
        /// </summary>
        internal const string Separator = "------------------------------------------------------------------------";

        /// <summary>
        /// JSON serialization settings. Designed to minimize the payload as much as possible.
        /// </summary>
        internal static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ContractResolver = new SubstituteEmptyWithNullStringContractResolver()
        };

        /// <summary>
        /// The original console color.
        /// </summary>
        private readonly ConsoleColor defaultColor;

        /// <summary>
        /// The default log instance.
        /// </summary>
        private readonly ILogger<TBase> regularLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLog{TBase}" /> class.
        /// </summary>
        /// <param name="regularLog">The default log implementation.</param>
        /// <param name="defaultColor">The default color for writing to the console.</param>
        public ConsoleLog(ILogger<TBase> regularLog, ConsoleColor defaultColor)
        {
            this.regularLog = regularLog;
            this.ConsoleWriter = Instrumentation.ConsoleWriter.Instance;
            this.defaultColor = defaultColor;
        }

        /// <summary>
        /// Gets or sets the ConsoleWriter. This is a test hook.
        /// </summary>
        internal IConsoleWriter ConsoleWriter { get; set; }

        /// <summary>
        /// First logs the given data to the default log, and then logs it to the console.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <param name="properties">The session properties.</param>
        /// <param name="event">The event data.</param>
        /// <param name="level">The event level.</param>
        /// <param name="options">The event options.</param>
        /// <param name="cv">An override to the cv value available on the session properties.</param>
        public void Write<T>(SessionProperties properties, T @event, EventLevel level, EventOptions options, string cv = null) where T : TBase
        {
            this.regularLog.Write(properties, @event, level, options, cv);

            var debugData = new Data<T> { Event = @event, Properties = properties, CV = cv ?? properties.CV.Get() };
            var data = JsonConvert.SerializeObject(debugData, Formatting.Indented, SerializationSettings);

            this.ConsoleWriter.WriteLine(data, this.GetConsoleColor(level));
            this.ConsoleWriter.WriteLine(Separator, this.defaultColor);
        }

        /// <summary>
        /// Converts the given event level into a corresponding console color.
        /// </summary>
        /// <param name="level">The event level.</param>
        /// <returns>An appropriate console color.</returns>
        private ConsoleColor GetConsoleColor(EventLevel level)
        {
            switch (level)
            {
                case EventLevel.Critical:
                case EventLevel.Error:
                    return ConsoleColor.Red;
                case EventLevel.Warning:
                    return ConsoleColor.Yellow;
                case EventLevel.Verbose:
                    return ConsoleColor.DarkGray;
                case EventLevel.LogAlways:
                case EventLevel.Informational:
                default:
                    return this.defaultColor;
            }
        }

        /// <summary>
        /// Encapsulates the event and session properties into a single data type
        /// so that it can be easily serialized to the console.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        internal class Data<T>
        {
            /// <summary>
            /// Gets or sets the event data.
            /// </summary>
            public T Event { get; set; }

            /// <summary>
            /// Gets or sets the session properties.
            /// </summary>
            public SessionProperties Properties { get; set; }

            /// <summary>
            /// Gets or sets the CV override.
            /// </summary>
            public string CV { get; set; }
        }

        /// <summary>
        /// A resolver to convert empty string to null. This is so that empty strings are not logged to console.
        /// </summary>        
        private sealed class SubstituteEmptyWithNullStringContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (property.PropertyType == typeof(string))
                {
                    // Wrap value provider supplied by Json.NET.
                    property.ValueProvider = new EmptyToNullStringValueProvider(property.ValueProvider);
                }

                return property;
            }

            private sealed class EmptyToNullStringValueProvider : IValueProvider
            {
                private readonly IValueProvider provider;

                public EmptyToNullStringValueProvider(IValueProvider provider)
                {
                    if (provider == null)
                    {
                        throw new ArgumentNullException("provider");
                    }

                    this.provider = provider;
                }

                public object GetValue(object target)
                {
                    var value = this.provider.GetValue(target) as string;
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }

                public void SetValue(object target, object value)
                {
                    this.provider.SetValue(target, value);
                }
            }
        }
    }
}
