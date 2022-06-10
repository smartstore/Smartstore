namespace Smartstore.ComponentModel
{
    /// <summary>
    /// Holder for untyped object instances.
    /// </summary>
    public interface IObjectContainer
    {
        Type ValueType { get; set; }

        object Value { get; set; }
    }
}