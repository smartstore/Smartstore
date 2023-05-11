namespace Smartstore.ComponentModel
{
    public interface IMapperCallback<in TFrom, in TTo>
        where TFrom : class
        where TTo : class
    {
        Task MapCallbackAsync(TFrom from, TTo to, dynamic parameters = null);
    }

    public abstract class MapperCallback<TFrom, TTo> : IMapperCallback<TFrom, TTo>
        where TFrom : class
        where TTo : class
    {
        public abstract void MapCallback(TFrom from, TTo to, dynamic parameters = null);

        public virtual Task MapCallbackAsync(TFrom from, TTo to, dynamic parameters = null)
        {
            MapCallback(from, to, null);
            return Task.CompletedTask;
        }
    }
}