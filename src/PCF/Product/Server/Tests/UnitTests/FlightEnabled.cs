namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    public sealed class FlightEnabled : IFlightContext, IDisposable
    {
        private List<string> flightNames = new List<string>();
        private IFlightContext originalContext;

        public FlightEnabled(string name)
        {
            this.flightNames.Add(name);
            this.originalContext = FlightingUtilities.Instance;
            FlightingUtilities.Instance = this;
        }

        public FlightEnabled(string[] flightNames)
        {
            this.flightNames.AddRange(flightNames);
            this.originalContext = FlightingUtilities.Instance;
            FlightingUtilities.Instance = this;
        }

        public void Dispose()
        {
            FlightingUtilities.Instance = this.originalContext;
        }

        public bool IsEnabled<TContext>(string flightName, IEnumerable<TContext> parameters)
        {
            return this.flightNames.Contains(flightName) || this.originalContext.IsEnabled(flightName, parameters);
        }

        public bool IsEnabled(string flightName, bool useCached = true)
        {
            if (this.flightNames.Contains(flightName))
            {
                return true;
            }

            return this.originalContext.IsEnabled(flightName);
        }

        public bool IsEnabled<TContext>(string flightName, TContext context, bool useCached = true)
        {
            if (this.flightNames.Contains(flightName))
            {
                return true;
            }

            return this.originalContext.IsEnabled(flightName, context);
        }

        public bool IsEnabledAll<TContext>(string flightName, IEnumerable<TContext> context)
        {
            if (this.flightNames.Contains(flightName))
            {
                return true;
            }

            return this.originalContext.IsEnabledAll(flightName, context);
        }

        public bool IsEnabledAny<TContext>(string flightName, IEnumerable<TContext> context)
        {
            if (this.flightNames.Contains(flightName))
            {
                return true;
            }

            return this.originalContext.IsEnabledAll(flightName, context);
        }
    }
}
