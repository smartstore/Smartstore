using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Product> Products { get; set; }
    }
}