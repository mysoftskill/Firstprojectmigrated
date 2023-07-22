namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    
    /// <summary>
    /// Defines an attribute that may be added to any method.
    /// When added, that method will be automatically instrumented 
    /// if registered for interception by a DI framework.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class has no associated code.")]
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class InstrumentAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name to use for instrumentation.
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Identifies a method for automatic Incoming instrumentation.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class has no associated code.")]
    public class IncomingAttribute : InstrumentAttribute
    {
    }

    /// <summary>
    /// Identifies a method for automatic Internal instrumentation.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class has no associated code.")]
    public class InternalAttribute : InstrumentAttribute
    {
    }

    /// <summary>
    /// Identifies a method for automatic Outgoing instrumentation.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Class has no associated code.")]
    public class OutgoingAttribute : InstrumentAttribute
    {
    }
}
