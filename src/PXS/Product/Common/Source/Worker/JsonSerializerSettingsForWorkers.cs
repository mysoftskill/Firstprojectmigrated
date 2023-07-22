// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Worker
{
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json;

    /// <summary>
    ///     A helper class to centralize Json serializer setting with CodeQL suppression.
    /// </summary>
    public class JsonSerializerSettingsForWorkers
    {
        /// <summary>
        ///     Set up Json serializer setting using TypeNameHandling.All enumeration value.
        ///     Also suppress the CodeQL alert.
        /// </summary>
        /// <param name="container">
        ///     Unity container used by dependency injection framework.
        /// </param>
        public static void SetupJsonSerializerSettings(IUnityContainer container)
        {
            // TypeNameHandling.All is unsafe, but PXS workers won't get user inputs that specify which types 
            // get created. Also since all workers use this Json serializer settings, it's risky to switch to
            // TypeNameHandling.None mode without thorough testing of all workers. We can update workers to use
            // the safer serialization method one worker a time later.
            var serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };    // lgtm[cs/unsafe-type-name-handling]
            container.RegisterInstance(serializerSettings);
        }
    }
}
