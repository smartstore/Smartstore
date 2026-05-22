using FluentMigrator.Builders;
using FluentMigrator.Builders.Create.Table;
using FluentMigrator.Infrastructure;
using Smartstore.Data;

namespace FluentMigrator;

public static class FluentMigratorExtensions
{
    public static ICreateTableWithColumnSyntax WithIdColumn(this ICreateTableWithColumnSyntax root)
    {
        return root.WithColumn("Id")
            .AsInt32()
            .PrimaryKey()
            .Identity()
            .NotNullable();
    }

    public static TNext AsMaxString<TNext>(this IColumnTypeSyntax<TNext> root)
        where TNext : IFluentSyntax
    {
        return root.AsString(int.MaxValue);
    }

    public static TNext AsJson<TNext>(this IColumnTypeSyntax<TNext> root)
        where TNext : IFluentSyntax
    {
        return (DataSettings.Instance.DbFactory?.DbSystem) switch
        {
            DbSystemType.MySql => root.AsCustom("JSON"),
            DbSystemType.PostgreSql => root.AsCustom("JSONB"),
            _ => root.AsString(int.MaxValue)  // SQL Server: NVARCHAR(MAX), SQLite: TEXT
        };
    }
}