namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Autofac
{
    using AutoMapper;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;
    using Microsoft.PrivacyServices.Identity.Metadata;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Registers the dependencies for this API.
    /// </summary>
    public class RegistrationModule : Module
    {
        private readonly string executionPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationModule" /> class.
        /// </summary>
        /// <param name="executionPath">The location where the code is running.</param>
        public RegistrationModule(string executionPath)
        {
            this.executionPath = executionPath;
        }

        /// <summary>
        /// Registers dependencies for this component with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            // Hook up AutoMapper configurations.
            builder.RegisterInstance<Policy>(Policies.Current);
            builder.RegisterInstance<IManifest>(Manifest.Current);
            builder.RegisterType<MappingProfile>().As<Profile>().SingleInstance();
            builder.RegisterType<OperationNameProvider>().As<IOperationNameProvider>().SingleInstance();
            builder.RegisterType<OperationAccessProvider>().As<IOperationAccessProvider>().SingleInstance();
        }
    }
}
