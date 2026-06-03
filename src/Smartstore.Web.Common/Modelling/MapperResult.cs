#nullable enable

namespace Smartstore.Web.Modelling;

/// <summary>
/// Helper class to return both the model and the data from a mapping operation.
/// </summary>
public record class MapperResult<TModel, TData>(TModel Model, TData Data)
    where TModel : ModelBase
{
    public TModel Model { get; } = Model;
    public TData Data { get; } = Data;
}
