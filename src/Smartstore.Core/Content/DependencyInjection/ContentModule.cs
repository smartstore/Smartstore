using Autofac;
using Smartstore.Core.Content.Blogs;
using Smartstore.Core.Content.News;

namespace Smartstore.Core.DependencyInjection
{
    public sealed class ContentModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BlogService>().As<IBlogService>().InstancePerLifetimeScope();
            builder.RegisterType<NewsService>().As<INewsService>().InstancePerLifetimeScope();
        }
    }
}