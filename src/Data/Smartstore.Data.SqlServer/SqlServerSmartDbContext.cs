using System;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;

namespace Smartstore.Data.SqlServer
{
    public class SqlServerSmartDbContext : SmartDbContext
    {
        public SqlServerSmartDbContext(DbContextOptions<SmartDbContext> options)
            : base(options)
        {
        }
    }
}
