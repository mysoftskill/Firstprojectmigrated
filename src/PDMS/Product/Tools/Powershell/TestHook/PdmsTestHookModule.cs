namespace Microsoft.PrivacyServices.DataManagement.Client.PowerShell.TestHook
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Reflection;

    // NOTE: Since all these 3 classes serve the same purpose, it is better to keep them in the same file

    /// <summary>
    /// This class will be instantiated on module import and the OnImport() method run.
    /// Register our binding redirect handler here.
    /// </summary>
    public class PdmsModuleInitializer : IModuleAssemblyInitializer
    {
        public void OnImport()
        {
            AppDomain.CurrentDomain.AssemblyResolve += DependencyResolution.ResolveAssembly;
        }
    }

    /// <summary>
    /// This class will be called upon module unload. 
    /// Unregister our binding redirect handler here.
    /// </summary>
    public class PdmsModuleCleanup : IModuleAssemblyCleanup
    {
        public void OnRemove(PSModuleInfo psModuleInfo)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= DependencyResolution.ResolveAssembly;
        }
    }

    /// <summary>
    /// Class handles binding redirect.
    /// This is to mitigate the issue when different modules have dependencies on different version of the same dll (e.g. Newtonsoft.Json),
    /// This handler will always load the one from the PDMS module directory (normally the latest version).
    /// </summary>
    internal static class DependencyResolution
    {
        /// <summary>
        /// The path we will load rediret assembly from.
        /// </summary>
        private static readonly string modulePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// List of assemblies that can be redirected
        /// </summary>
        private static readonly string[] AssembliesNeedRedirect = new string[]
        {
            "Newtonsoft.Json"
        };

        public static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            if (Array.Exists(AssembliesNeedRedirect, x => (string.Compare(x, assemblyName.Name, StringComparison.OrdinalIgnoreCase) == 0)))
            {
                return Assembly.LoadFrom(Path.Combine(modulePath, $"{assemblyName.Name}.dll"));
            }

            return null;
        }
    }
}
