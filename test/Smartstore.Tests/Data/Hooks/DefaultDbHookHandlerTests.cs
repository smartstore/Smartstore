using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Tests.Data.Hooks
{
    [TestFixture]
    public class DefaultDbHookHandlerTests
    {
        private SmartDbContext _db;
        private Lazy<IDbSaveHook, HookMetadata>[] _hooks;
        private IDbHookProcessor _handler;
        private IDbHookRegistry _registry;

        [OneTimeSetUp]
        public virtual void SetUp()
        {
            _db = new SmartDbContext(new Microsoft.EntityFrameworkCore.DbContextOptions<SmartDbContext>());
            _hooks =
            [
                CreateHook<Hook_Acl_Deleted, IAclRestricted>(),
                CreateHook<Hook_Auditable_Inserting_Updating_Important, IAuditable>(),
                CreateHook<Hook_Category_Pre, BaseEntity>(),
                CreateHook<Hook_Entity_Inserted_Deleted_Update, BaseEntity>(),
                CreateHook<Hook_LocalizedEntity_Deleted, ILocalizedEntity>(),
                CreateHook<Hook_Product_Post, BaseEntity>(),
                CreateHook<Hook_SoftDeletable_Updating_ChangingState, ISoftDeletable>()
            ];
            _registry = new DefaultDbHookRegistry(_hooks);
            //_handler = new LegacyDbHookHandler(_hooks);
            _handler = new DefaultDbHookProcessor(_registry, new SimpleDbHookActivator());
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task Can_handle_voidness()
        {
            var entries = new[]
            {
                CreateEntry<Product>(EntityState.Modified), // > Hook_Entity_Inserted_Deleted_Update, Hook_Product_Post
				CreateEntry<GenericAttribute>(EntityState.Deleted), // Hook_Entity_Inserted_Deleted_Update
				CreateEntry<Currency>(EntityState.Deleted), // Hook_Acl_Deleted, Hook_Entity_Inserted_Deleted_Update
				CreateEntry<Category>(EntityState.Added) // Hook_Entity_Inserted_Deleted_Update
			};

            var processedHooks = (await _handler.SavedChangesAsync(entries, HookImportance.Normal)).ProcessedHooks;
            var expected = GetExpectedSaveHooks(entries, true, false);

            Assert.That(processedHooks.Count(), Is.EqualTo(expected.Count));
            Assert.That(processedHooks.All(x => expected.Contains(x.GetType())), Is.True);
        }

        [Test]
        public async Task Can_handle_importance()
        {
            var entries = new[]
            {
                CreateEntry<Product>(EntityState.Modified), // > Important
				CreateEntry<GenericAttribute>(EntityState.Deleted),
                CreateEntry<Currency>(EntityState.Modified),
                CreateEntry<Category>(EntityState.Added) // > Important
			};

            var result = await _handler.SavingChangesAsync(entries, HookImportance.Important);
            var anyStateChanged = result.AnyStateChanged;
            var processedHooks = result.ProcessedHooks;
            var expected = GetExpectedSaveHooks(entries, false, true);

            Assert.Multiple(() =>
            {
                Assert.That(processedHooks.Count(), Is.EqualTo(expected.Count));
                Assert.That(anyStateChanged, Is.False);
                Assert.That(processedHooks.All(x => expected.Contains(x.GetType())), Is.True);
            });
        }

        private ICollection<Type> GetExpectedSaveHooks(IEnumerable<IHookedEntity> entries, bool isPost, bool importantOnly)
        {
            var hset = new HashSet<Type>();

            foreach (var hook in _hooks)
            {
                foreach (var e in entries)
                {
                    if (ShouldHandle(hook.Metadata.ImplType, e, isPost, importantOnly))
                    {
                        hset.Add(hook.Metadata.ImplType);
                    }
                }
            }

            return hset;
        }

        private static bool ShouldHandle(Type hookType, IHookedEntity entry, bool isPost, bool importantOnly)
        {
            bool result = false;

            if (hookType == typeof(Hook_Acl_Deleted))
            {
                result = isPost && !importantOnly && typeof(IAclRestricted).IsAssignableFrom(entry.EntityType) && entry.State == EntityState.Deleted;
            }
            else if (hookType == typeof(Hook_Auditable_Inserting_Updating_Important))
            {
                result = !isPost && typeof(IAuditable).IsAssignableFrom(entry.EntityType) && (entry.State == EntityState.Added || entry.State == EntityState.Modified);
            }
            else if (hookType == typeof(Hook_Category_Pre))
            {
                result = !isPost && !importantOnly && typeof(Category).IsAssignableFrom(entry.EntityType);
            }
            else if (hookType == typeof(Hook_Entity_Inserted_Deleted_Update))
            {
                result =
                    (isPost && !importantOnly && (entry.State == EntityState.Added || entry.State == EntityState.Deleted)) ||
                    (!isPost && !importantOnly && (entry.State == EntityState.Modified));
            }
            else if (hookType == typeof(Hook_LocalizedEntity_Deleted))
            {
                result = isPost && !importantOnly && typeof(ILocalizedEntity).IsAssignableFrom(entry.EntityType) && entry.State == EntityState.Deleted;
            }
            else if (hookType == typeof(Hook_Product_Post))
            {
                result = isPost && !importantOnly && typeof(Product).IsAssignableFrom(entry.EntityType);
            }
            else if (hookType == typeof(Hook_SoftDeletable_Updating_ChangingState))
            {
                result = !isPost && !importantOnly && typeof(ISoftDeletable).IsAssignableFrom(entry.EntityType) && entry.State == EntityState.Modified;
            }

            return result;
        }

        #region Utils

        private IHookedEntity CreateEntry<T>(EntityState state) where T : BaseEntity, new()
        {
            return new HookedEntityMock(new T(), state, _db);
        }

        private static Lazy<IDbSaveHook, HookMetadata> CreateHook<THook, TEntity>() where THook : IDbSaveHook, new() where TEntity : class
        {
            var hook = new Lazy<IDbSaveHook, HookMetadata>(() => new THook(), new HookMetadata
            {
                HookedType = typeof(TEntity),
                DbContextType = typeof(SmartDbContext),
                ImplType = typeof(THook),
                Importance = typeof(THook).GetAttribute<ImportantAttribute>(false)?.Importance ?? HookImportance.Normal,
                Order = 0
            });

            return hook;
        }

        private static Lazy<IDbSaveHook, HookMetadata> CreateHook(Type implType, Type hookedType)
        {
            var hook = new Lazy<IDbSaveHook, HookMetadata>(() => (IDbSaveHook)Activator.CreateInstance(implType), new HookMetadata
            {
                HookedType = hookedType,
                DbContextType = typeof(SmartDbContext),
                ImplType = implType,
                Importance = implType.GetAttribute<ImportantAttribute>(false)?.Importance ?? HookImportance.Normal,
                Order = implType.GetAttribute<OrderAttribute>(false)?.Order ?? 0
            });

            return hook;
        }

        #endregion
    }
}
