using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.ComponentModel;
using Smartstore.Utilities;

namespace Smartstore;

public static class DatabaseFacadeExtensions
{
    #region Database creation

    extension (DatabaseFacade databaseFacade)
    {
        /// <summary>
        /// Ensures that the database for the context exists. If it exists, no action is taken. If it does not
        /// exist then the database is created WITHOUT populating the schema.
        /// </summary>
        /// <param name="databaseFacade">The <see cref="DatabaseFacade" /> for the context.</param>
        /// <returns>
        /// <see langword="true" /> if the database is created, <see langword="false" /> if it already existed.
        /// </returns>
        public bool EnsureCreatedSchemaless()
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
        public async Task<bool> EnsureCreatedSchemalessAsync(CancellationToken cancelToken = default)
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
    }

    #endregion

    #region ExecuteScalar

    extension (DatabaseFacade databaseFacade)
    {
        public T ExecuteScalarInterpolated<T>(FormattableString sql)
            => ExecuteScalarRaw<T>(databaseFacade, sql.Format, sql.GetArguments());

        public Task<T> ExecuteScalarInterpolatedAsync<T>(FormattableString sql, CancellationToken cancelToken = default)
            => ExecuteScalarRawAsync<T>(databaseFacade, sql.Format, sql.GetArguments(), cancelToken);

        public T ExecuteScalarRaw<T>(string sql, params object[] parameters)
            => ExecuteScalarRaw<T>(databaseFacade, sql, parameters.AsEnumerable());

        public T ExecuteScalarRaw<T>(string sql, IEnumerable<object> parameters)
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

        public Task<T> ExecuteScalarRawAsync<T>(string sql, CancellationToken cancelToken)
            => ExecuteScalarRawAsync<T>(databaseFacade, sql, [], cancelToken);

        public Task<T> ExecuteScalarRawAsync<T>(string sql, params object[] parameters)
            => ExecuteScalarRawAsync<T>(databaseFacade, sql, parameters.AsEnumerable());

        public async Task<T> ExecuteScalarRawAsync<T>(string sql, IEnumerable<object> parameters, CancellationToken cancelToken = default)
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
    }

    #endregion

    #region ExecuteReader

    extension(DatabaseFacade databaseFacade)
    {
        public RelationalDataReader ExecuteReaderInterpolated(FormattableString sql)
            => ExecuteReaderRaw(databaseFacade, sql.Format, sql.GetArguments());

        public Task<RelationalDataReader> ExecuteReaderInterpolatedAsync(FormattableString sql, CancellationToken cancelToken = default)
            => ExecuteReaderRawAsync(databaseFacade, sql.Format, sql.GetArguments(), cancelToken);

        public RelationalDataReader ExecuteReaderRaw(string sql, params object[] parameters)
            => ExecuteReaderRaw(databaseFacade, sql, parameters.AsEnumerable());

        public RelationalDataReader ExecuteReaderRaw(string sql, IEnumerable<object> parameters)
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

        public Task<RelationalDataReader> ExecuteReaderRawAsync(string sql, CancellationToken cancelToken)
            => ExecuteReaderRawAsync(databaseFacade, sql, [], cancelToken);

        public Task<RelationalDataReader> ExecuteReaderRawAsync(string sql, params object[] parameters)
            => ExecuteReaderRawAsync(databaseFacade, sql, parameters.AsEnumerable());

        public  async Task<RelationalDataReader> ExecuteReaderRawAsync(
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
    }

    #endregion

    #region ExecuteQuery

    extension (DatabaseFacade databaseFacade)
    {
        public IEnumerable<T> ExecuteQueryInterpolated<T>(FormattableString sql)
            => ExecuteQueryRaw<T>(databaseFacade, sql.Format, sql.GetArguments());

        public IAsyncEnumerable<T> ExecuteQueryInterpolatedAsync<T>(FormattableString sql, CancellationToken cancelToken = default)
            => ExecuteQueryRawAsync<T>(databaseFacade, sql.Format, sql.GetArguments(), cancelToken);

        public IEnumerable<T> ExecuteQueryRaw<T>(string sql, params object[] parameters)
            => ExecuteQueryRaw<T>(databaseFacade, sql, parameters.AsEnumerable());

        public IEnumerable<T> ExecuteQueryRaw<T>(string sql, IEnumerable<object> parameters)
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

        public IAsyncEnumerable<T> ExecuteQueryRawAsync<T>(string sql, CancellationToken cancelToken)
            => ExecuteQueryRawAsync<T>(databaseFacade, sql, [], cancelToken);

        public IAsyncEnumerable<T> ExecuteQueryRawAsync<T>(string sql, params object[] parameters)
            => ExecuteQueryRawAsync<T>(databaseFacade, sql, parameters.AsEnumerable());

        public async IAsyncEnumerable<T> ExecuteQueryRawAsync<T>(
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
