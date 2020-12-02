using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Smartstore.Core.Common.Services;
using Smartstore.Domain;
using Smartstore.Engine;

namespace Smartstore
{
    public static class GenericAttributeExtensions
    {
        /// <summary>
        /// Gets an entity generic attribute value
        /// </summary>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
		/// <param name="storeId">Load a value specific for a certain store; pass 0 to load a value shared for all stores</param>
        /// <returns>Attribute value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TProp GetAttribute<TProp>(this BaseEntity entity, string key, int storeId = 0)
        {
            return GetAttribute<TProp>(entity, key, null, storeId);
        }

        /// <summary>
        /// Gets an entity specific generic attribute value
        /// </summary>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="genericAttributeService">GenericAttributeService</param>
        /// <param name="storeId">Load a value specific for a certain store; pass 0 to load a value shared for all stores</param>
        /// <returns>Attribute value</returns>
        public static TProp GetAttribute<TProp>(this BaseEntity entity, string key, IGenericAttributeService genericAttributeService, int storeId = 0)
        {
            Guard.NotNull(entity, nameof(entity));

            genericAttributeService ??= EngineContext.Current.Scope.ResolveOptional<IGenericAttributeService>();
            if (genericAttributeService == null)
            {
                return default;
            }

            return genericAttributeService.GetAttribute<TProp>(
                entity.GetEntityName(),
                entity.Id,
                key,
                storeId);
        }

        /// <summary>
        /// Gets an entity generic attribute value
        /// </summary>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="storeId">Load a value specific for a certain store; pass 0 to load a value shared for all stores</param>
        /// <returns>Attribute value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TProp> GetAttributeAsync<TProp>(this BaseEntity entity, string key, int storeId = 0)
        {
            return GetAttributeAsync<TProp>(entity, key, null, storeId);
        }

        /// <summary>
        /// Gets an entity specific generic attribute value
        /// </summary>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="genericAttributeService">GenericAttributeService</param>
        /// <param name="storeId">Load a value specific for a certain store; pass 0 to load a value shared for all stores</param>
        /// <returns>Attribute value</returns>
        public static Task<TProp> GetAttributeAsync<TProp>(this BaseEntity entity, string key, IGenericAttributeService genericAttributeService, int storeId = 0)
        {
            Guard.NotNull(entity, nameof(entity));

            genericAttributeService ??= EngineContext.Current.Scope.ResolveOptional<IGenericAttributeService>();
            if (genericAttributeService == null)
            {
                return Task.FromResult(default(TProp));
            }

            return genericAttributeService.GetAttributeAsync<TProp>(
                entity.GetEntityName(),
                entity.Id,
                key,
                storeId);
        }
    }
}