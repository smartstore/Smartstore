using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Smartstore.ComponentModel;

namespace Smartstore.Data.Batching
{
    internal class BulkConfig
    {
        public bool SetOutputIdentity { get; set; }
        public bool UseTempDB { get; set; }
        public bool UniqueTableNameTempDb { get; set; } = true;
        public List<string> PropertiesToInclude { get; set; }
        public List<string> PropertiesToExclude { get; set; }
        public List<string> UpdateByProperties { get; set; }
        internal OperationType OperationType { get; set; }
    }

    internal class TableInfo
    {
        public string Schema { get; set; }
        public string SchemaFormatted => Schema != null ? $"[{Schema}]." : "";
        public string TableName { get; set; }
        public string FullTableName => $"{SchemaFormatted}[{TableName}]";
        public List<string> PrimaryKeys { get; set; }
        public bool HasSinglePrimaryKey { get; set; }
        public bool UpdateByPropertiesAreNullable { get; set; }

        protected string TempDBPrefix => BulkConfig.UseTempDB ? "#" : "";
        public string TempTableSufix { get; set; }
        public string TempTableName => $"{TableName}{TempTableSufix}";
        public string FullTempTableName => $"{SchemaFormatted}[{TempDBPrefix}{TempTableName}]";
        public string FullTempOutputTableName => $"{SchemaFormatted}[{TempDBPrefix}{TempTableName}Output]";

        public bool CreatedOutputTable => false;

        public bool InsertToTempTable { get; set; }
        public string IdentityColumnName { get; set; }
        public bool HasIdentity => IdentityColumnName != null;
        public bool HasOwnedTypes { get; set; }
        public bool HasAbstractList { get; set; }
        public bool ColumnNameContainsSquareBracket { get; set; }
        public bool LoadOnlyPKColumn { get; set; }
        public int NumberOfEntities { get; set; }

        public BulkConfig BulkConfig { get; set; }
        public Dictionary<string, string> OutputPropertyColumnNamesDict { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> PropertyColumnNamesDict { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, FastProperty> FastPropertyDict { get; set; } = new Dictionary<string, FastProperty>();
        public Dictionary<string, INavigation> OwnedTypesDict { get; set; } = new Dictionary<string, INavigation>();
        public HashSet<string> ShadowProperties { get; set; } = new HashSet<string>();
        public Dictionary<string, ValueConverter> ConvertibleProperties { get; set; } = new Dictionary<string, ValueConverter>();
        public string TimeStampOutColumnType => "varbinary(8)";
        public string TimeStampColumnName { get; set; }

        public static TableInfo CreateInstance<T>(HookingDbContext context, IList<T> entities, OperationType operationType, BulkConfig bulkConfig)
        {
            return CreateInstance<T>(context, typeof(T), entities, operationType, bulkConfig);
        }

        public static TableInfo CreateInstance(HookingDbContext context, Type type, IList<object> entities, OperationType operationType, BulkConfig bulkConfig)
        {
            return CreateInstance<object>(context, type, entities, operationType, bulkConfig);
        }

        private static TableInfo CreateInstance<T>(HookingDbContext context, Type type, IList<T> entities, OperationType operationType, BulkConfig bulkConfig)
        {
            var tableInfo = new TableInfo
            {
                NumberOfEntities = entities.Count,
                BulkConfig = bulkConfig ?? new BulkConfig() { }
            };
            tableInfo.BulkConfig.OperationType = operationType;

            bool isExplicitTransaction = context.Database.GetDbConnection().State == ConnectionState.Open;
            if (tableInfo.BulkConfig.UseTempDB == true && !isExplicitTransaction && (operationType != OperationType.Insert || tableInfo.BulkConfig.SetOutputIdentity))
            {
                throw new InvalidOperationException("UseTempDB when set then BulkOperation has to be inside Transaction. More info in README of the library in GitHub.");
                // Otherwise throws exception: 'Cannot access destination table' (gets Dropped too early because transaction ends before operation is finished)
            }

            var isDeleteOperation = operationType == OperationType.Delete;
            tableInfo.LoadData<T>(context, type, entities, isDeleteOperation);
            return tableInfo;
        }

        private void LoadData<T>(HookingDbContext context, Type type, IList<T> entities, bool loadOnlyPKColumn)
        {
            LoadOnlyPKColumn = loadOnlyPKColumn;
            var entityType = context.Model.FindEntityType(type);
            if (entityType == null)
            {
                type = entities[0].GetType();
                entityType = context.Model.FindEntityType(type);
                HasAbstractList = true;
            }
            if (entityType == null)
                throw new InvalidOperationException($"DbContext does not contain EntitySet for Type: { type.Name }");

            //var relationalData = entityType.Relational(); relationalData.Schema relationalData.TableName // DEPRECATED in Core3.0
            var storeObjectIdent = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table).Value;
            bool isSqlServer = context.DataProvider.ProviderType == DataProviderType.SqlServer;
            string defaultSchema = isSqlServer ? "dbo" : null;
            Schema = entityType.GetSchema() ?? defaultSchema;
            TableName = entityType.GetTableName();

            TempTableSufix = "Temp";

            if (!BulkConfig.UseTempDB || BulkConfig.UniqueTableNameTempDb)
            {
                TempTableSufix += Guid.NewGuid().ToString().Substring(0, 8); // 8 chars of Guid as tableNameSufix to avoid same name collision with other tables
            }

            bool areSpecifiedUpdateByProperties = BulkConfig.UpdateByProperties?.Count > 0;
            var primaryKeys = entityType.FindPrimaryKey()?.Properties?.Select(a => a.Name)?.ToList();

            HasSinglePrimaryKey = primaryKeys?.Count == 1;
            PrimaryKeys = areSpecifiedUpdateByProperties ? BulkConfig.UpdateByProperties : primaryKeys;

            var allProperties = entityType.GetProperties().AsEnumerable();

            // load all derived type properties
            if (entityType.IsAbstract())
            {
                var extendedAllProperties = allProperties.ToList();
                foreach (var derived in entityType.GetDirectlyDerivedTypes())
                {
                    extendedAllProperties.AddRange(derived.GetProperties());
                }

                allProperties = extendedAllProperties.Distinct();
            }

            var ownedTypes = entityType.GetNavigations().Where(a => a.TargetEntityType.IsOwned());
            HasOwnedTypes = ownedTypes.Any();
            OwnedTypesDict = ownedTypes.ToDictionary(a => a.Name, a => a);

            IdentityColumnName = allProperties.SingleOrDefault(a => a.IsPrimaryKey() && a.ClrType.Name.StartsWith("Int") && a.ValueGenerated == ValueGenerated.OnAdd)?.GetColumnName(storeObjectIdent); // ValueGenerated equals OnAdd even for nonIdentity column like Guid so we only type int as second condition

            // timestamp/row version properties are only set by the Db, the property has a [Timestamp] Attribute or is configured in FluentAPI with .IsRowVersion()
            // They can be identified by the columne type "timestamp" or .IsConcurrencyToken in combination with .ValueGenerated == ValueGenerated.OnAddOrUpdate
            string timestampDbTypeName = nameof(TimestampAttribute).Replace("Attribute", "").ToLower(); // = "timestamp";
            var timeStampProperties = allProperties.Where(a => (a.IsConcurrencyToken && a.ValueGenerated == ValueGenerated.OnAddOrUpdate) || a.GetColumnType() == timestampDbTypeName);
            TimeStampColumnName = timeStampProperties.FirstOrDefault()?.GetColumnName(storeObjectIdent); // can be only One
            var allPropertiesExceptTimeStamp = allProperties.Except(timeStampProperties);
            var properties = allPropertiesExceptTimeStamp.Where(a => a.GetComputedColumnSql() == null);

            // TimeStamp prop. is last column in OutputTable since it is added later with varbinary(8) type in which Output can be inserted
            OutputPropertyColumnNamesDict = allPropertiesExceptTimeStamp.Concat(timeStampProperties).ToDictionary(a => a.Name, b => b.GetColumnName(storeObjectIdent).Replace("]", "]]")); // square brackets have to be escaped
            ColumnNameContainsSquareBracket = allPropertiesExceptTimeStamp.Concat(timeStampProperties).Any(a => a.GetColumnName(storeObjectIdent).Contains("]"));

            bool AreSpecifiedPropertiesToInclude = BulkConfig.PropertiesToInclude?.Count() > 0;
            bool AreSpecifiedPropertiesToExclude = BulkConfig.PropertiesToExclude?.Count() > 0;

            if (AreSpecifiedPropertiesToInclude)
            {
                if (areSpecifiedUpdateByProperties) // Adds UpdateByProperties to PropertyToInclude if they are not already explicitly listed
                {
                    foreach (var updateByProperty in BulkConfig.UpdateByProperties)
                    {
                        if (!BulkConfig.PropertiesToInclude.Contains(updateByProperty))
                        {
                            BulkConfig.PropertiesToInclude.Add(updateByProperty);
                        }
                    }
                }
                else // Adds PrimaryKeys to PropertyToInclude if they are not already explicitly listed
                {
                    foreach (var primaryKey in PrimaryKeys)
                    {
                        if (!BulkConfig.PropertiesToInclude.Contains(primaryKey))
                        {
                            BulkConfig.PropertiesToInclude.Add(primaryKey);
                        }
                    }
                }
            }

            foreach (var property in allProperties)
            {
                if (property.PropertyInfo != null) // skip Shadow Property
                {
                    FastPropertyDict.Add(property.Name, FastProperty.GetProperty(property.PropertyInfo));
                }
            }

            UpdateByPropertiesAreNullable = properties.Any(a => PrimaryKeys != null && PrimaryKeys.Contains(a.Name) && a.IsNullable);

            if (AreSpecifiedPropertiesToInclude || AreSpecifiedPropertiesToExclude)
            {
                if (AreSpecifiedPropertiesToInclude && AreSpecifiedPropertiesToExclude)
                    throw new InvalidOperationException("Only one group of properties, either PropertiesToInclude or PropertiesToExclude can be specified, specifying both not allowed.");
                if (AreSpecifiedPropertiesToInclude)
                    properties = properties.Where(a => BulkConfig.PropertiesToInclude.Contains(a.Name));
                if (AreSpecifiedPropertiesToExclude)
                    properties = properties.Where(a => !BulkConfig.PropertiesToExclude.Contains(a.Name));
            }

            if (loadOnlyPKColumn)
            {
                PropertyColumnNamesDict = properties.Where(a => PrimaryKeys.Contains(a.Name)).ToDictionary(a => a.Name, b => b.GetColumnName(storeObjectIdent).Replace("]", "]]"));
            }
            else
            {
                PropertyColumnNamesDict = properties.ToDictionary(a => a.Name, b => b.GetColumnName(storeObjectIdent).Replace("]", "]]"));
                ShadowProperties = new HashSet<string>(properties.Where(p => p.IsShadowProperty() && !p.IsForeignKey()).Select(p => p.GetColumnName(storeObjectIdent)));
                foreach (var property in properties.Where(p => p.GetValueConverter() != null))
                {
                    string columnName = property.GetColumnName(storeObjectIdent);
                    ValueConverter converter = property.GetValueConverter();
                    ConvertibleProperties.Add(columnName, converter);
                }

                foreach (var navigation in entityType.GetNavigations().Where(x => !x.IsCollection && !x.TargetEntityType.IsOwned()))
                {
                    FastPropertyDict.Add(navigation.Name, FastProperty.GetProperty(navigation.PropertyInfo));
                }

                if (HasOwnedTypes)  // Support owned entity property update. TODO: Optimize
                {
                    foreach (var navgationProperty in ownedTypes)
                    {
                        var property = navgationProperty.PropertyInfo;
                        FastPropertyDict.Add(property.Name, FastProperty.GetProperty(property));

                        Type navOwnedType = type.Assembly.GetType(property.PropertyType.FullName);
                        var ownedEntityType = context.Model.FindEntityType(property.PropertyType);
                        if (ownedEntityType == null) // when entity has more then one ownedType (e.g. Address HomeAddress, Address WorkAddress) or one ownedType is in multiple Entities like Audit is usually.
                        {
                            ownedEntityType = context.Model.GetEntityTypes().SingleOrDefault(a => a.DefiningNavigationName == property.Name && a.DefiningEntityType.Name == entityType.Name);
                        }
                        var ownedEntityProperties = ownedEntityType.GetProperties().ToList();
                        var ownedEntityPropertyNameColumnNameDict = new Dictionary<string, string>();
                        var ownedStoreObjectIdent = StoreObjectIdentifier.Create(ownedEntityType, StoreObjectType.Table).Value;

                        foreach (var ownedEntityProperty in ownedEntityProperties)
                        {
                            if (!ownedEntityProperty.IsPrimaryKey())
                            {
                                string columnName = ownedEntityProperty.GetColumnName(ownedStoreObjectIdent);
                                ownedEntityPropertyNameColumnNameDict.Add(ownedEntityProperty.Name, columnName);
                                var ownedEntityPropertyFullName = property.Name + "_" + ownedEntityProperty.Name;
                                if (!FastPropertyDict.ContainsKey(ownedEntityPropertyFullName))
                                {
                                    FastPropertyDict.Add(ownedEntityPropertyFullName, FastProperty.GetProperty(ownedEntityProperty.PropertyInfo));
                                }
                            }

                            var converter = ownedEntityProperty.GetValueConverter();
                            if (converter != null)
                            {
                                ConvertibleProperties.Add($"{navgationProperty.Name}_{ownedEntityProperty.Name}", converter);
                            }
                        }
                        var ownedProperties = property.PropertyType.GetProperties();
                        foreach (var ownedProperty in ownedProperties)
                        {
                            if (ownedEntityPropertyNameColumnNameDict.ContainsKey(ownedProperty.Name))
                            {
                                string columnName = ownedEntityPropertyNameColumnNameDict[ownedProperty.Name];
                                var ownedPropertyType = Nullable.GetUnderlyingType(ownedProperty.PropertyType) ?? ownedProperty.PropertyType;

                                bool doAddProperty = true;
                                if (AreSpecifiedPropertiesToInclude && !BulkConfig.PropertiesToInclude.Contains(columnName))
                                {
                                    doAddProperty = false;
                                }
                                if (AreSpecifiedPropertiesToExclude && BulkConfig.PropertiesToExclude.Contains(columnName))
                                {
                                    doAddProperty = false;
                                }

                                if (doAddProperty)
                                {
                                    PropertyColumnNamesDict.Add(property.Name + "." + ownedProperty.Name, columnName);
                                    OutputPropertyColumnNamesDict.Add(property.Name + "." + ownedProperty.Name, columnName);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}