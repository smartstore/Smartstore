using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.ComponentModel;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class DatabaseFacadeExtensions
    {
        #region Database creation

        /// <summary>
        /// Ensures that the database for the context exists. If it exists, no action is taken. If it does not
        /// exist then the database is created WITHOUT populating the schema.
        /// </summary>
        /// <param name="databaseFacade">The <see cref="DatabaseFacade" /> for the context.</param>
        /// <returns>
        /// <see langword="true" /> if the database is created, <see langword="false" /> if it already existed.
        /// </returns>
        public static bool EnsureCreatedSchemaless(this DatabaseFacade databaseFacade)
        {
            Guard.NotNull(databaseFacade);

            if (GetFacadeDependencies(databaseFacade).DatabaseCreator is RelationalDatabaseCreator creator)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!creator.Exists())
                    {
                        creator.Create();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Ensures that the database for the context exists. If it exists, no action is taken. If it does not
        /// exist then the database is created WITHOUT populating the schema.
        /// </summary>
        /// <param name="databaseFacade">The <see cref="DatabaseFacade" /> for the context.</param>
        /// <returns>
        /// <see langword="true" /> if the database is created, <see langword="false" /> if it already existed.
        /// </returns>
        public static async Task<bool> EnsureCreatedSchemalessAsync(this DatabaseFacade databaseFacade, CancellationToken cancelToken = default)
        {
            Guard.NotNull(databaseFacade);

            var creator = GetFacadeDependencies(databaseFacade).DatabaseCreator as RelationalDatabaseCreator;
            if (creator != null)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!await creator.ExistsAsync(cancelToken))
                    {
                        await creator.CreateAsync(cancelToken);
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region ExecuteScalar

        public static T ExecuteScalarInterpolated<T>(this DatabaseFacade databaseFacade, FormattableString sql)
            => ExecuteScalarRaw<T>(databaseFacade, sql.Format, sql.GetArguments());

        public static Task<T> ExecuteScalarInterpolatedAsync<T>(this DatabaseFacade databaseFacade, FormattableString sql, CancellationToken cancelToken = default)
            => ExecuteScalarRawAsync<T>(databaseFacade, sql.Format, sql.GetArguments(), cancelToken);

        public static T ExecuteScalarRaw<T>(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
            => ExecuteScalarRaw<T>(databaseFacade, sql, parameters.AsEnumerable());

        public static T ExecuteScalarRaw<T>(this DatabaseFacade databaseFacade, string sql, IEnumerable<object> parameters)
        {
            Guard.NotNull(databaseFacade);
            Guard.NotEmpty(sql);
            Guard.NotNull(parameters);

            var facadeDependencies = GetFacadeDependencies(databaseFacade);
            var concurrencyDetector = facadeDependencies.ConcurrencyDetector;
            var logger = facadeDependencies.CommandLogger;

            using (concurrencyDetector.EnterCriticalSection())
            {
                var rawSqlCommand = facadeDependencies.RawSqlCommandBuilder
                    .Build(sql, parameters);

                var scalarValue = rawSqlCommand
                    .RelationalCommand
                    .ExecuteScalar(
                        new RelationalCommandParameterObject(
                            facadeDependencies.RelationalConnection,
                            rawSqlCommand.ParameterValues,
                            null,
                            ((IDatabaseFacadeDependenciesAccessor)databaseFacade).Context,
                            logger));

                return scalarValue.Convert<T>();
            }
        }

        public static Task<T> ExecuteScalarRawAsync<T>(this DatabaseFacade databaseFacade, string sql, CancellationToken cancelToken)
            => ExecuteScalarRawAsync<T>(databaseFacade, sql, Enumerable.Empty<object>(), cancelToken);

        public static Task<T> ExecuteScalarRawAsync<T>(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
            => ExecuteScalarRawAsync<T>(databaseFacade, sql, parameters.AsEnumerable());

        public static async Task<T> ExecuteScalarRawAsync<T>(
            this DatabaseFacade databaseFacade,
            string sql,
            IEnumerable<object> parameters,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(databaseFacade);
            Guard.NotEmpty(sql);
            Guard.NotNull(parameters);

            var facadeDependencies = GetFacadeDependencies(databaseFacade);
            var concurrencyDetector = facadeDependencies.ConcurrencyDetector;
            var logger = facadeDependencies.CommandLogger;

            using (concurrencyDetector.EnterCriticalSection())
            {
                var rawSqlCommand = facadeDependencies.RawSqlCommandBuilder
                    .Build(sql, parameters);

                var scalarValue = await rawSqlCommand
                    .RelationalCommand
                    .ExecuteScalarAsync(
                        new RelationalCommandParameterObject(
                            facadeDependencies.RelationalConnection,
                            rawSqlCommand.ParameterValues,
                            null,
                            ((IDatabaseFacadeDependenciesAccessor)databaseFacade).Context,
                            logger), cancelToken)
                    .ConfigureAwait(false);

                return scalarValue.Convert<T>();
            }
        }

        #endregion

        #region ExecuteReader

        public static RelationalDataReader ExecuteReaderInterpolated(this DatabaseFacade databaseFacade, FormattableString sql)
            => ExecuteReaderRaw(databaseFacade, sql.Format, sql.GetArguments());

        public static Task<RelationalDataReader> ExecuteReaderInterpolatedAsync(this DatabaseFacade databaseFacade, FormattableString sql, CancellationToken cancelToken = default)
            => ExecuteReaderRawAsync(databaseFacade, sql.Format, sql.GetArguments(), cancelToken);

        public static RelationalDataReader ExecuteReaderRaw(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
            => ExecuteReaderRaw(databaseFacade, sql, parameters.AsEnumerable());

        public static RelationalDataReader ExecuteReaderRaw(this DatabaseFacade databaseFacade, string sql, IEnumerable<object> parameters)
        {
            Guard.NotNull(databaseFacade);
            Guard.NotEmpty(sql);
            Guard.NotNull(parameters);

            var facadeDependencies = GetFacadeDependencies(databaseFacade);
            var concurrencyDetector = facadeDependencies.ConcurrencyDetector;
            var logger = facadeDependencies.CommandLogger;

            using (concurrencyDetector.EnterCriticalSection())
            {
                var rawSqlCommand = facadeDependencies.RawSqlCommandBuilder
                    .Build(sql, parameters);

                return rawSqlCommand
                    .RelationalCommand
                    .ExecuteReader(
                        new RelationalCommandParameterObject(
                            facadeDependencies.RelationalConnection,
                            rawSqlCommand.ParameterValues,
                            null,
                            ((IDatabaseFacadeDependenciesAccessor)databaseFacade).Context,
                            logger));
            }
        }

        public static Task<RelationalDataReader> ExecuteReaderRawAsync(this DatabaseFacade databaseFacade, string sql, CancellationToken cancelToken)
            => ExecuteReaderRawAsync(databaseFacade, sql, Enumerable.Empty<object>(), cancelToken);

        public static Task<RelationalDataReader> ExecuteReaderRawAsync(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
            => ExecuteReaderRawAsync(databaseFacade, sql, parameters.AsEnumerable());

        public static async Task<RelationalDataReader> ExecuteReaderRawAsync(
            this DatabaseFacade databaseFacade,
            string sql,
            IEnumerable<object> parameters,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(databaseFacade);
            Guard.NotEmpty(sql);
            Guard.NotNull(parameters);

            var facadeDependencies = GetFacadeDependencies(databaseFacade);
            var concurrencyDetector = facadeDependencies.ConcurrencyDetector;
            var logger = facadeDependencies.CommandLogger;

            using (concurrencyDetector.EnterCriticalSection())
            {
                var rawSqlCommand = facadeDependencies.RawSqlCommandBuilder
                    .Build(sql, parameters);

                return await rawSqlCommand
                    .RelationalCommand
                    .ExecuteReaderAsync(
                        new RelationalCommandParameterObject(
                            facadeDependencies.RelationalConnection,
                            rawSqlCommand.ParameterValues,
                            null,
                            ((IDatabaseFacadeDependenciesAccessor)databaseFacade).Context,
                            logger), cancelToken)
                    .ConfigureAwait(false);
            }
        }

        #endregion

        #region ExecuteQuery

        public static IEnumerable<T> ExecuteQueryInterpolated<T>(this DatabaseFacade databaseFacade, FormattableString sql)
            => ExecuteQueryRaw<T>(databaseFacade, sql.Format, sql.GetArguments());

        public static IAsyncEnumerable<T> ExecuteQueryInterpolatedAsync<T>(this DatabaseFacade databaseFacade, FormattableString sql, CancellationToken cancelToken = default)
            => ExecuteQueryRawAsync<T>(databaseFacade, sql.Format, sql.GetArguments(), cancelToken);

        public static IEnumerable<T> ExecuteQueryRaw<T>(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
            => ExecuteQueryRaw<T>(databaseFacade, sql, parameters.AsEnumerable());

        public static IEnumerable<T> ExecuteQueryRaw<T>(this DatabaseFacade databaseFacade, string sql, IEnumerable<object> parameters)
        {
            var isComplexType = typeof(T).IsPlainObjectType();
            if (isComplexType)
            {
                Guard.HasDefaultConstructor<T>();
            }

            using var reader = ExecuteReaderRaw(databaseFacade, sql, parameters);
            while (reader.Read())
            {
                if (reader.DbDataReader.FieldCount > 0)
                {
                    yield return isComplexType
                        ? MapReaderToObject<T>(reader.DbDataReader)
                        : reader.DbDataReader.GetValue(0).Convert<T>();
                }
            }
        }

        public static IAsyncEnumerable<T> ExecuteQueryRawAsync<T>(this DatabaseFacade databaseFacade, string sql, CancellationToken cancelToken)
            => ExecuteQueryRawAsync<T>(databaseFacade, sql, Enumerable.Empty<object>(), cancelToken);

        public static IAsyncEnumerable<T> ExecuteQueryRawAsync<T>(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
            => ExecuteQueryRawAsync<T>(databaseFacade, sql, parameters.AsEnumerable());

        public static async IAsyncEnumerable<T> ExecuteQueryRawAsync<T>(
            this DatabaseFacade databaseFacade,
            string sql,
            IEnumerable<object> parameters,
            [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            var isComplexType = typeof(T).IsPlainObjectType();
            if (isComplexType)
            {
                Guard.HasDefaultConstructor<T>();
            }

            using var reader = await ExecuteReaderRawAsync(databaseFacade, sql, parameters, cancelToken);
            while (await reader.ReadAsync(cancelToken))
            {
                if (reader.DbDataReader.FieldCount > 0)
                {
                    yield return isComplexType
                        ? MapReaderToObject<T>(reader.DbDataReader)
                        : reader.DbDataReader.GetValue(0).Convert<T>();
                }
            }
        }

        private static T MapReaderToObject<T>(DbDataReader reader)
        {
            var fastProps = FastProperty.GetProperties(typeof(T));
            var obj = Activator.CreateInstance<T>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (!reader.IsDBNull(i))
                {
                    string columnName = reader.GetName(i);

                    if (fastProps.TryGetValue(columnName, out var prop))
                    {
                        var value = reader.GetValue(i);
                        if (ConvertUtility.TryConvert(value, prop.Property.PropertyType, out var converted))
                        {
                            prop.SetValue(obj, converted);
                        }
                    }
                }
            }

            return obj;
        }

        #endregion

        internal static IRelationalDatabaseFacadeDependencies GetFacadeDependencies(this DatabaseFacade databaseFacade)
        {
            var dependencies = ((IDatabaseFacadeDependenciesAccessor)databaseFacade).Dependencies;
            if (dependencies is IRelationalDatabaseFacadeDependencies relationalDependencies)
            {
                return relationalDependencies;
            }

            throw new InvalidOperationException(RelationalStrings.RelationalNotInUse);
        }

        internal static TService GetRelationalService<TService>(this IInfrastructure<IServiceProvider> databaseFacade)
        {
            Guard.NotNull(databaseFacade);

            var service = databaseFacade.Instance.GetService<TService>() ?? throw new InvalidOperationException(RelationalStrings.RelationalNotInUse);
            return service;
        }
    }
}
