using System.Collections.Generic;
using NUnit.Framework;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Tests.Domain
{
    [TestFixture]
    public class EntityEqualityTests
    {
        [Test]
        public void Two_transient_entities_should_not_be_equal()
        {
            var p1 = new Product();
            var p2 = new Product();

            Assert.That(p2, Is.Not.EqualTo(p1), "Different transient entities should not be equal");
        }

        [Test]
        public void Two_references_to_same_transient_entity_should_be_equal()
        {
            var p1 = new Product();
            var p2 = p1;

            Assert.That(p2, Is.EqualTo(p1), "Two references to the same transient entity should be equal");
        }

        [Test]
        public void Two_references_with_the_same_id_should_be_equal()
        {
            int id = 10;
            var p1 = new Product { Id = id };
            var p2 = new Product { Id = id };

            Assert.That(p2, Is.EqualTo(p1), "Entities with the same id should be equal");
        }

        [Test]
        public void Entities_with_different_id_should_not_be_equal()
        {
            var p1 = new Product { Id = 2 };
            var p2 = new Product { Id = 5 };

            Assert.That(p2, Is.Not.EqualTo(p1), "Entities with different ids should not be equal");
        }

        [Test]
        public void Entity_should_not_equal_transient_entity()
        {
            var p1 = new Product { Id = 1 };
            var p2 = new Product();

            Assert.That(p2, Is.Not.EqualTo(p1), "Entity and transient entity should not be equal");
        }

        [Test]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Assertion", "NUnit2021:Incompatible types for EqualTo constraint", Justification = "<Pending>")]
        public void Entities_with_same_id_but_different_type_should_not_be_equal()
        {
            int id = 10;
            var p1 = new Product { Id = id };

            var c1 = new Category { Id = id };

            Assert.That(p1, Is.Not.EqualTo(c1), "Entities of different types should not be equal, even if they have the same id");
        }

        [Test]
        public void Equality_works_using_operators()
        {
            var p1 = new Product { Id = 1 };
            var p2 = new Product { Id = 1 };

            Assert.That(p1, Is.EqualTo(p2));

            var p3 = new Product();

            Assert.That(p1, Is.Not.EqualTo(p3));
        }

        [Test]
        public void Equality_works_with_HashSets()
        {
            var p1 = new Product { Id = 1 };
            var p2 = new Product { Id = 1 };

            var hset = new HashSet<Product>();
            hset.UnionWith(new[] { p1, p2 });

            Assert.That(hset, Has.Count.EqualTo(1));

            var p3 = new Product { Id = 2 };
            hset.Add(p3);

            Assert.That(hset, Has.Count.EqualTo(2));
        }
    }
}
