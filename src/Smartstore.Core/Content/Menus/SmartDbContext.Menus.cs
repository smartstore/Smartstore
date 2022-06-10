using Smartstore.Core.Content.Menus;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<MenuEntity> Menus { get; set; }
        public DbSet<MenuItemEntity> MenuItems { get; set; }
    }
}