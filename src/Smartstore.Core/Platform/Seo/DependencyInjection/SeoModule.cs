using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Products;
using Smartstore.Core.Stores;
using Smartstore.Data;

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
        // TODO: (core) Remove this class later
        private readonly SmartDbContext _db;

        public TestProductXmlSitemapPublisher(SmartDbContext db)
        {
            _db = db;
        }

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            var query = _db.Products
                .AsNoTracking()
                .ApplyStoreFilter(context.RequestStoreId);
            
            return new ProductXmlSitemapResult { DbContext = _db, Query = query, Context = context };
        }

        class ProductXmlSitemapResult : XmlSitemapProvider
        {
            public SmartDbContext DbContext { get; init; }
            public IQueryable<Product> Query { get; init; }
            public XmlSitemapBuildContext Context { get; init; }

            public override Task<int> GetTotalCountAsync()
            {
                return Query.CountAsync();
            }

            public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
            {
                var pager = new FastPager<Product>(Query, Context.MaximumNodeCount);

                while ((await pager.ReadNextPageAsync(x => new { x.Id, x.UpdatedOnUtc }, x => x.Id)).Out(out var products))
                {
                    if (Context.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    foreach (var x in products)
                    {
                        yield return new NamedEntity { EntityName = "Product", Id = x.Id, LastMod = x.UpdatedOnUtc };
                    }
                }
            }

            //private IEnumerable<NamedEntity> Enlist()
            //{
            //    int i = 50000;
            //    while (i > 0)
            //    {
            //        yield return new NamedEntity { EntityName = "Product", Id = i, LastMod = DateTime.UtcNow };
            //        i--;
            //    }
            //}

            public override int Order => int.MaxValue;
        }
    }
}