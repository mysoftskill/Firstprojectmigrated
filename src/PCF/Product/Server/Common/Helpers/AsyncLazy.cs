namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    /// <summary>
    /// A class that on-demand loads an instance of type T.
    /// </summary>
    public sealed class AsyncLazy<T>
    {
        private readonly Lazy<Task<T>> item;

        /// <summary>
        /// Creates a new AsyncLazy instance.
        /// </summary>
        public AsyncLazy(Func<Task<T>> callback)
        {
            this.item = new Lazy<Task<T>>(callback);
        }

        /// <summary>
        /// Magic compiler method; Provides the ability to "await" this object.
        /// </summary>
        public TaskAwaiter<T> GetAwaiter() => this.item.Value.GetAwaiter();
    }
}
