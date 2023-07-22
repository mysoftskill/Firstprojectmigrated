
//------------------------------------------------------------------------------
// This code was generated by a tool.
//
//   Tool : Bond Compiler 0.10.1.0
//   File : QosSchemas_types.cs
//
// Changes to this file may cause incorrect behavior and will be lost when
// the code is regenerated.
// <auto-generated />
//------------------------------------------------------------------------------


// suppress "Missing XML comment for publicly visible type or member"
#pragma warning disable 1591


#region ReSharper warnings
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantUsingDirective
#endregion

namespace Microsoft.PrivacyServices.UX.Monitoring.Events
{
    using System.Collections.Generic;

    [global::Bond.Attribute("Description", "Incoming service event")]
    [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class IncomingServiceEvent
        : global::Microsoft.Osgs.Infra.Monitoring.BaseIncomingServiceEvent
    {
        [global::Bond.Attribute("Description", "The flight behaviors.")]
        [global::Bond.Id(20)]
        public string flightBehaviors { get; set; }

        [global::Bond.Attribute("Description", "The original referrer for BI. referrer header is always login.live.com so this logs the original referrer.")]
        [global::Bond.Id(30)]
        public string originalReferrer { get; set; }

        [global::Bond.Attribute("Description", "The mvc area name for the request.")]
        [global::Bond.Id(40)]
        public string mvcAreaName { get; set; }

        [global::Bond.Attribute("Description", "The mvc controller name for the request.")]
        [global::Bond.Id(50)]
        public string mvcControllerName { get; set; }

        [global::Bond.Attribute("Description", "Identifies the current host (web|xbox|windows).")]
        [global::Bond.Id(60)]
        public string hostId { get; set; }

        public IncomingServiceEvent()
            : this("Microsoft.PrivacyServices.UX.Monitoring.Events.IncomingServiceEvent", "IncomingServiceEvent")
        {}

        protected IncomingServiceEvent(string fullName, string name)
        {
            flightBehaviors = "";
            originalReferrer = "";
            mvcAreaName = "";
            mvcControllerName = "";
            hostId = "";
        }
    }

    [global::Bond.Attribute("Description", "Outgoing service event")]
    [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class OutgoingServiceEvent
        : global::Microsoft.Osgs.Infra.Monitoring.BaseOutgoingServiceEvent
    {
        [global::Bond.Attribute("Description", "The flight behaviors.")]
        [global::Bond.Id(20)]
        public string flightBehaviors { get; set; }

        [global::Bond.Attribute("Description", "The original referrer for BI. referrer header is always login.live.com so this logs the original referrer.")]
        [global::Bond.Id(30)]
        public string originalReferrer { get; set; }

        [global::Bond.Attribute("Description", "The mvc area name for the request.")]
        [global::Bond.Id(40)]
        public string mvcAreaName { get; set; }

        [global::Bond.Attribute("Description", "The mvc controller name for the request.")]
        [global::Bond.Id(50)]
        public string mvcControllerName { get; set; }

        [global::Bond.Attribute("Description", "Identifies the current host (web|xbox|windows).")]
        [global::Bond.Id(60)]
        public string hostId { get; set; }

        [global::Bond.Attribute("Description", "When verbose logging is enabled contains the response content of outgoing calls.")]
        [global::Bond.Id(70)]
        public string responseContent { get; set; }

        public OutgoingServiceEvent()
            : this("Microsoft.PrivacyServices.UX.Monitoring.Events.OutgoingServiceEvent", "OutgoingServiceEvent")
        {}

        protected OutgoingServiceEvent(string fullName, string name)
        {
            flightBehaviors = "";
            originalReferrer = "";
            mvcAreaName = "";
            mvcControllerName = "";
            hostId = "";
            responseContent = "";
        }
    }

    [global::Bond.Attribute("Description", "Debug trace event")]
    [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class TraceEvent
        : global::Microsoft.Telemetry.Data<global::Microsoft.Telemetry.Base>
    {
        [global::Bond.Attribute("Description", "Trace message.")]
        [global::Bond.Id(10)]
        public string message { get; set; }

        public TraceEvent()
            : this("Microsoft.PrivacyServices.UX.Monitoring.Events.TraceEvent", "TraceEvent")
        {}

        protected TraceEvent(string fullName, string name)
        {
            message = "";
        }
    }

    [global::Bond.Attribute("Description", "Outgoing service event for Compass CMS")]
    [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class CompassOutgoingServiceEvent
        : global::Microsoft.Osgs.Infra.Monitoring.BaseOutgoingServiceEvent
    {
        [global::Bond.Attribute("Description", "Compass endpoint.")]
        [global::Bond.Id(10)]
        public string Endpoint { get; set; }

        [global::Bond.Attribute("Description", "Path to a content item.")]
        [global::Bond.Id(20)]
        public string ContentPath { get; set; }

        [global::Bond.Attribute("Description", "Locale.")]
        [global::Bond.Id(30)]
        public string Locale { get; set; }

        [global::Bond.Attribute("Description", "Cache key generated for the item.")]
        [global::Bond.Id(40)]
        public string CacheKey { get; set; }

        [global::Bond.Attribute("Description", "Indicates whether the cache was missed.")]
        [global::Bond.Id(50)]
        public bool IsCacheMiss { get; set; }

        public CompassOutgoingServiceEvent()
            : this("Microsoft.PrivacyServices.UX.Monitoring.Events.CompassOutgoingServiceEvent", "CompassOutgoingServiceEvent")
        {}

        protected CompassOutgoingServiceEvent(string fullName, string name)
        {
            Endpoint = "";
            ContentPath = "";
            Locale = "";
            CacheKey = "";
        }
    }

    [global::Bond.Attribute("Description", "Outgoing service event for cache")]
    [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class CacheOutgoingServiceEvent
        : global::Microsoft.Osgs.Infra.Monitoring.BaseOutgoingServiceEvent
    {
        [global::Bond.Attribute("Description", "Cache key")]
        [global::Bond.Id(10)]
        public string Key { get; set; }

        public CacheOutgoingServiceEvent()
            : this("Microsoft.PrivacyServices.UX.Monitoring.Events.CacheOutgoingServiceEvent", "CacheOutgoingServiceEvent")
        {}

        protected CacheOutgoingServiceEvent(string fullName, string name)
        {
            Key = "";
        }
    }
} // Microsoft.PrivacyServices.UX.Monitoring.Events
