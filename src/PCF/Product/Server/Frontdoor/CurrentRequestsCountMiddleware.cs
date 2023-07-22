namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;

    public class CurrentRequestsCountMiddleware : OwinMiddleware
    {
        private static int CurrentRequestCount = 0;

        public static int GetCurrentRequestCount()
        {
            return CurrentRequestCount;
        }

        public CurrentRequestsCountMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            try
            {
                Interlocked.Increment(ref CurrentRequestCount);
                await Next.Invoke(context);
            }
            finally
            {
                Interlocked.Decrement(ref CurrentRequestCount);
            }
        }
    }
}
