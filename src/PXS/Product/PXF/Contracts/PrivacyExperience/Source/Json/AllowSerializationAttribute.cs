// <copyright file="PrivacyJsonTypeNameHintAttribute.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.Json
{
    using System;

    /// <summary>
    ///     Decorates a JSON-serializable class with its type code so it can be serialized and deserialized based on the polymorphic type.
    ///     Yes, JSON.NET supports something like this, but there are serious security issues with its approach.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class AllowSerializationAttribute : Attribute
    {
    }
}
