//using System;
//using Smartstore.Core.Catalog.Products;
//using Smartstore.Core.Data;
//using Smartstore.Core.Localization;
//using Smartstore.Core.Security;
//using Smartstore.Data.Hooks;
//using Smartstore.Domain;

//namespace Smartstore.Tests.Data.Hooks
//{
//    internal class Hook_Entity_Inserted_Deleted_Update : DbSaveHook<SmartDbContext, BaseEntity>
//    {
//        protected override void OnInserted(BaseEntity entity, IHookedEntity entry) { }
//        protected override void OnDeleted(BaseEntity entity, IHookedEntity entry) { }
//        protected override void OnUpdating(BaseEntity entity, IHookedEntity entry) { }
//        protected override void OnUpdated(BaseEntity entity, IHookedEntity entry) { }
//    }

//    internal class Hook_Acl_Deleted : DbSaveHook<SmartDbContext, IAclRestricted>
//    {
//        protected override void OnDeleted(IAclRestricted entity, IHookedEntity entry) { }
//    }

//    [Important]
//    internal class Hook_Auditable_Inserting_Updating_Important : DbSaveHook<SmartDbContext, IAuditable>
//    {
//        protected override void OnInserting(IAuditable entity, IHookedEntity entry) { }
//        protected override void OnUpdating(IAuditable entity, IHookedEntity entry) { }
//    }

//    internal class Hook_SoftDeletable_Updating_ChangingState : DbSaveHook<SmartDbContext, ISoftDeletable>
//    {
//        protected override void OnUpdating(ISoftDeletable entity, IHookedEntity entry)
//        {
//            entry.State = EntityState.Unchanged;
//        }
//    }

//    internal class Hook_LocalizedEntity_Deleted : DbSaveHook<SmartDbContext, ILocalizedEntity>
//    {
//        protected override void OnDeleted(ILocalizedEntity entity, IHookedEntity entry) { }
//    }

//    internal class Hook_Product_Post : IDbSaveHook
//    {
//        public void OnBeforeSave(IHookedEntity entry)
//        {
//            throw new NotImplementedException();
//        }

//        public void OnAfterSave(IHookedEntity entry)
//        {
//            if (entry.EntityType != typeof(Product))
//                throw new NotSupportedException();
//        }

//        public void OnBeforeSaveCompleted() { }
//        public void OnAfterSaveCompleted() { }
//    }

//    internal class Hook_Category_Pre : IDbSaveHook
//    {
//        public void OnBeforeSave(IHookedEntity entry)
//        {
//            if (entry.EntityType != typeof(Category))
//                throw new NotSupportedException();
//        }

//        public void OnAfterSave(IHookedEntity entry)
//        {
//            throw new NotImplementedException();
//        }

//        public void OnBeforeSaveCompleted() { }
//        public void OnAfterSaveCompleted() { }
//    }
//}