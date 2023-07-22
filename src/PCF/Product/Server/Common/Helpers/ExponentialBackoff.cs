namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Helpers
{
    using System;
    using System.Threading.Tasks;

    public class ExponentialBackoff
    {
        private readonly TimeSpan delay, maxDelay, delayStartsWith;
        private int retries, pow;

        /// <summary>
        /// Contrustor of class ExponentialBackoff
        /// </summary>
        /// <param name="delay">A time unit applied every time to get the final result</param>
        /// <param name="maxDelay">Max delay time</param>
        /// <param name="delayStartsWith">A base delay to start with</param>
        public ExponentialBackoff(TimeSpan? delay, TimeSpan? maxDelay, TimeSpan? delayStartsWith = null)
        {
            this.delay = delay ?? throw new ArgumentNullException(nameof(delay));
            this.maxDelay = maxDelay ?? TimeSpan.MaxValue;
            this.delayStartsWith = delayStartsWith ?? TimeSpan.Zero;
            this.retries = 0;
            this.pow = 1;
        }

        public Task Delay()
        {
            this.retries++;
            if (this.retries < 31)
            {
                this.pow <<= 1;
            }

            // divide by 2 for smaller granularity
            var delayInTicks = Math.Min(this.delayStartsWith.Ticks + this.delay.Ticks * (pow - 1) / 2, this.maxDelay.Ticks);

            return Task.Delay(TimeSpan.FromTicks(delayInTicks));
        }

        public void Reset()
        {
            this.retries = 0;
            this.pow = 1;
        }
    }
}
