using FluentMigrator;
using FluentMigrator.Builders.Create.Index;
using FluentMigrator.Expressions;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.SqlServer;
using FluentMigrator.SqlServer;
using NUnit.Framework;

namespace Smartstore.Core.Tests.Platform.Identity;

/// <summary>
/// Verifies the SQL Server provider SQL text without claiming execution against
/// a live SQL Server engine. Database integration is a separate check.
/// </summary>
[TestFixture]
public class CustomerUsernameSqlServerSqlGenerationTests
{
    [Test]
    public void Generates_nonunique_rollback_index_for_sql_server()
    {
        var expression = new CreateIndexExpression
        {
            Index = new IndexDefinition { Name = "IX_Customer_Username" }
        };
        var builder = new CreateIndexExpressionBuilder(expression);
        builder.OnTable("Customer").OnColumn("Username").Ascending();

        var sql = new SqlServer2014Generator().Generate(expression);
        TestContext.Progress.WriteLine("SQL SERVER ROLLBACK SQL: " + sql);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("CREATE INDEX [IX_Customer_Username]"));
            Assert.That(sql, Does.Contain("ON [dbo].[Customer]"));
            Assert.That(sql, Does.Not.Contain("UNIQUE"));
            Assert.That(sql, Does.Not.Contain("WHERE"));
        }
    }

    [Test]
    public void Generates_filtered_unique_index_with_null_exclusion_for_sql_server()
    {
        var expression = new CreateIndexExpression
        {
            Index = new IndexDefinition { Name = "IX_Customer_Username" }
        };
        var builder = new CreateIndexExpressionBuilder(expression);
        builder.OnTable("Customer")
            .OnColumn("Username").Ascending()
            .WithOptions().Unique()
            .WithOptions().Filter("([Username] IS NOT NULL)");

        var generator = new SqlServer2014Generator();
        var sql = generator.Generate(expression);
        TestContext.Progress.WriteLine("SQL SERVER INDEX SQL: " + sql);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("UNIQUE"));
            Assert.That(sql, Does.Contain("[Customer]"));
            Assert.That(sql, Does.Contain("[Username]"));
            Assert.That(sql, Does.Contain("WHERE"));
            Assert.That(sql, Does.Contain("IS NOT NULL"));
        }
    }
}
