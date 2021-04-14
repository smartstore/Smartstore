using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Data.Providers
{
    public class DbFactoryDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<DbFactoryDbContextOptionsBuilder, DbFactoryOptionsExtension>
    {
        public DbFactoryDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
            : base(optionsBuilder)
        {
            Guard.NotNull(optionsBuilder, nameof(optionsBuilder));
        }

        /// <summary>
        /// TODO: (core) ...
        /// </summary>
        public virtual DbFactoryDbContextOptionsBuilder Something(bool something)
            => WithOption(e => e.WithSomething(something));
    }
}
