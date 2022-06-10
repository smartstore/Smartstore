namespace Smartstore.Core.Content.Menus
{
    public class ModifiedParameter
    {
        public ModifiedParameter() : this(null)
        {
        }
        public ModifiedParameter(string name)
        {
            Name = name;
            BooleanParamNames = new List<string>();
        }

        public bool HasValue()
        {
            return !Name.IsEmpty();
        }

        public string Name { get; set; }
        public object Value { get; set; }
        // little hack here due to ugly MVC implementation
        // find more info here: http://www.mindstorminteractive.com/blog/topics/jquery-fix-asp-net-mvc-checkbox-truefalse-value/
        public IList<string> BooleanParamNames { get; private set; }
    }
}
