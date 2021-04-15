using System;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;

namespace Smartstore.Data.MySql
{
    public class MySqlSmartDbContext : SmartDbContext
    {
        public MySqlSmartDbContext(DbContextOptions<MySqlSmartDbContext> options)
            : base(options)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetInvariantType()
            => typeof(SmartDbContext);
    }
}
