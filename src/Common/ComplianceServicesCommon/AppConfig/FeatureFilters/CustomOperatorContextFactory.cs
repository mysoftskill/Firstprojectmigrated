namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using System.Text;

    /// <summary>
    /// A helper class to create a CustomOperatorContext instance based input
    /// type provided.
    /// </summary>
    public static class CustomOperatorContextFactory
    {
        public class CustomOperatorContext : ICustomOperatorContext
        {
            public object Value { get; set; }

            public string Key { get; set; }

            public Compare Compare { get; set; }

            public string ServiceName { get; set; }

            public string EnvironmentName { get; set; }

            public string MachineName { get; set; }

            public string AssemblyVersion { get; set; }

            public string IncomingOperationName { get; set; }

            public string IncomingCallerName { get; set; }

            public string Market { get; set; }

            public void IntializeEnvironmentProperties(string serviceName, string machineName, string environmentName,
                string assemblyVersion, string opName, string callerName)
            {
                ServiceName = serviceName;
                MachineName = machineName;
                EnvironmentName = environmentName;
                AssemblyVersion = assemblyVersion;
                IncomingCallerName = callerName;
                IncomingOperationName = opName;
            }


            public override string ToString()
            {
                StringBuilder result = new StringBuilder((Value ?? "").ToString());
                if(!string.IsNullOrEmpty(Key))
                {
                    result.Append("_");
                    result.Append(Key);
                }
                if (!string.IsNullOrEmpty(IncomingOperationName))
                {
                    result.Append("_");
                    result.Append(IncomingOperationName);
                }
                if (!string.IsNullOrEmpty(IncomingCallerName))
                {
                    result.Append("_");
                    result.Append(IncomingCallerName);
                }
                // Everything else will remain same for a given environment
                return result.ToString();
            }
        }

        public static CustomOperatorContext CreateDefaultStringComparisonContext(string val)
        {
            return CreateDefaultStringComparisonContextWithKeyValue(null, val);
        }

        public static CustomOperatorContext CreateDefaultStringComparisonContextWithKeyValue(string key, string val)
        {
            return new CustomOperatorContext
            {
                Value = val,
                Key = key,
                Compare = (object a, object b) => ((string)a).Equals((string)b, StringComparison.OrdinalIgnoreCase)
            };
        }

        public static CustomOperatorContext CreateDefaultIntValueComparisonContext(int val)
        {
            return CreateDefaultIntComparisonContextWithKeyValue(null, val);
        }

        public static CustomOperatorContext CreateDefaultIntComparisonContextWithKeyValue(string key, int val)
        {
            return new CustomOperatorContext
            {
                Value = val,
                Key = key,
                Compare = (object a, object b) => Convert.ToInt32(a) == Convert.ToInt32(b)
            };
        }
    }
}