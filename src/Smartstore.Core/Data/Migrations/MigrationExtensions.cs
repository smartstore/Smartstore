using System.Runtime.CompilerServices;
using FluentMigrator;
using Smartstore.Data;

namespace Smartstore.Core.Data.Migrations
{
    public static partial class MigrationExtensions
    {
        /// <summary>
        /// Gets a value indicating whether a table exists.
        /// </summary>
        /// <param name="migration">Migration.</param>
        /// <param name="dbSystem">The database system name, see <see cref="DbSystemType"/>.</param>
        /// <param name="table">The table name.</param>
        /// <returns>A value indicating whether a table exists.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasTable(this Migration migration, string dbSystem, string table)
        {
            return migration.IfDatabase(dbSystem).Schema.Table(table).Exists();
        }

        /// <summary>
        /// Gets a value indicating whether a column exists.
        /// </summary>
        /// <param name="migration">Migration.</param>
        /// <param name="dbSystem">The database system name, see <see cref="DbSystemType"/>.</param>
        /// <param name="table">The table name.</param>
        /// <param name="column">The column name.</param>
        /// <returns>A value indicating whether a column exists.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasColumn(this Migration migration, string dbSystem, string table, string column)
        {
            return migration.IfDatabase(dbSystem).Schema.Table(table).Column(column).Exists();
        }

        /// <summary>
        /// Gets a value indicating whether an index exists.
        /// </summary>
        /// <param name="migration">Migration.</param>
        /// <param name="dbSystem">The database system name, see <see cref="DbSystemType"/>.</param>
        /// <param name="table">The table name.</param>
        /// <param name="index">The index name.</param>
        /// <returns>A value indicating whether an index exists.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasIndex(this Migration migration, string dbSystem, string table, string index)
        {
            return migration.IfDatabase(dbSystem).Schema.Table(table).Index(index).Exists();
        }

        /// <summary>
        /// Gets a value indicating whether a foreign key exists.
        /// </summary>
        /// <param name="migration">Migration.</param>
        /// <param name="dbSystem">The database system name, see <see cref="DbSystemType"/>.</param>
        /// <param name="table">The table name.</param>
        /// <param name="foreignKey">The foreign key name.</param>
        /// <returns>A value indicating whether a foreign key exists.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasForeignKey(this Migration migration, string dbSystem, string table, string foreignKey)
        {
            return migration.IfDatabase(dbSystem).Schema.Table(table).Constraint(foreignKey).Exists();
        }

        /// <summary>
        /// Deletes tables if they exist. 
        /// </summary>
        /// <param name="migration">Migration.</param>
        /// <param name="dbSystem">The database system name, see <see cref="DbSystemType"/>.</param>
        /// <param name="tables">Table names.</param>
        /// <returns>Number of deleted tables.</returns>
        public static int DeleteTables(this Migration migration, string dbSystem, params string[] tables)
        {
            var num = 0;

            foreach (var name in tables)
            {
                if (migration.HasTable(dbSystem, name))
                {
                    migration.Delete.Table(name);
                    num++;
                }
            }

            return num;
        }

        /// <summary>
        /// Deletes columns if they exist. 
        /// </summary>
        /// <param name="migration">Migration.</param>
        /// <param name="dbSystem">The database system name, see <see cref="DbSystemType"/>.</param>
        /// <param name="table">The table name.</param>
        /// <param name="columns">Column names.</param>
        /// <returns>Number of deleted columns.</returns>
        public static int DeleteColumns(this Migration migration, string dbSystem, string table, params string[] columns)
        {
            var num = 0;

            foreach (var name in columns)
            {
                if (migration.HasColumn(dbSystem, table, name))
                {
                    migration.Delete.Column(name);
                    num++;
                }
            }

            return num;
        }
    }
}
