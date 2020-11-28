using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Smartstore.Core.Localization.Routing;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LocalizationBuilderExtensions
    {
		public static IMvcBuilder AddAppLocalization(this IMvcBuilder builder)
		{
			//builder.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
			builder.AddDataAnnotationsLocalization(options =>
			{
				// TODO: (core) Set DataAnnotationLocalizerProvider
				//options.DataAnnotationLocalizerProvider = (type, factory) =>
				//{
				//	var scope = EfStringLocalizerFactory.ResolveTranslationScope(type, "SmartStore.DataAnnotations.Validation");
				//	return factory.Create(scope, type.FullName);
				//};
			});

			//builder.Services.AddScoped<IStringLocalizerScope, StringLocalizerScope>();
			//builder.Services.AddScoped<ITranslationService, TranslationService>();
			//builder.Services.AddTransient<IViewLocalizer, SmartViewLocalizer>();
			//builder.Services.AddSingleton<IStringLocalizerFactory, EfStringLocalizerFactory>();
			//builder.Services.AddSingleton<IUrlHelper, SmartUrlHelper>();

			builder.Services.TryAddEnumerable(
					ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, AppLocalizationMvcOptionsSetup>());

			return builder;
		}
	}

	internal class AppLocalizationMvcOptionsSetup : ConfigureOptions<MvcOptions>
	{
		//private readonly IStringLocalizerFactory _localizerFactory;
		private readonly IServiceProvider _serviceProvider;

		public AppLocalizationMvcOptionsSetup(
			/*IStringLocalizerFactory localizerFactory,*/
			IServiceProvider serviceProvider)
			: base(ConfigureMvc)
		{
			//_localizerFactory = localizerFactory;
			_serviceProvider = serviceProvider;
		}

		public override void Configure(MvcOptions options)
		{
			base.Configure(options);
			//options.ModelMetadataDetailsProviders.Add(new LocalizedMetadataProvider(_localizerFactory));
			options.Conventions.Add(new LocalizedRoutingConvention(_serviceProvider));
			////options.ModelValidatorProviders.Add(new LocalizedDataAnnotationsModelValidatorProvider());
		}

		public static void ConfigureMvc(MvcOptions options)
		{
		}
	}
}
