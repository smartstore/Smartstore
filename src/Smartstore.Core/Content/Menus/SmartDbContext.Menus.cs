using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Content.Menus;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Menu> Menus { get; set; }

        public DbSet<MenuItem> MenuItems { get; set; }
    }
}