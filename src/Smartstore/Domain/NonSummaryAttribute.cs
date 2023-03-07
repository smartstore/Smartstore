namespace Smartstore.Domain
{
    /// <summary>
    /// Used to annotate entity properties with potentially large data (such as long text, BLOB, etc.).
    /// Properties annotated with this attribute will be excluded when the <c>SelectSummary</c> extension method
    /// is called to speed up the materialization of entity lists.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NonSummaryAttribute : Attribute
    {
    }
}
