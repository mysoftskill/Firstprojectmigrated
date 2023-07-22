namespace PCF.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    public sealed class FlightDisabled :IFlightContext, IDisposable
    {
        private readonly string featureName;
        private IFlightContext originalContext;

        public FlightDisabled(string name)
        {
            this.featureName = name;
            this.originalContext = FlightingUtilities.Instance;
            FlightingUtilities.Instance = this;
        }

        public void Dispose()
        {
        }

        public bool IsEnabled<TContext>(string flightName, IEnumerable<TContext> parameters)
        {
            if (flightName == this.featureName)
            {
                return false;
            }

            return this.originalContext.IsEnabled(flightName, parameters);
        }

        public bool IsEnabled(string flightName, bool useCached = true)
        {
            if (flightName == this.featureName)
            {
                return false;
            }

            return this.originalContext.IsEnabled(flightName);
        }

        public bool IsEnabled<TContext>(string flightName, TContext context, bool useCached = true)
        {
            if (flightName == this.featureName)
            {
                return false;
            }

            return this.originalContext.IsEnabled(flightName, context);
        }

        public bool IsEnabledAll<TContext>(string flightName, IEnumerable<TContext> context)
        {
            if (flightName == this.featureName)
            {
                return false;
            }

            return this.originalContext.IsEnabledAll(flightName, context);
        }

        public bool IsEnabledAny<TContext>(string flightName, IEnumerable<TContext> context)
        {
            if (flightName == this.featureName)
            {
                return false;
            }

            return this.originalContext.IsEnabledAny(flightName, context);
        }
    }
}
