namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;
    using System.Linq;

    /// <summary>
    /// Provides helper functions for interacting with the InstrumentAttributes class.
    /// </summary>
    public static class InstrumentAttributeModule
    {
        /// <summary>
        /// Determines if the given method on the given class has an InstrumentationAttribute.
        /// If it does, it extracts the attribute and its corresponding property information.
        /// </summary>
        /// <param name="classType">The type of the class.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <returns>The attribute object with properties populated. Or null if not found.</returns>
        public static InstrumentAttribute GetAttribute(Type classType, string methodName)
        {
            var methodInfo = classType.GetMethod(methodName);
            var attributeData = methodInfo.CustomAttributes.SingleOrDefault(a => a.AttributeType.BaseType == typeof(InstrumentAttribute));
            
            InstrumentAttribute returnValue = null;

            if (attributeData != null)
            {
                if (attributeData.AttributeType == typeof(OutgoingAttribute))
                {
                    returnValue = new OutgoingAttribute();
                }
                else if (attributeData.AttributeType == typeof(IncomingAttribute))
                {
                    returnValue = new IncomingAttribute();
                }
                else if (attributeData.AttributeType == typeof(InternalAttribute))
                {
                    returnValue = new InternalAttribute();
                }

                returnValue.Name = attributeData.NamedArguments.SingleOrDefault(a => a.MemberName == "Name").TypedValue.Value as string;
            }

            return returnValue;
        }
    }    
}