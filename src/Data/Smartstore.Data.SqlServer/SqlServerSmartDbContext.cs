using System;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;

namespace Smartstore.Data.SqlServer
{
    public class SqlServerSmartDbContext : SmartDbContext
    {
        public SqlServerSmartDbContext(DbContextOptions<SqlServerSmartDbContext> options)
            : base(options)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Type GetInvariantType()
            => typeof(SmartDbContext);
    }
}
