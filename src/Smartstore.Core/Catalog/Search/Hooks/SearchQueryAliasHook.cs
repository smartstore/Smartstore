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
        const string AliasLocaleKey = "Alias";

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
            var type = entry.EntityType;
            var entity = entry.Entity;

            if (type == typeof(SpecificationAttribute))
            {
                if (await CheckAliasDuplicate<ProductAttribute, SpecificationAttribute>(entry, entity, cancelToken))
                {
                    return HookResult.Ok;
                }

                if (IsPropertyModified(entry, nameof(SpecificationAttribute.Alias)))
                {
                    await _catalogSearchQueryAliasMapper.Value.ClearAttributeCacheAsync();
                }
            }
            else if (entity is SpecificationAttributeOption specAttributeOption)
            {
                if (await CheckEntityDuplicate<SpecificationAttributeOption>(
                    entry,
                    entity,
                    x => x.Name,
                    x => x.SpecificationAttributeId == specAttributeOption.SpecificationAttributeId && x.Name == specAttributeOption.Name,
                    cancelToken))
                {
                    return HookResult.Ok;
                }

                if (await CheckAliasDuplicate<SpecificationAttributeOption>(entry, entity, null, cancelToken))
                {
                    return HookResult.Ok;
                }

                if (IsPropertyModified(entry, nameof(SpecificationAttributeOption.Alias)))
                {
                    await _catalogSearchQueryAliasMapper.Value.ClearAttributeCacheAsync();
                }
            }
            else if (entity is ProductSpecificationAttribute specAttribute)
            {
                if (entry.State == EState.Deleted)
                    return HookResult.Void;

                await CheckEntityDuplicate<ProductSpecificationAttribute>(
                    entry,
                    entity,
                    x => x.SpecificationAttributeOption?.Name ?? _db.SpecificationAttributeOptions
                        .Where(o => o.Id == x.SpecificationAttributeOptionId)
                        .Select(o => o.Name)
                        .FirstOrDefault(),
                    x => x.ProductId == specAttribute.ProductId && x.SpecificationAttributeOptionId == specAttribute.SpecificationAttributeOptionId,
                    cancelToken);
            }
            else if (type == typeof(ProductAttribute))
            {
                if (await CheckAliasDuplicate<ProductAttribute, SpecificationAttribute>(entry, entity, cancelToken))
                {
                    return HookResult.Ok;
                }

                if (IsPropertyModified(entry, nameof(ProductAttribute.Alias)))
                {
                    await _catalogSearchQueryAliasMapper.Value.ClearAttributeCacheAsync();
                }
            }
            else if (entity is ProductAttributeOption attributeOption)
            {
                if (entry.State == EState.Deleted)
                    return HookResult.Void;

                if (await CheckEntityDuplicate<ProductAttributeOption>(
                    entry,
                    entity,
                    x => x.Name,
                    x => x.ProductAttributeOptionsSetId == attributeOption.ProductAttributeOptionsSetId && x.Name == attributeOption.Name,
                    cancelToken))
                {
                    return HookResult.Ok;
                }

                // ClearVariantCacheAsync not necessary here.
                await CheckAliasDuplicate<ProductAttributeOption>(entry, entity, null, cancelToken);
            }
            else if (entity is ProductVariantAttribute productAttribute)
            {
                if (entry.State == EState.Deleted)
                    return HookResult.Void;

                await CheckEntityDuplicate<ProductVariantAttribute>(
                    entry,
                    entity,
                    x => x.ProductAttribute?.Name ?? _db.ProductAttributes
                        .Where(a => a.Id == x.ProductAttributeId)
                        .Select(a => a.Name)
                        .FirstOrDefault(),
                    x => x.ProductId == productAttribute.ProductId && x.ProductAttributeId == productAttribute.ProductAttributeId,
                    cancelToken);
            }
            else if (entity is ProductVariantAttributeValue attributeValue)
            {
                if (await CheckEntityDuplicate<ProductVariantAttributeValue>(
                    entry,
                    entity,
                    x => x.Name,
                    x => x.ProductVariantAttributeId == attributeValue.ProductVariantAttributeId && x.Name == attributeValue.Name,
                    cancelToken))
                {
                    return HookResult.Ok;
                }

                if (await CheckAliasDuplicate<ProductVariantAttributeValue>(
                    entry,
                    entity,
                    (all, e) => all.AnyAsync(x => x.Id != e.Id && x.ProductVariantAttributeId == e.ProductVariantAttributeId && x.Alias == e.Alias),
                    cancelToken))
                {
                    return HookResult.Ok;
                }

                if (IsPropertyModified(entry, nameof(ProductVariantAttributeValue.Alias)))
                {
                    await _catalogSearchQueryAliasMapper.Value.ClearAttributeCacheAsync();
                }
            }
            else if (entity is LocalizedProperty prop)
            {
                // INFO: Not fired when SpecificationAttribute or SpecificationAttributeOption is deleted.
                // Not necessary anyway because cache cleared by above code.
                if (!prop.LocaleKey.EqualsNoCase(AliasLocaleKey))
                    return HookResult.Ok;

                // Validating ProductVariantAttributeValue goes too far here.
                var keyGroup = prop.LocaleKeyGroup;
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

                    if (prop.LocaleValue.HasValue() && await CheckAliasDuplicate(entry, prop, cancelToken))
                    {
                        return HookResult.Ok;
                    }
                }

                if (IsPropertyModified(entry, nameof(LocalizedProperty.LocaleValue)))
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

        /// <summary>
        /// Checks whether the given alias already exists in the database. 
        /// An error message is created if so. This is relevant for the "Added" and "Modified" states.
        /// </summary>
        private async Task<bool> CheckAliasDuplicate<TEntity>(
            IHookedEntity entry,
            BaseEntity baseEntity,
            Func<IQueryable<TEntity>, TEntity, Task<bool>> hasDuplicate,
            CancellationToken cancelToken)
            where TEntity : BaseEntity
        {
            if ((entry.InitialState == EState.Added || entry.InitialState == EState.Modified)
                && baseEntity is ISearchAlias entity)
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
                            entry.ResetState();
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the given alias already exists for the two entity types in the database.
        /// An error message is created if so. This is relevant for the "Added" and "Modified" states.
        /// </summary>
        private async Task<bool> CheckAliasDuplicate<T1, T2>(IHookedEntity entry, BaseEntity baseEntity, CancellationToken cancelToken)
            where T1 : BaseEntity where T2 : BaseEntity
        {
            if ((entry.InitialState == EState.Added || entry.InitialState == EState.Modified)
                && baseEntity is ISearchAlias entity)
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
                        entry.ResetState();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks for a duplicate alias in localized properties. An error message is created if so.
        /// </summary>
        private async Task<bool> CheckAliasDuplicate(IHookedEntity entry, LocalizedProperty property, CancellationToken cancelToken)
        {
            var existingProps = await _db.LocalizedProperties
                .Where(x =>
                    x.Id != property.Id &&
                    x.LocaleKey == AliasLocaleKey &&
                    x.LocaleKeyGroup == property.LocaleKeyGroup &&
                    x.LanguageId == property.LanguageId &&
                    x.LocaleValue == property.LocaleValue)
                .Select(x => new { x.Id, x.LocaleKeyGroup, x.EntityId })
                .ToListAsync(cancelToken);

            if (existingProps.Count == 0)
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
                    existingProps = await _db.LocalizedProperties
                        .ApplyStandardFilter(property.LanguageId, 0, otherKeyGroup, AliasLocaleKey)
                        .Where(x => x.LocaleValue == property.LocaleValue)
                        .Select(x => new { x.Id, x.LocaleKeyGroup, x.EntityId })
                        .ToListAsync(cancelToken);
                }

                if (existingProps.Count == 0)
                {
                    return false;
                }
            }

            var toDeleteIds = new HashSet<int>();

            foreach (var prop in existingProps)
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
                    entry.ResetState();
                    return true;
                }
                else
                {
                    // Delete accidentally dead localized properties in one go.
                    toDeleteIds.Add(prop.Id);
                }
            }

            if (toDeleteIds.Count > 0)
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

        /// <summary>
        /// Checks for a duplicate entity based on the given expression <paramref name="getDuplicate"/>.
        /// An error message is created if so.
        /// </summary>
        private async Task<bool> CheckEntityDuplicate<TEntity>(
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
                    entry.ResetState();
                    return true;
                }
            }

            return false;
        }

        private static bool IsPropertyModified(IHookedEntity entry, string propertyName)
        {
            if (entry.State == EState.Detached)
                return false;

            var prop = entry.Entry.Property(propertyName);
            if (prop == null)
                return false;

            if (entry.State == EState.Added)
            {
                // OriginalValues cannot be used for entities in the Added state.
                return prop.CurrentValue != null;
            }
            else if (entry.State == EState.Deleted)
            {
                // CurrentValues cannot be used for entities in the Deleted state.
                return prop.OriginalValue != null;
            }
            else
            {
                return (prop.CurrentValue != null && !prop.CurrentValue.Equals(prop.OriginalValue)) ||
                    (prop.OriginalValue != null && !prop.OriginalValue.Equals(prop.CurrentValue));
            }
        }

        private string CreateValueExistsMessage(string resourceKey, string checkedValue)
        {
            return T(resourceKey).Value.FormatInvariant(checkedValue.NaIfEmpty()) + " " + T("Common.Error.ChooseDifferentValue");
        }
    }
}
