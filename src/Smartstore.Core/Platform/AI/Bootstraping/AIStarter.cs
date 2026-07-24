using Autofac;
using Smartstore.Core.AI;
using Smartstore.Core.AI.Metadata;
using Smartstore.Core.AI.Prompting;
using Smartstore.Engine.Builders;
using Smartstore.Net.Http;

namespace Smartstore.Core.Bootstrapping;

internal sealed class AIStarter : StarterBase
{
    public override bool Matches(IApplicationContext appContext)
        => appContext.IsInstalled;

    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.AddHttpClient("AIRemoteMetadata")
            .AddSmartstoreUserAgent()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri("http://localhost:59318/");
                client.Timeout = TimeSpan.FromSeconds(2);
            });
    }

    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        // Register all prompt generators.
        var promptGeneratorTypes = appContext.TypeScanner.FindTypes<IAIPromptGenerator>();
        foreach (var type in promptGeneratorTypes)
        {
            builder.RegisterType(type).As<IAIPromptGenerator>().Keyed<IAIPromptGenerator>(type).InstancePerLifetimeScope();
        }

        builder.RegisterType<RemoteAIMetadataLoader>().As<IRemoteAIMetadataLoader>().SingleInstance();
        //builder.RegisterType<DefaultAIMetadataLoader>().As<IAIMetadataLoader>().SingleInstance();
        builder.RegisterType<JsonAIMetadataLoader>().As<IAIMetadataLoader>().SingleInstance();
        builder.RegisterType<DefaultAIChatCache>().As<IAIChatCache>().SingleInstance();
        builder.RegisterType<AIMessageBuilder>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<AIMessageResources>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<AIProviderFactory>().As<IAIProviderFactory>().InstancePerLifetimeScope();
        builder.RegisterType<DefaultLlmsGenerator>().As<ILlmsGenerator>().InstancePerLifetimeScope();
    }
}