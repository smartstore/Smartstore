using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Data;

namespace Smartstore.Core.Data.Migrations
{
    public class DbMigrationManager
    {
        private static DbMigrationManager _instance;
        private List<Type> _dbContextTypes = new();
        private readonly ConcurrentDictionary<Type, bool> _suppressMap = new();
        private Multimap<Type, string> _appliedMigrations;
        private readonly object _lock = new();

        private DbMigrationManager()
        {
        }

        public static DbMigrationManager Instance
        {
            get => _instance ??= new DbMigrationManager();
        }

        public void RegisterDbContext(Type dbContextType)
        {
            Guard.NotNull(dbContextType, nameof(dbContextType));
            Guard.IsAssignableFrom<HookingDbContext>(dbContextType);

            if (!_dbContextTypes.Contains(dbContextType))
            {
                _dbContextTypes.Add(dbContextType);
            }
        }

        public IReadOnlyCollection<Type> GetDbContextTypes()
            => _dbContextTypes.AsReadOnly();

        internal void SetSuppressInitialCreate<TContext>(bool suppress) where TContext : DbContext
        {
            _suppressMap[typeof(TContext)] = suppress;
        }

        public bool SuppressInitialCreate<TContext>() where TContext : DbContext
        {
            if (_suppressMap.TryGetValue(typeof(TContext), out var value))
            {
                return value;
            }

            return false;
        }

        internal void AddAppliedMigration(Type contextType, string migrationName)
        {
            if (_appliedMigrations == null)
            {
                lock (_lock)
                {
                    if (_appliedMigrations == null)
                    {
                        _appliedMigrations = new Multimap<Type, string>();
                    }
                }
            }

            _appliedMigrations.Add(contextType, migrationName);
        }

        public IEnumerable<string> GetAppliedMigrations()
        {
            if (_appliedMigrations != null)
            {
                return _appliedMigrations.SelectMany(x => x.Value);
            }

            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetAppliedMigrations<TContext>() where TContext : DbContext
        {
            if (_appliedMigrations != null && _appliedMigrations.ContainsKey(typeof(TContext)))
            {
                return _appliedMigrations[typeof(TContext)].AsReadOnly();
            }

            return Enumerable.Empty<string>();
        }
    }
}
