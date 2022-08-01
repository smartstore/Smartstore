namespace Smartstore.ComponentModel
{
    public interface IMapper<in TFrom, in TTo>
        where TFrom : class
        where TTo : class
    {
        /// <summary>
        /// Maps the specified source object into the destination object.
        /// </summary>
        /// <param name="from">The source object to map from.</param>
        /// <param name="to">The destination object to map to.</param>
        /// <param name="parameters">Custom parameters</param>
        Task MapAsync(TFrom from, TTo to, dynamic parameters = null);
    }

    public abstract class Mapper<TFrom, TTo> : IMapper<TFrom, TTo>
        where TFrom : class
        where TTo : class
    {
        protected abstract void Map(TFrom from, TTo to, dynamic parameters = null);

        public virtual Task MapAsync(TFrom from, TTo to, dynamic parameters = null)
        {
            Map(from, to, null);
            return Task.CompletedTask;
        }
    }

    public static class IMapperExtensions
    {
        /// <summary>
        /// Maps the specified source object to a new object with a type of <typeparamref name="TTo"/>.
        /// </summary>
        /// <typeparam name="TFrom">The type of the source object.</typeparam>
        /// <typeparam name="TTo">The type of the destination object.</typeparam>
        /// <param name="mapper">The mapper.</param>
        /// <param name="from">The source object.</param>
        /// <param name="parameters">Custom parameters</param>
        /// <returns>The mapped object of type <typeparamref name="TTo"/>.</returns>
        public static async Task<TTo> MapAsync<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, TFrom from, dynamic parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            Guard.NotNull(mapper, nameof(mapper));
            Guard.NotNull(from, nameof(from));

            var to = Activator.CreateInstance<TTo>();
            await mapper.MapAsync(from, to, parameters);
            return to;
        }

        /// <summary>
        /// Maps a collection of <typeparamref name="TFrom"/> into an array of <typeparamref name="TTo"/>.
        /// </summary>
        /// <typeparam name="TFrom">The type of the source objects.</typeparam>
        /// <typeparam name="TTo">The type of the destination objects.</typeparam>
        /// <param name="mapper">The mapper.</param>
        /// <param name="from">The source collection.</param>
        /// <param name="parameters">Custom parameters</param>
        /// <returns>An array of <typeparamref name="TTo"/>.</returns>
        public static async Task<TTo[]> MapArrayAsync<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, IEnumerable<TFrom> from, dynamic parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            Guard.NotNull(mapper, nameof(mapper));
            Guard.NotNull(from, nameof(from));

            return await from
                .SelectAwait<TFrom, TTo>(async x => await MapAsync(mapper, x, parameters))
                .AsyncToArray();
        }

        /// <summary>
        /// Maps a collection of <typeparamref name="TFrom"/> into a list of <typeparamref name="TTo"/>.
        /// </summary>
        /// <typeparam name="TFrom">The type of the source objects.</typeparam>
        /// <typeparam name="TTo">The type of the destination objects.</typeparam>
        /// <param name="mapper">The mapper.</param>
        /// <param name="from">The source collection.</param>
        /// <param name="parameters">Custom parameters</param>
        /// <returns>A list of <typeparamref name="TTo"/>.</returns>
        public static async Task<List<TTo>> MapListAsync<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, IEnumerable<TFrom> from, dynamic parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            Guard.NotNull(mapper, nameof(mapper));
            Guard.NotNull(from, nameof(from));

            return await from
                .SelectAwait<TFrom, TTo>(async x => await MapAsync<TFrom, TTo>(mapper, x, parameters))
                .AsyncToList();
        }

        /// <summary>
        /// Maps a collection of <typeparamref name="TFrom"/> into a list of <typeparamref name="TTo"/>.
        /// </summary>
        /// <typeparam name="TFrom">The type of the source objects.</typeparam>
        /// <typeparam name="TTo">The type of the destination objects.</typeparam>
        /// <param name="mapper">The mapper.</param>
        /// <param name="from">The source collection.</param>
        /// <param name="to">The destination collection.</param>
        /// <param name="parameters">Custom parameters</param>
        /// <returns>A list of <typeparamref name="TTo"/>.</returns>
        public static async Task MapCollectionAsync<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, IEnumerable<TFrom> from, ICollection<TTo> to, dynamic parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            Guard.NotNull(mapper, nameof(mapper));
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            to.Clear();
            var items = await MapArrayAsync(mapper, from, (object)parameters);
            to.AddRange(items);
        }
    }
}
