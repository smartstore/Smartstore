using System.Data;
using System.Linq;
using FluentMigrator.Expressions;
using FluentMigrator.Runner.Conventions;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Data.PostgreSql.Migration
{
    internal class PostgreSqlConventionProvider : IConventionProvider
    {
        public void Configure(IConventionSet conventionSet)
        {
            if (conventionSet.ColumnsConventions.FirstOrDefault(x => x is CiTextColumnsConvention) == null)
            {
                conventionSet.ColumnsConventions.Add(new CiTextColumnsConvention());
            }
        }
    }

    internal class CiTextColumnsConvention : IColumnsConvention
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
