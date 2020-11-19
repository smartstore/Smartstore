using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Dasync.Collections;
using Smartstore.Core.Data;

namespace Smartstore.Core.Seo.DependencyInjection
{
    public sealed class SeoModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UrlService>().As<IUrlService>().InstancePerLifetimeScope();
            builder.RegisterType<XmlSitemapGenerator>().As<IXmlSitemapGenerator>().InstancePerLifetimeScope();

            builder.RegisterType<TestProductXmlSitemapPublisher>().As<IXmlSitemapPublisher>().InstancePerLifetimeScope();
        }
    }

    internal class TestProductXmlSitemapPublisher : IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;

        public TestProductXmlSitemapPublisher(SmartDbContext db)
        {
            _db = db;
        }

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            return new ProductXmlSitemapResult { DbContext = _db, Context = context };
        }

        class ProductXmlSitemapResult : XmlSitemapProvider
        {
            public SmartDbContext DbContext { get; init; }
            public XmlSitemapBuildContext Context { get; init; }

            public override Task<int> GetTotalCountAsync()
            {
                return Task.FromResult(50000);
            }

            public override IAsyncEnumerable<NamedEntity> EnlistAsync(CancellationToken cancelToken = default)
            {
                return Enlist().ToAsyncEnumerable();
            }

            private static IEnumerable<NamedEntity> Enlist()
            {
                int i = 50000;
                while (i > 0)
                {
                    yield return new NamedEntity { EntityName = "Product", Id = i, LastMod = DateTime.UtcNow };
                    i--;
                }
            }

            public override int Order => int.MaxValue;
        }
    }
}