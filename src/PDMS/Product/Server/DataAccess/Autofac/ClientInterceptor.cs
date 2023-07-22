namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac
{
    using System.Threading.Tasks;

    using Castle.DynamicProxy;

    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// An interceptor that is designed specifically for wrapping the clients.    
    /// </summary>
    public class ClientInterceptor : IInterceptor
    {
        private readonly string partnerName;
        private readonly ISessionFactory sessionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientInterceptor"/> class.
        /// </summary>
        /// <param name="sessionFactory">The session factory used for instrumenting calls.</param>
        /// <param name="clientName">The name to display as the out bound service.</param>
        public ClientInterceptor(ISessionFactory sessionFactory, string clientName)
        {
            this.sessionFactory = sessionFactory;
            this.partnerName = clientName;
        }

        /// <summary>
        /// Decorates the asynchronous methods with instrumentation.
        /// We should only use async versions of the DocumentClient methods.
        /// </summary>
        /// <param name="invocation">The method to wrap.</param>
        public void Intercept(IInvocation invocation)
        {
            var method = invocation.MethodInvocationTarget;

            if (typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                var session = this.sessionFactory.StartSession(this.partnerName, SessionType.Outgoing);

                invocation.Proceed();

                invocation.ReturnValue = InterceptAsync((dynamic)invocation.ReturnValue, session);
            }
            else
            {
                invocation.Proceed();
            }
        }

        /// <summary>
        /// Wraps an invocation to a Task{T} response. Currently all async IDocumentClient methods return Task{T}.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="task">The task response.</param>
        /// <param name="session">The session object.</param>
        /// <returns>The original result.</returns>
        private static async Task<T> InterceptAsync<T>(Task<T> task, ISession session)
        {
            try
            {
                T result = await task.ConfigureAwait(false);
                var value = result as IHttpResult;

                if (value != null)
                {
                    session.Success(value);
                }

                return result;
            }
            catch (BaseException exn)
            {
                session.Done((SessionStatus)int.MaxValue, exn); // The status is ignored by the writer.
                throw;
            }
        }
    }
}