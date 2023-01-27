using System.Data;
using System.Linq;
using FluentMigrator.Expressions;
using FluentMigrator.Runner.Conventions;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Data.PostgreSql.Migration
{
    internal class PostgreSqlConventionSource : IConventionSource
    {
        public void Configure(IConventionSet conventionSet)
        {
            conventionSet.ColumnsConventions.Add(new CitextColumnsConvention());
        }
    }

    internal class CitextColumnsConvention : IColumnsConvention
    {
        public IColumnsExpression Apply(IColumnsExpression expression)
        {
            foreach (var column in expression.Columns)
            {
                if (column.Type == DbType.String)
                {
                    column.Type = null;
                    column.CustomType = "citext";
                }
            }

            return expression;
        }
    }
}
