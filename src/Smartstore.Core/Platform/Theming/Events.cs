namespace Smartstore.Core.Theming
{
    public class ThemeTouchedEvent
    {
        public ThemeTouchedEvent(string themeName)
        {
            Guard.NotEmpty(themeName, nameof(themeName));
            this.ThemeName = themeName;
        }

        public string ThemeName { get; set; }
    }

    public class ThemeSwitchedEvent
    {
        public string OldTheme { get; set; }
        public string NewTheme { get; set; }
    }
}
