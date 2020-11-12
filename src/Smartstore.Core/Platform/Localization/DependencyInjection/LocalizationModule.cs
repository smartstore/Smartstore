using Autofac;

namespace Smartstore.Core.Localization.DependencyInjection
{
    public sealed class LocalizationModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LocalizedEntityService>().As<ILocalizedEntityService>().InstancePerLifetimeScope();
            builder.RegisterType<LanguageService>().As<ILanguageService>().InstancePerLifetimeScope();
            builder.RegisterType<LocalizationService>().As<ILocalizationService>().InstancePerLifetimeScope();
            builder.RegisterType<XmlResourceManager>().As<IXmlResourceManager>().InstancePerLifetimeScope();
            builder.RegisterType<LocalizedEntityHelper>().InstancePerLifetimeScope();

            builder.RegisterType<Text>().As<IText>().InstancePerLifetimeScope();
            builder.Register<Localizer>(c => c.Resolve<IText>().Get).InstancePerLifetimeScope();
            builder.Register<LocalizerEx>(c => c.Resolve<IText>().GetEx).InstancePerLifetimeScope();
        }
    }
}