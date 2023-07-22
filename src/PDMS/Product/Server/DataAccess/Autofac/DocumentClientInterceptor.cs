namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Castle.DynamicProxy;

    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// An interceptor that is designed specifically for wrapping the IDocumentClient.    
    /// </summary>
    public class DocumentClientInterceptor : IInterceptor
    {
        private readonly string partnerName = "DocumentDB";
        private readonly ISessionFactory sessionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientInterceptor"/> class.
        /// </summary>
        /// <param name="sessionFactory">The session factory used for instrumenting calls.</param>
        public DocumentClientInterceptor(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
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
                var apiName = $"{this.partnerName}.{invocation.Method.Name}";

                var targetUriObj = invocation.Arguments.FirstOrDefault(o => o is Uri);

                if (targetUriObj == null)
                {
                    targetUriObj = invocation.Arguments.FirstOrDefault(o => o is string) ?? string.Empty;
                }

                var targetUri = targetUriObj.ToString();

                var session = this.sessionFactory.StartSession(apiName, SessionType.Outgoing);

                invocation.Proceed();

                invocation.ReturnValue = InterceptAsync((dynamic)invocation.ReturnValue, session, targetUri);
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
        /// <param name="targetUri">The uri of the call.</param>
        /// <returns>The original result.</returns>
        private static async Task<T> InterceptAsync<T>(Task<T> task, ISession session, string targetUri)
        {
            try
            {
                T result = await task.ConfigureAwait(false);
                session.Success(Tuple.Create(targetUri, result));
                return result;
            }
            catch (DocumentClientException exn)
            {
                session.Error(Tuple.Create(targetUri, exn));
                throw;
            }
        }
    }
}