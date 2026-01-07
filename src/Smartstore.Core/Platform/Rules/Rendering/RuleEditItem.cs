namespace Smartstore.Core.Rules.Rendering
{
    /// <summary>
    /// Represents an edit item for rules.
    /// </summary>
    public class RuleEditItem
    {
        public int RuleId { get; set; }
        public string Op { get; set; }
        public string Value { get; set; }
        public string Error { get; set; }
    }
}
