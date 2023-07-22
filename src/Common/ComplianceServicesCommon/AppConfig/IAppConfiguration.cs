
namespace Microsoft.Azure.ComplianceServices.Common
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IAppConfiguration
    {
        /// /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="useCached">boolean indicating whether cache should be checked first for the feature.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        ValueTask<bool> IsFeatureFlagEnabledAsync(string feature, bool useCached = true);

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="context">A list of context providing information that can be used to evaluate whether a feature
        //     should be on or off.</param>
        /// <param name="useCached">boolean indicating whether cache should be checked first for the feature.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        ValueTask<bool> IsFeatureFlagEnabledAnyAsync<TContext>(string feature, IEnumerable<TContext> context, bool useCached = true);

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="context">Feature is evaluated against all of the context and determined to be enabled if all the context 
        /// <param name="useCached">boolean indicating whether cache should be checked first for the feature.</param>
        /// Evaluation returns true</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        ValueTask<bool> IsFeatureFlagEnabledAllAsync<TContext>(string feature, IEnumerable<TContext> context, bool useCached = true);

        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="context">A context providing information that can be used to evaluate whether a feature
        ///     should be on or off.</param>
        /// <param name="useCached">boolean indicating whether cache should be checked first for the feature.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        ValueTask<bool> IsFeatureFlagEnabledAsync<TContext>(string feature, TContext context, bool useCached = true);

        /// <summary>
        /// returns all enabled features names applicable for the provided context.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="feature">The name of the feature to check.</param>
        /// <param name="context">A context providing information that can be used to evaluate whether a feature
        //     should be on or off.</param>
        /// <returns>List of feature names.</returns>
        Task<IEnumerable<string>> GetEnabledFeaturesAsync<TContext>(TContext context);

        /// <summary>
        /// Extracts the value with the specified key and converts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="configurationName">The key of the configuration section's value to convert.</param>
        /// <returns>The converted value or default(T) if no value is found.</returns>
        T GetConfigValue<T>(string configurationName);

        /// <summary>
        /// Extracts the value with the specified key and converts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="configurationName">The key of the configuration section's value to convert.</param>
        /// <param name="defaultValue">The default value to use if no value is found.</param>
        /// <returns>The converted value.</returns>
        T GetConfigValue<T>(string configurationName, T defaultValue);

        /// <summary>
        /// Extracts the array value with the specified key and converts it to the specified type.
        /// For example, when value of config "key" is set to ["AF35E434-D887-43FA-A970-B6CB811C5327", "B635026A-0CDD-4B53-9CCE-B863DCC5F76A"], 
        /// and the content type is set to application/json 
        /// GetConfigValues<Guid>("key") will return Guid[2]
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="configurationName">The key of the configuration section's value to convert.</param>
        /// <returns>The converted value or null if no value is found.</returns>
        T[] GetConfigValues<T>(string configurationName);       
    }
}
