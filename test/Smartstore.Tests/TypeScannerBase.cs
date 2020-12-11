using System;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Smartstore.Engine;
using Smartstore.Test.Common;

namespace Smartstore.Tests
{
    public abstract class TypeScannerBase : TestsBase
    {
        protected ITypeScanner typeScanner;

        protected abstract Type[] GetTypes();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            typeScanner = new DefaultTypeScanner(
                new ModuleCatalog(), 
                NullLogger.Instance, 
                typeof(TypeScannerBase).Assembly);
        }
    }
}
