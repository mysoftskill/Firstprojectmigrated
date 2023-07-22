namespace Microsoft.PrivacyServices.DataManagement.Common.Autofac
{
    using System;
    using System.Collections.Generic;

    using AutoMapper;
    using AutoMapper.Configuration;

    using global::Autofac;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    /// <summary>
    /// Extension functions for the ContainerBuilder class to enable Parallax registrations.
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Register AutoMapper in the system. This should only be called once.
        /// </summary>
        /// <param name="containerBuilder">The container build for registration.</param>
        public static void RegisterAutoMapper(this ContainerBuilder containerBuilder)
        {
            containerBuilder
               .Register(ctx =>
               {
                   var mapConfigSetup = new MapperConfigurationExpression();

                   foreach (var profile in ctx.Resolve<IEnumerable<Profile>>())
                   {
                       mapConfigSetup.AddProfile(profile);
                   }

                   mapConfigSetup.AllowNullCollections = true;

                   return new MapperConfiguration(mapConfigSetup);
               })
               .As<IConfigurationProvider>()
               .SingleInstance();

            containerBuilder.Register(ctx => new Mapper(ctx.Resolve<IConfigurationProvider>())).As<IMapper>().SingleInstance();
        }

        /// <summary>
        /// Register AutoMapper in the system. This should only be called once.
        /// </summary>
        /// <param name="containerBuilder">The container build for registration.</param>
        public static void RegisterAppConfiguration(this ContainerBuilder containerBuilder)
        {
            containerBuilder
               .Register(ctx =>
               {
                   var configuration = ctx.Resolve<IPrivacyConfigurationManager>();
                   var appConfigSettings = configuration.AppConfigSettings;

                   string labelFilter = string.IsNullOrEmpty(appConfigSettings.LabelFilter) ? LabelNames.None : appConfigSettings.LabelFilter;

                   if (labelFilter == "devbox")
                   {
                       return new AppConfiguration(@"local.settings.json");
                   }
                   else
                   {
                       return new AppConfiguration(new Uri(appConfigSettings.Endpoint), labelFilter);
                   }
               })
               .As<IAppConfiguration>()
               .SingleInstance();
        }
    }
}
