using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/*
 * This is a local clone of AMC's Product\WebRole\Source\Core\Cms\SimpleMemoryCacheClientException.cs.
 * Keep changes minimal, or backport them to AMC.
 */

namespace Microsoft.PrivacyServices.UX.Core.Cache
{
    /// <summary>
    /// Occurs when <see cref="SimpleMemoryCacheClient"/> fails to perform operation.
    /// </summary>
    [Serializable]
    public class SimpleMemoryCacheClientException : Exception
    {
        public SimpleMemoryCacheClientException()
        {
        }
        public SimpleMemoryCacheClientException(string message) : base(message) { }
        public SimpleMemoryCacheClientException(string message, Exception inner) : base(message, inner) { }
        protected SimpleMemoryCacheClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
