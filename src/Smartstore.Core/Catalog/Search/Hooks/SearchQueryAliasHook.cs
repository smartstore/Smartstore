using Autofac;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Seo;
using Smartstore.Data.Hooks;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Catalog.Search
{
    [Important]
    internal class SearchQueryAliasHook : AsyncDbSaveHook<BaseEntity>
    {
        private readonly SmartDbContext _db;
        private readonly Lazy<ICatalogSearchQueryAliasMapper> _catalogSearchQueryAliasMapper;
        private readonly SeoSettings _seoSettings;

        private string _errorMessage;

        public SearchQueryAliasHook(
            SmartDbContext db,
            Lazy<ICatalogSearchQueryAliasMapper> catalogSearchQueryAliasMapper,
            SeoSettings seoSettings)
        {
            _db = db;
            _catalogSearchQueryAliasMapper = catalogSearchQueryAliasMapper;
            _seoSettings = seoSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public override async Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            // TODO: (mg) (core) Please thoroughly check whether you need to hook every state. return Void for useless states.

            var type = entry.EntityType;
            var entity = entry.Entity;

            if (type == typeof(SpecificationAttribute))
            {
                if (await HasAliasDuplicate<ProductAttribute, SpecificationAttribute>(entry, entity, cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }

                if (IsPropertyModified(entry, "Alias"))
                {
                    await _catalogSearchQueryAliasMapper.Value.ClearAttributeCacheAsync();
                }
            }
            else if (entity is SpecificationAttributeOption specAttributeOption)
            {
                if (await HasEntityDuplicate<SpecificationAttributeOption>(
                    entry,
                    entity,
                    x => x.Name,
                    x => x.SpecificationAttributeId == specAttributeOption.SpecificationAttributeId && x.Name == specAttributeOption.Name,
                    cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }

                if (await HasAliasDuplicate<SpecificationAttributeOption>(entry, entity, null, cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }

                if (IsPropertyModified(entry, "Alias"))
                {
                    await _catalogSearchQueryAliasMapper.Value.ClearAttributeCacheAsync();
                }
            }
            else if (entity is ProductSpecificationAttribute specAttribute)
            {
                if (await HasEntityDuplicate<ProductSpecificationAttribute>(
                    entry,
                    entity,
                    x => x.SpecificationAttributeOption?.Name ?? _db.SpecificationAttributeOptions.Where(o => o.Id == x.SpecificationAttributeOptionId).Select(o => o.Name).FirstOrDefault(),
                    x => x.ProductId == specAttribute.ProductId && x.SpecificationAttributeOptionId == specAttribute.SpecificationAttributeOptionId,
                    cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }
            }
            else if (type == typeof(ProductAttribute))
            {
                if (await HasAliasDuplicate<ProductAttribute, SpecificationAttribute>(entry, entity, cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }

                if (IsPropertyModified(entry, "Alias"))
                {
                    await _catalogSearchQueryAliasMapper.Value.ClearAttributeCacheAsync();
                }
            }
            else if (entity is ProductAttributeOption attributeOption)
            {
                if (await HasEntityDuplicate<ProductAttributeOption>(
                    entry,
                    entity,
                    x => x.Name,
                    x => x.ProductAttributeOptionsSetId == attributeOption.ProductAttributeOptionsSetId && x.Name == attributeOption.Name,
                    cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }

                // ClearVariantCacheAsync() not necessary.
                if (await HasAliasDuplicate<ProductAttributeOption>(entry, entity, null, cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }
            }
            else if (entity is ProductVariantAttribute productAttribute)
            {
                if (await HasEntityDuplicate<ProductVariantAttribute>(
                    entry,
                    entity,
                    x => x.ProductAttribute?.Name ?? _db.ProductAttributes.Where(a => a.Id == x.ProductAttributeId).Select(a => a.Name).FirstOrDefault(),
                    x => x.ProductId == productAttribute.ProductId && x.ProductAttributeId == productAttribute.ProductAttributeId,
                    cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }
            }
            else if (entity is ProductVariantAttributeValue attributeValue)
            {
                if (await HasEntityDuplicate<ProductVariantAttributeValue>(
                    entry,
                    entity,
                    x => x.Name,
                    x => x.ProductVariantAttributeId == attributeValue.ProductVariantAttributeId && x.Name == attributeValue.Name,
                    cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }

                if (await HasAliasDuplicate<ProductVariantAttributeValue>(
                    entry,
                    entity,
                    (all, e) => all.AnyAsync(x => x.Id != e.Id && x.ProductVariantAttributeId == e.ProductVariantAttributeId && x.Alias == e.Alias),
                    cancelToken))
                {
                    entry.ResetState();
                    return HookResult.Ok;
                }

                if (IsPropertyModified(entry, "Alias"))
                {
                    await _catalogSearchQueryAliasMapper.Value.ClearAttributeCacheAsync();
                }
            }
            else if (entity is LocalizedProperty prop)
            {
                // Note, not fired when SpecificationAttribute or SpecificationAttributeOption deleted.
                // Not necessary anyway because cache cleared by above code.
                var keyGroup = prop.LocaleKeyGroup;

                if (!prop.LocaleKey.EqualsNoCase("Alias"))
                {
                    return HookResult.Ok;
                }

                // Validating ProductVariantAttributeValue goes too far here.
                if (!keyGroup.EqualsNoCase(nameof(SpecificationAttribute)) &&
                    !keyGroup.EqualsNoCase(nameof(SpecificationAttributeOption)) &&
                    !keyGroup.EqualsNoCase(nameof(ProductAttribute)) &&
                    !keyGroup.EqualsNoCase(nameof(ProductAttributeOption)))
                {
                    return HookResult.Ok;
                }

                // Check alias duplicate.
                if (entry.InitialState == EState.Added || entry.InitialState == EState.Modified)
                {
                    prop.LocaleValue = SlugUtility.Slugify(prop.LocaleValue, _seoSettings);

                    if (prop.LocaleValue.HasValue() && await HasAliasDuplicate(prop, cancelToken))
                    {
                        entry.ResetState();
                        return HookResult.Ok;
                    }
                }

                if (IsPropertyModified(entry, "LocaleValue"))
                {
                    if (keyGroup.EqualsNoCase(nameof(SpecificationAttribute)) || keyGroup.EqualsNoCase(nameof(SpecificationAttributeOption)))
                    {
                        await _catalogSearchQueryAliasMapper.Value.ClearAttributeCacheAsync();
                    }
                    else if (keyGroup.EqualsNoCase(nameof(ProductAttribute)) || keyGroup.EqualsNoCase(nameof(ProductVariantAttributeValue)))
                    {
                        // Not necessary for ProductAttributeOption.
                        await _catalogSearchQueryAliasMapper.Value.ClearVariantCacheAsync();
                    }
                }
            }
            else
            {
                return HookResult.Void;
            }

            return HookResult.Ok;
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // The user must be informed that his changes were not saved due to a duplicate alias.
            if (_errorMessage.HasValue())
            {
                var message = new string(_errorMessage);
                _errorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
        }

        private async Task<bool> HasAliasDuplicate<TEntity>(
            IHookedEntity entry,
            BaseEntity baseEntity,
            Func<IQueryable<TEntity>, TEntity, Task<bool>> hasDuplicate,
            CancellationToken cancelToken)
            where TEntity : BaseEntity
        {
            if (entry.InitialState == EState.Added || entry.InitialState == EState.Modified)
            {
                if (baseEntity is ISearchAlias entity)
                {
                    entity.Alias = SlugUtility.Slugify(entity.Alias, _seoSettings);
                    if (entity.Alias.HasValue())
                    {
                        var dbSet = _db.Set<TEntity>().AsNoTracking();

                        //if (allEntities != null && allEntities.Any(x => x.Id != entity.Id && x.Alias == entity.Alias))
                        if (dbSet is IQueryable<ISearchAlias> allEntities)
                        {
                            var duplicateExists = hasDuplicate == null
                                ? await allEntities.AnyAsync(x => x.Id != entity.Id && x.Alias == entity.Alias, cancelToken)
                                : await hasDuplicate(dbSet, (TEntity)entity);

                            if (duplicateExists)
                            {
                                _errorMessage = CreateValueExistsMessage("Common.Error.AliasAlreadyExists", entity.Alias);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private async Task<bool> HasAliasDuplicate<T1, T2>(IHookedEntity entry, BaseEntity baseEntity, CancellationToken cancelToken)
            where T1 : BaseEntity where T2 : BaseEntity
        {
            if (entry.InitialState == EState.Added || entry.InitialState == EState.Modified)
            {
                if (baseEntity is ISearchAlias entity)
                {
                    entity.Alias = SlugUtility.Slugify(entity.Alias, _seoSettings);
                    if (entity.Alias.HasValue())
                    {
                        var entities1 = _db.Set<T1>().AsNoTracking() as IQueryable<ISearchAlias>;
                        var entities2 = _db.Set<T2>().AsNoTracking() as IQueryable<ISearchAlias>;

                        var duplicate1 = await entities1.FirstOrDefaultAsync(x => x.Alias == entity.Alias, cancelToken);
                        var duplicate2 = await entities2.FirstOrDefaultAsync(x => x.Alias == entity.Alias, cancelToken);

                        if (duplicate1 != null || duplicate2 != null)
                        {
                            var type = entry.EntityType;

                            if (duplicate1 != null && duplicate1.Id == entity.Id && type == typeof(T1))
                            {
                                return false;
                            }
                            if (duplicate2 != null && duplicate2.Id == entity.Id && type == typeof(T2))
                            {
                                return false;
                            }

                            _errorMessage = CreateValueExistsMessage("Common.Error.AliasAlreadyExists", entity.Alias);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private async Task<bool> HasAliasDuplicate(LocalizedProperty property, CancellationToken cancelToken)
        {
            var existingProperties = await _db.LocalizedProperties.Where(x =>
                x.Id != property.Id &&
                x.LocaleKey == "Alias" &&
                x.LocaleKeyGroup == property.LocaleKeyGroup &&
                x.LanguageId == property.LanguageId &&
                x.LocaleValue == property.LocaleValue)
                .ToListAsync(cancelToken);

            if (!existingProperties.Any())
            {
                // Check cases where alias has to be globally unique.
                string otherKeyGroup = null;

                if (property.LocaleKeyGroup.EqualsNoCase(nameof(SpecificationAttribute)))
                {
                    otherKeyGroup = nameof(ProductAttribute);
                }
                else if (property.LocaleKeyGroup.EqualsNoCase(nameof(ProductAttribute)))
                {
                    otherKeyGroup = nameof(SpecificationAttribute);
                }

                if (otherKeyGroup.HasValue())
                {
                    existingProperties = await _db.LocalizedProperties
                        .ApplyStandardFilter(property.LanguageId, 0, otherKeyGroup, "Alias")
                        .Where(x => x.LocaleValue == property.LocaleValue)
                        .ToListAsync(cancelToken);
                }

                if (!existingProperties.Any())
                {
                    return false;
                }
            }

            var toDeleteIds = new HashSet<int>();

            foreach (var prop in existingProperties)
            {
                // Check if the related entity exists. The user would not be able to solve an invalidated alias when the related entity does not exist anymore.
                var relatedEntityExists = true;

                if (prop.LocaleKeyGroup.EqualsNoCase(nameof(SpecificationAttribute)))
                {
                    relatedEntityExists = await _db.SpecificationAttributes.AnyAsync(x => x.Id == prop.EntityId, cancelToken);
                }
                else if (prop.LocaleKeyGroup.EqualsNoCase(nameof(SpecificationAttributeOption)))
                {
                    relatedEntityExists = await _db.SpecificationAttributeOptions.AnyAsync(x => x.Id == prop.EntityId, cancelToken);
                }
                else if (prop.LocaleKeyGroup.EqualsNoCase(nameof(ProductAttribute)))
                {
                    relatedEntityExists = await _db.ProductAttributes.AnyAsync(x => x.Id == prop.EntityId, cancelToken);
                }
                else if (prop.LocaleKeyGroup.EqualsNoCase(nameof(ProductAttributeOption)))
                {
                    relatedEntityExists = await _db.ProductAttributeOptions.AnyAsync(x => x.Id == prop.EntityId, cancelToken);
                }
                //else if (prop.LocaleKeyGroup.EqualsNoCase(nameof(ProductVariantAttributeValue)))
                //{
                //}

                if (relatedEntityExists)
                {
                    // We cannot delete any localized property because we are going to throw duplicate alias exception in OnBeforeSaveCompleted.
                    _errorMessage = CreateValueExistsMessage("Common.Error.AliasAlreadyExists", property.LocaleValue);
                    return true;
                }
                else
                {
                    // Delete accidentally dead localized properties.
                    toDeleteIds.Add(prop.Id);
                }
            }

            if (toDeleteIds.Any())
            {
                try
                {
                    await _db.LocalizedProperties
                        .Where(x => toDeleteIds.Contains(x.Id))
                        .ExecuteDeleteAsync(cancelToken);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            return false;
        }

        private async Task<bool> HasEntityDuplicate<TEntity>(
            IHookedEntity entry,
            BaseEntity baseEntity,
            Func<TEntity, string> getName,
            Expression<Func<TEntity, bool>> getDuplicate,
            CancellationToken cancelToken) where TEntity : BaseEntity
        {
            if (entry.InitialState == EState.Added || entry.InitialState == EState.Modified)
            {
                var dbSet = _db.Set<TEntity>().AsNoTracking();
                var existingEntity = await dbSet.FirstOrDefaultAsync(getDuplicate, cancelToken);

                if (existingEntity != null && existingEntity.Id != baseEntity.Id)
                {
                    _errorMessage = CreateValueExistsMessage("Common.Error.OptionAlreadyExists", getName(existingEntity));
                    return true;
                }
            }

            return false;
        }

        private static bool IsPropertyModified(IHookedEntity entry, string propertyName)
        {
            var result = false;

            if (entry.State != EState.Detached)
            {
                var prop = entry.Entry.Property(propertyName);
                if (prop != null)
                {
                    if (entry.State == EState.Added)
                    {
                        // OriginalValues cannot be used for entities in the Added state.
                        result = prop.CurrentValue != null;
                    }
                    else if (entry.State == EState.Deleted)
                    {
                        // CurrentValues cannot be used for entities in the Deleted state.
                        result = prop.OriginalValue != null;
                    }
                    else
                    {
                        result =
                            (prop.CurrentValue != null && !prop.CurrentValue.Equals(prop.OriginalValue)) ||
                            (prop.OriginalValue != null && !prop.OriginalValue.Equals(prop.CurrentValue));
                    }
                }
            }

            return result;
        }

        private string CreateValueExistsMessage(string resourceKey, string checkedValue)
        {
            return T(resourceKey).Value.FormatInvariant(checkedValue.NaIfEmpty()) + " " + T("Common.Error.ChooseDifferentValue");
        }
    }
}
