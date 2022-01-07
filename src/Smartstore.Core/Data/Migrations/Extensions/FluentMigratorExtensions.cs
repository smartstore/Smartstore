using FluentMigrator.Builders.Create.Table;

namespace FluentMigrator
{
    public static class FluentMigratorExtensions
    {
        public static ICreateTableWithColumnSyntax WithIdColumn(this ICreateTableWithColumnSyntax root)
        {
            return root.WithColumn("Id").AsInt32().PrimaryKey().Identity().NotNullable();
        }

        public static ICreateTableColumnOptionOrWithColumnSyntax AsMaxString(this ICreateTableColumnAsTypeSyntax root)
        {
            return root.AsString(int.MaxValue);
        }
    }
}
