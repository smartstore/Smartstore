using Smartstore.Core.Theming;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<ThemeVariable> ThemeVariables { get; set; }
    }
}