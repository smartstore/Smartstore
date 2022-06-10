namespace Smartstore.Core.Search
{
    public class SearchField
    {
        public SearchField(string name, float boost = 0f)
        {
            Guard.NotEmpty(name, nameof(name));

            Name = name;
            Boost = boost;
        }

        public string Name { get; }
        public float Boost { get; }
    }
}
