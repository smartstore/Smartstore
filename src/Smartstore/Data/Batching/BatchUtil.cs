using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Smartstore.Data.Batching
{
    internal enum OperationType
    {
        Insert,
        InsertOrUpdate,
        InsertOrUpdateDelete,
        Update,
        Delete,
        Read
    }

    internal static class BatchUtil
    {
        static readonly int SelectStatementLength = "SELECT".Length;

        private static readonly BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Private(this object obj, string privateField) => obj?.GetType().GetField(privateField, bindingFlags)?.GetValue(obj);
        private static T Private<T>(this object obj, string privateField) => (T)obj?.GetType().GetField(privateField, bindingFlags)?.GetValue(obj);

        // In comment are Examples of how SqlQuery is changed for Sql Batch

        // SELECT [a].[Column1], [a].[Column2], .../r/n
        // FROM [Table] AS [a]/r/n
        // WHERE [a].[Column] = FilterValue
        // --
        // DELETE [a]
        // FROM [Table] AS [a]
        // WHERE [a].[Columns] = FilterValues
        public static (string, List<object>) GetSqlDelete(IQueryable query, HookingDbContext context)
        {
            (string sql, string tableAlias, string tableAliasSufixAs, string topStatement, string leadingComments, IEnumerable<object> parameters)
                = GetBatchSql(query, context, isUpdate: false);

            tableAlias = context.DataProvider.ProviderType == DataProviderType.Sqlite ? tableAlias : $"[{tableAlias}]";

            var resultQuery = $"{leadingComments}DELETE {topStatement}{tableAlias}{sql}";
            return (resultQuery, new List<object>(parameters));
        }

        // SELECT [a].[Column1], [a].[Column2], .../r/n
        // FROM [Table] AS [a]/r/n
        // WHERE [a].[Column] = FilterValue
        // --
        // UPDATE [a] SET [UpdateColumns] = N'updateValues'
        // FROM [Table] AS [a]
        // WHERE [a].[Columns] = FilterValues
        public static (string, List<object>) GetSqlUpdate(IQueryable query, HookingDbContext context, object updateValues, List<string> updateColumns)
        {
            (string sql, string tableAlias, string tableAliasSufixAs, string topStatement, string leadingComments, IEnumerable<object> parameters)
                = GetBatchSql(query, context, isUpdate: true);

            var sqlParameters = new List<object>(parameters);

            string sqlSET = GetSqlSetSegment(context, updateValues.GetType(), updateValues, updateColumns, sqlParameters);
            var resultQuery = $"{leadingComments}UPDATE {topStatement}{tableAlias}{tableAliasSufixAs} {sqlSET}{sql}";
            return (resultQuery, sqlParameters);
        }

        public static (string, List<object>) GetSqlUpdate<T>(IQueryable<T> query, HookingDbContext context, Expression<Func<T, T>> expression) where T : class
        {
            return GetSqlUpdate<T>(query, context, typeof(T), expression);
        }

        public static (string, List<object>) GetSqlUpdate(IQueryable query, HookingDbContext context, Type type, Expression<Func<object, object>> expression)
        {
            return GetSqlUpdate<object>(query, context, type, expression);
        }

        private static (string, List<object>) GetSqlUpdate<T>(IQueryable query, HookingDbContext context, Type type, Expression<Func<T, T>> expression) where T : class
        {
            (string sql, string tableAlias, string tableAliasSufixAs, string topStatement, string leadingComments, IEnumerable<object> parameters)
                = GetBatchSql(query, context, isUpdate: true);

            var sqlColumns = new StringBuilder();
            var sqlParameters = new List<object>(parameters);
            var columnNameValueDict = TableInfo.CreateInstance(query.GetDbContext(), type, new List<object>(), OperationType.Read, new BulkConfig()).PropertyColumnNamesDict;

            CreateUpdateBody(columnNameValueDict, tableAlias, expression.Body, context.DataProvider, ref sqlColumns, ref sqlParameters);

            sqlColumns = context.DataProvider.ProviderType == DataProviderType.Sqlite ? sqlColumns.Replace($"[{tableAlias}].", "") : sqlColumns;

            var resultQuery = $"{leadingComments}UPDATE {topStatement}{tableAlias}{tableAliasSufixAs} SET {sqlColumns} {sql}";
            return (resultQuery, sqlParameters);
        }

        public static (string, string, string, string, string, IEnumerable<object>) GetBatchSql(IQueryable query, HookingDbContext context, bool isUpdate)
        {
            var (fullSqlQuery, parameters) = ToParametrizedSql(query, context);

            var databaseType = context.DataProvider.ProviderType;
            var (leadingComments, sqlQuery) = SplitLeadingCommentsAndMainSqlQuery(fullSqlQuery);

            string tableAlias = string.Empty;
            string tableAliasSufixAs = string.Empty;
            string topStatement = string.Empty;
            if (databaseType != DataProviderType.Sqlite) // when Sqlite and Deleted metod tableAlias is Empty: ""
            {
                string escapeSymbolEnd = (databaseType == DataProviderType.SqlServer) ? "]" : "."; // SqlServer : PostgreSql;
                string escapeSymbolStart = (databaseType == DataProviderType.SqlServer) ? "[" : " "; // SqlServer : PostgreSql;
                string tableAliasEnd = sqlQuery[SelectStatementLength..sqlQuery.IndexOf(escapeSymbolEnd)]; // " TOP(10) [table_alias" / " [table_alias" : " table_alias"
                int tableAliasStartIndex = tableAliasEnd.IndexOf(escapeSymbolStart);
                tableAlias = tableAliasEnd.Substring(tableAliasStartIndex + escapeSymbolStart.Length); // "table_alias"
                topStatement = tableAliasEnd.Substring(0, tableAliasStartIndex).TrimStart(); // "TOP(10) " / if TOP not present in query this will be a Substring(0,0) == ""
            }

            int indexFROM = sqlQuery.IndexOf(Environment.NewLine);
            string sql = sqlQuery[indexFROM..];
            sql = sql.Contains("{") ? sql.Replace("{", "{{") : sql; // Curly brackets have to be escaped:
            sql = sql.Contains("}") ? sql.Replace("}", "}}") : sql; // https://github.com/aspnet/EntityFrameworkCore/issues/8820

            if (isUpdate && databaseType == DataProviderType.Sqlite)
            {
                var match = Regex.Match(sql, @"FROM (""[^""]+"")( AS ""[^""]+"")");
                tableAlias = match.Groups[1].Value;
                tableAliasSufixAs = match.Groups[2].Value;
                sql = sql[(match.Index + match.Length)..];
            }

            return (sql, tableAlias, tableAliasSufixAs, topStatement, leadingComments, parameters);
        }

        public static string GetSqlSetSegment(HookingDbContext context, Type updateValuesType, object updateValues, List<string> updateColumns, List<object> parameters)
        {
            var tableInfo = TableInfo.CreateInstance(context, updateValuesType, new List<object>(), OperationType.Read, new BulkConfig());
            return GetSqlSetSegment(context, tableInfo, updateValuesType, updateValues, Activator.CreateInstance(updateValuesType), updateColumns, parameters);
        }

        private static string GetSqlSetSegment(
            HookingDbContext context,
            TableInfo tableInfo,
            Type updateValuesType,
            object updateValues,
            object defaultValues,
            List<string> updateColumns,
            List<object> parameters)
        {
            string sql = string.Empty;
            foreach (var propertyNameColumnName in tableInfo.PropertyColumnNamesDict)
            {
                string propertyName = propertyNameColumnName.Key;
                string columnName = propertyNameColumnName.Value;
                var pArray = propertyName.Split(new char[] { '.' });
                Type lastType = updateValuesType;
                PropertyInfo property = lastType.GetProperty(pArray[0]);
                if (property != null)
                {
                    object propertyUpdateValue = property.GetValue(updateValues);
                    object propertyDefaultValue = property.GetValue(defaultValues);
                    for (int i = 1; i < pArray.Length; i++)
                    {
                        lastType = property.PropertyType;
                        property = lastType.GetProperty(pArray[i]);
                        propertyUpdateValue = propertyUpdateValue != null ? property.GetValue(propertyUpdateValue) : propertyUpdateValue;
                        var lastDefaultValues = lastType.Assembly.CreateInstance(lastType.FullName);
                        propertyDefaultValue = property.GetValue(lastDefaultValues);
                    }

                    if (tableInfo.ConvertibleProperties.ContainsKey(columnName))
                    {
                        propertyUpdateValue = tableInfo.ConvertibleProperties[columnName].ConvertToProvider.Invoke(propertyUpdateValue);
                    }

                    bool isDifferentFromDefault = propertyUpdateValue != null && propertyUpdateValue?.ToString() != propertyDefaultValue?.ToString();
                    if (isDifferentFromDefault || (updateColumns != null && updateColumns.Contains(propertyName)))
                    {
                        sql += $"[{columnName}] = @{columnName}, ";
                        propertyUpdateValue ??= DBNull.Value;
                        parameters.Add(context.DataProvider.CreateParameter($"@{columnName}", propertyUpdateValue));
                    }
                }
            }
            if (String.IsNullOrEmpty(sql))
            {
                throw new InvalidOperationException("SET Columns not defined. If one or more columns should be updated to theirs default value use 'updateColumns' argument.");
            }
            sql = sql.Remove(sql.Length - 2, 2); // removes last excess comma and space: ", "
            return $"SET {sql}";
        }

        /// <summary>
        /// Recursive analytic expression 
        /// </summary>
        public static void CreateUpdateBody(
            Dictionary<string, string> columnNameValueDict,
            string tableAlias,
            Expression expression,
            DataProvider dataProvider,
            ref StringBuilder sqlColumns,
            ref List<object> sqlParameters)
        {
            var dbType = dataProvider.ProviderType;

            if (expression is MemberInitExpression memberInitExpression)
            {
                foreach (var item in memberInitExpression.Bindings)
                {
                    if (item is MemberAssignment assignment)
                    {
                        if (columnNameValueDict.TryGetValue(assignment.Member.Name, out string value))
                            sqlColumns.Append($" [{tableAlias}].[{value}]");
                        else
                            sqlColumns.Append($" [{tableAlias}].[{assignment.Member.Name}]");

                        sqlColumns.Append(" =");

                        CreateUpdateBody(columnNameValueDict, tableAlias, assignment.Expression, dataProvider, ref sqlColumns, ref sqlParameters);

                        if (memberInitExpression.Bindings.IndexOf(item) < (memberInitExpression.Bindings.Count - 1))
                            sqlColumns.Append(" ,");
                    }
                }
            }
            else if (expression is MemberExpression memberExpression && memberExpression.Expression is ParameterExpression)
            {
                if (columnNameValueDict.TryGetValue(memberExpression.Member.Name, out string value))
                    sqlColumns.Append($" [{tableAlias}].[{value}]");
                else
                    sqlColumns.Append($" [{tableAlias}].[{memberExpression.Member.Name}]");
            }
            else if (expression is ConstantExpression constantExpression)
            {
                var parmName = $"param_{sqlParameters.Count}";
                sqlParameters.Add(dataProvider.CreateParameter(parmName, constantExpression.Value ?? DBNull.Value));
                sqlColumns.Append($" @{parmName}");
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                switch (unaryExpression.NodeType)
                {
                    case ExpressionType.Convert:
                        CreateUpdateBody(columnNameValueDict, tableAlias, unaryExpression.Operand, dataProvider, ref sqlColumns, ref sqlParameters);
                        break;
                    case ExpressionType.Not:
                        sqlColumns.Append(" ~");//this way only for SQL Server 
                        CreateUpdateBody(columnNameValueDict, tableAlias, unaryExpression.Operand, dataProvider, ref sqlColumns, ref sqlParameters);
                        break;
                    default: break;
                }
            }
            else if (expression is BinaryExpression binaryExpression)
            {
                CreateUpdateBody(columnNameValueDict, tableAlias, binaryExpression.Left, dataProvider, ref sqlColumns, ref sqlParameters);

                switch (binaryExpression.NodeType)
                {
                    case ExpressionType.Add:
                        sqlColumns.Append(dbType == DataProviderType.Sqlite && IsStringConcat(binaryExpression) ? " ||" : " +");
                        break;
                    case ExpressionType.Divide:
                        sqlColumns.Append(" /");
                        break;
                    case ExpressionType.Multiply:
                        sqlColumns.Append(" *");
                        break;
                    case ExpressionType.Subtract:
                        sqlColumns.Append(" -");
                        break;
                    case ExpressionType.And:
                        sqlColumns.Append(" &");
                        break;
                    case ExpressionType.Or:
                        sqlColumns.Append(" |");
                        break;
                    case ExpressionType.ExclusiveOr:
                        sqlColumns.Append(" ^");
                        break;
                    default: break;
                }

                CreateUpdateBody(columnNameValueDict, tableAlias, binaryExpression.Right, dataProvider, ref sqlColumns, ref sqlParameters);
            }
            else
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                var parmName = $"param_{sqlParameters.Count}";
                sqlParameters.Add(dataProvider.CreateParameter(parmName, value ?? DBNull.Value));
                sqlColumns.Append($" @{parmName}");
            }
        }

        internal static bool IsStringConcat(BinaryExpression binaryExpression)
        {
            var methodProperty = binaryExpression.GetType().GetProperty("Method");
            if (methodProperty == null)
            {
                return false;
            }
            var method = methodProperty.GetValue(binaryExpression) as MethodInfo;
            if (method == null)
            {
                return false;
            }
            return method.DeclaringType == typeof(string) && method.Name == nameof(string.Concat);
        }

        public static (string, string) SplitLeadingCommentsAndMainSqlQuery(string sqlQuery)
        {
            var leadingCommentsBuilder = new StringBuilder();
            var mainSqlQuery = sqlQuery;
            while (!string.IsNullOrWhiteSpace(mainSqlQuery)
                && !mainSqlQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                if (mainSqlQuery.StartsWith("--"))
                {
                    // pull off line comment
                    var indexOfNextNewLine = mainSqlQuery.IndexOf(Environment.NewLine);
                    if (indexOfNextNewLine > -1)
                    {
                        leadingCommentsBuilder.Append(mainSqlQuery.Substring(0, indexOfNextNewLine + Environment.NewLine.Length));
                        mainSqlQuery = mainSqlQuery[(indexOfNextNewLine + Environment.NewLine.Length)..];
                        continue;
                    }
                }

                if (mainSqlQuery.StartsWith("/*"))
                {
                    var nextBlockCommentEndIndex = mainSqlQuery.IndexOf("*/");
                    if (nextBlockCommentEndIndex > -1)
                    {
                        leadingCommentsBuilder.Append(mainSqlQuery.Substring(0, nextBlockCommentEndIndex + 2));
                        mainSqlQuery = mainSqlQuery.Substring(nextBlockCommentEndIndex + 2);
                        continue;
                    }
                }

                var nextNonWhitespaceIndex = Array.FindIndex(mainSqlQuery.ToCharArray(), x => !char.IsWhiteSpace(x));

                if (nextNonWhitespaceIndex > 0)
                {
                    leadingCommentsBuilder.Append(mainSqlQuery.Substring(0, nextNonWhitespaceIndex));
                    mainSqlQuery = mainSqlQuery.Substring(nextNonWhitespaceIndex);
                    continue;
                }

                // Fallback... just find the first index of SELECT
                var selectIndex = mainSqlQuery.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
                if (selectIndex > 0)
                {
                    leadingCommentsBuilder.Append(mainSqlQuery.Substring(0, selectIndex));
                    mainSqlQuery = mainSqlQuery.Substring(selectIndex);
                }

                break;
            }

            return (leadingCommentsBuilder.ToString(), mainSqlQuery);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Heavy stuff")]
        private static (string, IEnumerable<object>) ToParametrizedSql(IQueryable query, HookingDbContext context)
        {
            string relationalCommandCacheText = "_relationalCommandCache";
            string selectExpressionText = "_selectExpression";
            string querySqlGeneratorFactoryText = "_querySqlGeneratorFactory";
            string relationalQueryContextText = "_relationalQueryContext";

            string cannotGetText = "Cannot get";

            var dataProvider = context.DataProvider;
            var enumerator = query.Provider.Execute<IEnumerable>(query.Expression).GetEnumerator();
            var relationalCommandCache = enumerator.Private(relationalCommandCacheText) as RelationalCommandCache;
            var queryContext = enumerator.Private<RelationalQueryContext>(relationalQueryContextText) ?? throw new InvalidOperationException($"{cannotGetText} {relationalQueryContextText}");
            var parameterValues = queryContext.ParameterValues;

            string sql;
            IList<DbParameter> parameters;

            if (relationalCommandCache != null)
            {
                var command = relationalCommandCache.GetRelationalCommand(parameterValues);
                var parameterNames = new HashSet<string>(command.Parameters.Select(p => p.InvariantName));
                sql = command.CommandText;
                parameters = parameterValues.Where(pv => parameterNames.Contains(pv.Key)).Select(pv => dataProvider.CreateParameter("@" + pv.Key, pv.Value)).ToList();
            }
            else
            {
                SelectExpression selectExpression = enumerator.Private<SelectExpression>(selectExpressionText) ?? throw new InvalidOperationException($"{cannotGetText} {selectExpressionText}");
                IQuerySqlGeneratorFactory factory = enumerator.Private<IQuerySqlGeneratorFactory>(querySqlGeneratorFactoryText) ?? throw new InvalidOperationException($"{cannotGetText} {querySqlGeneratorFactoryText}");

                var sqlGenerator = factory.Create();
                var command = sqlGenerator.GetCommand(selectExpression);
                sql = command.CommandText;
                parameters = parameterValues.Select(pv => dataProvider.CreateParameter("@" + pv.Key, pv.Value)).ToList();
            }

            return (sql, parameters);
        }
    }
}