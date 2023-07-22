namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Owin;

    /// <summary>
    /// An OWIN middleware class that prints to console any unhandled exceptions.
    /// This is particularly useful to help diagnose dependency resolution or initialization failures.
    /// </summary>
    public class DebugMiddleWare : OwinMiddleware
    {
        /// <summary>
        /// The next middleware in the stack.
        /// </summary>
        private readonly OwinMiddleware next;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugMiddleWare" /> class.
        /// </summary>
        /// <param name="next">The next middleware in the stack.</param>
        public DebugMiddleWare(OwinMiddleware next) : base(next)
        {
            this.next = next;
        }

        /// <summary>
        /// Invokes the middleware behavior.
        /// Catches any exceptions and logs them to the console.
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        /// <returns>A task.</returns>
        public override async Task Invoke(IOwinContext context)
        {
            try
            {
                await this.next.Invoke(context).ConfigureAwait(false);                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}
