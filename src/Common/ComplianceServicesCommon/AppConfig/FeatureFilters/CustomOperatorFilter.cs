namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.FeatureManagement;
    using Microsoft.Extensions.Configuration;
    using System.Linq;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Implements IContextualFeatureFilter. Provides a way to evaluate features against
    /// "CustomOperatorFilter" filter.
    /// </summary>
    /// 
    /// File name used to map filter "Name" attribute to class. 
    /// NOTE: Do not change FilterAlias!
    [FilterAlias("CustomOperatorFilter")]
    public class CustomOperatorFilter : IContextualFeatureFilter<ICustomOperatorContext>
    {
        /// <summary>
        ///  There is no way to provide logger to this component through feature Manager
        ///  Adding temporary solution. ( Long term solution will be to implement a LoggerFactory)
        /// </summary>
        static internal PrivacyServices.Common.Azure.ILogger Logger { get; set; }

        /// <summary>
        /// Creates a CustomOperatorFilter, Called internally by FeatureManager framework.
        /// </summary>
        /// <param name="loggerFactory">Logging Factory</param>
        /// Todo, adding logging support. Task 1267243
        public CustomOperatorFilter(ILoggerFactory _) 
        {
        }

        /// <summary>
        /// Compares the Evaluation context against app Context to determine if the feature is enabled as
        /// per the definition of current filter context.
        /// </summary>
        /// <param name="context">Filter definition context.</param>
        /// <param name="appContext">Data to evaluate filter against.</param>
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, ICustomOperatorContext appContext)
        {
            IConfiguration parameters = context.Parameters;
            CustomOperatorFilterSettings settings = parameters.Get<CustomOperatorFilterSettings>();

            if(!CompareFlightEnvironment(settings, appContext))
            {
                return Task.FromResult<bool>(false);
            }
            // If key is not specified in filter setting and appContext have key return false.
            else if (string.IsNullOrEmpty(settings.Key) && !string.IsNullOrEmpty(appContext.Key))
            {
                return Task.FromResult<bool>(false);
            }
            // If key is  specified in filter setting and appContext key do not match
            else if (!string.IsNullOrEmpty(settings.Key) && settings.Key != appContext.Key)
            {
                return Task.FromResult<bool>(false);
            }
            else
            {
                try
                {
                    switch (settings.Operator)
                    {
                        case CustomOperatorNames.OperatorTypeInclude:
                            {
                                return Task.FromResult<bool>(settings.Value.Split(',').Any(arr => appContext.Compare(appContext.Value, arr.Trim())));
                            }
                        case CustomOperatorNames.OperatorTypeExclude:
                            {
                                return Task.FromResult<bool>(!(settings.Value.Split(',').Any(arr => appContext.Compare(appContext.Value, arr.Trim()))));
                            }
                        /// OperatorTypeEquals, OperatorTypeGreaterThan and OperatorTypeLessThan have a single entry in the argument list.
                        case CustomOperatorNames.OperatorTypeEquals:
                            {
                                return Task.FromResult<bool>(settings.Value.Equals(appContext.Value));
                            }
                        /// < and > are supported only for Integer values.
                        case CustomOperatorNames.OperatorTypeGreaterThan:
                            {
                                return Task.FromResult<bool>(Convert.ToInt32(settings.Value) < Convert.ToInt32(appContext.Value));
                            }
                        case CustomOperatorNames.OperatorTypeLessThan:
                            {
                                return Task.FromResult<bool>(Convert.ToInt32(settings.Value) > Convert.ToInt32(appContext.Value));
                            }
                        case CustomOperatorNames.OperatorTypePercentage:
                            {
                                return Task.FromResult<bool>(Convert.ToInt32(settings.Value) >= (RandomHelper.NextDouble() * 100));
                            }
                        default:
                            {
                                throw new ArgumentException($"Unknown operator {settings.Operator}");
                            }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Error(nameof(CustomOperatorFilter), $"Exception {ex} for {settings.Operator}");
                }
            }

            return Task.FromResult<bool>(false);
        }

        /// <summary>
        /// Compare generic properties for a flight. 
        /// EnvironmentInfo is implicitly added to a flight context during feature evaluation.
        /// </summary>
        /// <param name="context">Filter definition context.</param>
        /// <param name="appContext">Data to evaluate filter against.</param>
        /// <returns> True if the Envionment settigns present and match with context,
        /// false otherwise.
        /// </returns>
        private bool CompareFlightEnvironment(CustomOperatorFilterSettings settings, ICustomOperatorContext appContext)
        {
            // Compare Environment first, must be exact match if specified.
            if (!string.IsNullOrEmpty(settings.ServiceName)
                && !settings.ServiceName.Split(',').Any(arr => arr == appContext.ServiceName))
            {
                return false;
            }
            else if (!string.IsNullOrEmpty(settings.EnvironmentName)
                && !settings.EnvironmentName.Split(',').Any(arr => arr == appContext.EnvironmentName))
            {
                return false;
            }
            else if (!string.IsNullOrEmpty(settings.AssemblyVersion)
                && !settings.AssemblyVersion.Split(',').Any(arr => arr == appContext.AssemblyVersion))
            {
                return false;
            }
            else if (!string.IsNullOrEmpty(settings.MachineName)
                && !settings.MachineName.Split(',').Any(arr => arr == appContext.MachineName))
            {
                return false;
            }
            else if (!string.IsNullOrEmpty(settings.Market)
                && !settings.Market.Split(',').Any(arr => arr == appContext.Market))
            {
                return false;
            }
            else if (!string.IsNullOrEmpty(settings.IncomingCallerName)
                && !settings.IncomingCallerName.Split(',').Any(arr => arr == appContext.IncomingCallerName))
            {
                return false;
            }
            else if (!string.IsNullOrEmpty(settings.IncomingOperationName)
                && !settings.IncomingOperationName.Split(',').Any(arr => arr == appContext.IncomingOperationName))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}