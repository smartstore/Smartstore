using System;
using Smartstore.Web.Modelling;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Models.Theming
{
    public partial class ThemeVariableModel : ModelBase
    {
        public ThemeVariableModel(ThemeVariableInfo info, object value)
        {
            Info = Guard.NotNull(info, nameof(info));
            Value = value;
        }

        public ThemeVariableInfo Info { get; }
        public object Value { get; set; }
    }
}