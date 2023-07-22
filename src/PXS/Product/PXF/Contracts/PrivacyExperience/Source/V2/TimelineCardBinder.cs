// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    ///     This class exists to allow TypeNameHandling.Auto on 'Items' in <see cref="PagedResponse{TimelineCard}" />. This mitigates the attack vector
    ///     of sending dangerous types to be serialized, by having aAllowed List of types we understand described in this class.
    /// </summary>
    public class TimelineCardBinder : ISerializationBinder
    {
        private const string AssemblyName = "t";

        private static readonly Dictionary<string, Type> timelineCardTypes;

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (!typeof(TimelineCard).IsAssignableFrom(serializedType))
                throw new NotSupportedException();
            assemblyName = AssemblyName;
            typeName = serializedType.Name;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            if (assemblyName != AssemblyName)
                throw new NotSupportedException();
            if (timelineCardTypes.TryGetValue(typeName, out Type type))
                return type;
            throw new NotSupportedException();
        }

        static TimelineCardBinder()
        {
            timelineCardTypes = typeof(TimelineCard).Assembly.GetTypes()
                .Where(t => typeof(TimelineCard).IsAssignableFrom(t))
                .ToDictionary(t => t.Name, t => t);
        }
    }
}
