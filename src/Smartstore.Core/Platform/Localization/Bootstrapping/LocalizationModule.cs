using System;
using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;
using Smartstore.ComponentModel;
using Smartstore.Core.Seo;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Data;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class LocalizationModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CultureUrlFilter>().As<IUrlFilter>().SingleInstance();
            builder.RegisterType<LocalizedEntityService>().As<ILocalizedEntityService>().InstancePerLifetimeScope();
            builder.RegisterType<LanguageService>().As<ILanguageService>().InstancePerLifetimeScope();
            builder.RegisterType<LocalizationService>().As<ILocalizationService>().InstancePerLifetimeScope();
            builder.RegisterType<XmlResourceManager>().As<IXmlResourceManager>().InstancePerLifetimeScope();
            builder.RegisterType<LocalizedEntityHelper>().InstancePerLifetimeScope();

            builder.RegisterType<LanguageResolver>().As<ILanguageResolver>().InstancePerLifetimeScope();
            builder.RegisterType<LocalizationFileResolver>().As<ILocalizationFileResolver>().SingleInstance();

            builder.RegisterType<Text>().As<IText>().InstancePerLifetimeScope();
            builder.Register<Localizer>(c => c.Resolve<IText>().Get).InstancePerLifetimeScope();
            builder.Register<LocalizerEx>(c => c.Resolve<IText>().GetEx).InstancePerLifetimeScope();

            // Rule options provider.
            builder.RegisterType<LanguageRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        }

        protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
        {
            if (!DataSettings.DatabaseIsInstalled())
            {
                return;
            }
            
            var userProperty = FindUserProperty(registration.Activator.LimitType);

            if (userProperty == null)
                return;

            registration.Metadata.Add("Property.T", FastProperty.Create(userProperty));

            registration.PipelineBuilding += (sender, pipeline) =>
            {
                // Add our Localizer middleware to the pipeline.
                pipeline.Use(PipelinePhase.ParameterSelection, (context, next) => 
                {
                    next(context);

                    if (!context.NewInstanceActivated || context.Registration.Metadata.Get("Property.T") is not FastProperty prop)
                    {
                        return;
                    }

                    try
                    {
                        var iText = context.Resolve<IText>();
                        if (prop.Property.PropertyType == typeof(Localizer))
                        {
                            Localizer localizer = context.Resolve<IText>().Get;
                            prop.SetValue(context.Instance, localizer);
                        }
                        else
                        {
                            LocalizerEx localizerEx = context.Resolve<IText>().GetEx;
                            prop.SetValue(context.Instance, localizerEx);
                        }
                    }
                    catch { }
                });
            };
        }

        private static PropertyInfo FindUserProperty(Type type)
        {
            return type.GetProperty("T", typeof(Localizer)) ?? type.GetProperty("T", typeof(LocalizerEx));
        }
    }
}