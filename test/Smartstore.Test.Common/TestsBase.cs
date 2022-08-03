using System.Security.Principal;
using Moq;
using NUnit.Framework;

namespace Smartstore.Test.Common
{
    public abstract class TestsBase
    {
        protected MockRepository mocks;

        [OneTimeSetUp]
        public virtual void SetUp()
        {
            mocks = new MockRepository(MockBehavior.Loose);
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (mocks != null)
            {
                mocks.VerifyAll();
            }
        }

        protected static IPrincipal CreatePrincipal(string name, params string[] roles)
        {
            return new GenericPrincipal(new GenericIdentity(name, "TestIdentity"), roles);
        }
    }
}
