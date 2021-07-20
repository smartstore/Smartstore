using System.Diagnostics;

namespace Smartstore.Core.Theming
{
    public enum ThemeVariableType
    {
        String,
        Color,
        Number,
        Boolean,
        Select
    }

    /// <summary>
    /// Represents deserialized metadata for a theme variable
    /// </summary>
    [DebuggerDisplay("{Name}, Default: {DefaultValue}, Type: {TypeAsString}")]
    public class ThemeVariableInfo : Disposable
    {
        /// <summary>
        /// Gets the variable name as specified in the config file
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the default variable value as specified in the config file
        /// </summary>
        public string DefaultValue { get; internal set; }

        /// <summary>
        /// Gets the variable type as specified in the config file
        /// </summary>
        public ThemeVariableType Type { get; internal set; }

        public string TypeAsString
        {
            get
            {
                if (Type != ThemeVariableType.Select)
                {
                    return Type.ToString();
                }

                return "Select#" + this.SelectRef;
            }
        }

        /// <summary>
        /// Gets the id of the select element or <c>null</c>,
        /// if the variable is not a select type.
        /// </summary>
        public string SelectRef { get; internal set; }

        /// <summary>
        /// Gets the theme descriptor the variable belongs to
        /// </summary>
        public ThemeDescriptor ThemeDescriptor { get; internal set; }


        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                ThemeDescriptor = null;
            }
        }
    }
}