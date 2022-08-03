namespace Smartstore.Core.Theming
{
    public class ThemeConfiguration
    {
        public class VariableConfiguration
        {
            public string Type { get; set; }
            public string Value { get; set; }
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public string BaseTheme { get; set; }
        public string PreviewImagePath { get; set; }

        public Dictionary<string, VariableConfiguration> Variables { get; set; }
        public Dictionary<string, string[]> Selects { get; set; }
    }
}
