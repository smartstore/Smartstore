using System;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;
using Smartstore.Test.Common;

namespace Smartstore.Tests.Engine
{
    [TestFixture]
    public class TypeScannerTests
    {
        [Test]
        public void TypeFinder_Benchmark_Findings()
        {
            var scanner = new DefaultTypeScanner(
                new[] { typeof(ISomeInterface).Assembly },
                new ModuleCatalog(Array.Empty<IModuleDescriptor>()),
                NullLogger.Instance);

            var type = scanner.FindTypes<ISomeInterface>();
            type.Count().ShouldEqual(1);
            typeof(ISomeInterface).IsAssignableFrom(type.FirstOrDefault()).ShouldBeTrue();
        }

        public interface ISomeInterface
        {
        }

        public class SomeClass : ISomeInterface
        {
        }
    }
}
