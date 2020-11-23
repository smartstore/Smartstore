using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Products;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Product> Products { get; set; }
    }
}