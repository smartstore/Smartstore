using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Smartstore.Core.DataExchange.Import
{
    public class ImportRow<T> where T : BaseEntity
    {
        const string ExplicitNull = "[NULL]";
        const string ExplicitIgnore = "[IGNORE]";

        private bool _initialized = false;
        private T _entity;
        private string _entityDisplayName;
        private readonly int _position;
        private bool _isNew;
        private bool _isDirty;
        private ImportRowInfo _rowInfo;

        private readonly ImportDataSegmenter _segmenter;
        private readonly IDataRow _row;

        public ImportRow(ImportDataSegmenter parent, IDataRow row, int position)
        {
            _segmenter = parent;
            _row = row;
            _position = position;
        }

        public bool IsTransient => _entity.Id == 0;
        public bool IsNew => _isNew;
        public bool IsDirty => _isDirty;

        public int Position => _position;

        public T Entity => _entity;
        public string EntityDisplayName => _entityDisplayName;
        public bool NameChanged { get; set; }

        public ImportDataSegmenter Segmenter => _segmenter;
        public IDataRow DataRow => _row;

        public ImportRowInfo RowInfo
        {
            get => _rowInfo ??= new ImportRowInfo(Position, EntityDisplayName);
        }

        public void Initialize(T entity, string entityDisplayName)
        {
            _entity = entity;
            _entityDisplayName = entityDisplayName;
            _isNew = _entity.Id == 0;
            _initialized = true;
        }

        /// <summary>
        /// Determines whether a specific column exists in the underlying data table 
        /// and contains a non-null, convertible value.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="withAnyIndex">
        ///		If <c>true</c> and a column with the passed <paramref name="columnName"/> does not exist,
        ///		this method seeks for any indexed column with the same name.
        /// </param>
        /// <returns><c>true</c> if the column exists and contains a value, <c>false</c> otherwise</returns>
        /// <remarks>This method takes mapped column names into account.</remarks>
        public bool HasDataValue(string columnName, bool withAnyIndex = false)
        {
            var result = HasDataValue(columnName, null);

            if (!result && withAnyIndex)
            {
                // Column does not have a value, but withAnyIndex is true:
                // Test for values in any indexed column.
                var indexes = _segmenter.GetColumnIndexes(columnName);
                foreach (var idx in indexes)
                {
                    result = HasDataValue(columnName, idx);
                    if (result)
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether the column <c>name[index]</c> exists in the underlying data table 
        /// and contains a non-null, convertible value.
        /// </summary>
        /// <param name="columnName">The name of the column</param>
        /// <param name="index">The index of the column</param>
        /// <returns><c>true</c> if the column exists and contains a value, <c>false</c> otherwise</returns>
        /// <remarks>This method takes mapped column names into account.</remarks>
        public bool HasDataValue(string columnName, string index)
        {
            var mapping = _segmenter.ColumnMap.GetMapping(columnName, index);
            return _row.TryGetValue(mapping.MappedName, out var value) && value != null && value != DBNull.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TProp GetDataValue<TProp>(string columnName, bool force = false)
        {
            TryGetDataValue<TProp>(columnName, null, out var value, force);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TProp GetDataValue<TProp>(string columnName, string index, bool force = false)
        {
            TryGetDataValue<TProp>(columnName, index, out var value, force);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetDataValue<TProp>(string columnName, out TProp value, bool force = false)
            => TryGetDataValue(columnName, null, out value, force);

        public bool TryGetDataValue<TProp>(string columnName, string index, out TProp value, bool force = false)
        {
            var mapping = _segmenter.ColumnMap.GetMapping(columnName, index);

            if (!force && mapping.IgnoreProperty)
            {
                value = default;
                return false;
            }

            if (_row.TryGetValue(mapping.MappedName, out var rawValue) 
                && rawValue != null 
                && rawValue != DBNull.Value 
                && !rawValue.ToString().EqualsNoCase(ExplicitIgnore))
            {
                value = rawValue.ToString().EqualsNoCase(ExplicitNull)
                    ? default
                    : rawValue.Convert<TProp>(_segmenter.Culture);
                return true;
            }

            if (IsNew)
            {
                // Only transient/new entities should fallback to possible defaults.
                value = GetDefaultValue(mapping, default(TProp));
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetProperty<TProp>(
            ImportResult result,
            Expression<Func<T, TProp>> prop,
            TProp defaultValue = default,
            Func<object, CultureInfo, TProp> converter = null)
        {
            return SetProperty(
                result,
                null, // columnName
                prop,
                defaultValue,
                converter);
        }

        public bool SetProperty<TProp>(
            ImportResult result,
            string columnName,
            Expression<Func<T, TProp>> prop,
            TProp defaultValue = default,
            Func<object, CultureInfo, TProp> converter = null)
        {
            var isPropertySet = false;
            var pi = prop.ExtractPropertyInfo();
            var propName = pi.Name;
            var target = _entity;

            columnName ??= propName;

            try
            {
                var mapping = _segmenter.ColumnMap.GetMapping(columnName);

                if (mapping.IgnoreProperty)
                {
                    // Explicitly ignore this property.
                }
                else if (_row.TryGetValue(mapping.MappedName, out object value) &&
                    value != null &&
                    value != DBNull.Value &&
                    !value.ToString().EqualsNoCase(ExplicitIgnore))
                {
                    // Source contains field value. Set it.
                    object converted;
                    if (converter != null)
                    {
                        converted = converter(value, _segmenter.Culture);
                    }
                    else if (value.ToString().EqualsNoCase(ExplicitNull))
                    {
                        // Prop is "explicitly" set to null. Don't fallback to any default!
                        converted = default;
                    }
                    else
                    {
                        converted = value.Convert<TProp>(_segmenter.Culture);

                        // INFO: Caching annotations via ConcurrentDictionary is not really that much faster.
                        if (
                            converted is string str && 
                            pi.TryGetAttribute<StringLengthAttribute>(false, out var attribute) && 
                            attribute.MaximumLength > 0 &&
                            str.Length > attribute.MaximumLength)
                        {
                            converted = str.Truncate(attribute.MaximumLength);
                        }
                    }

                    target.GetType().GetProperty(propName).SetValue(target, converted);
                    isPropertySet = true;
                }
                else
                {
                    // Source field value does not exist or is null/empty.
                    if (IsNew)
                    {
                        // If entity is new and source field value is null, determine default value in this particular order: 
                        //		2.) Default value in field mapping table.
                        //		3.) passed default value argument.
                        defaultValue = GetDefaultValue(mapping, defaultValue, result);

                        // source does not contain field data or is empty...
                        if (defaultValue != null)
                        {
                            // ...but the entity is new. In this case set the default value if given.
                            target.GetType().GetProperty(propName).SetValue(target, defaultValue);
                            isPropertySet = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddWarning("Conversion failed: " + ex.Message, RowInfo, propName);
            }

            if (isPropertySet && !_isDirty)
            {
                _isDirty = true;
            }

            return isPropertySet;
        }

        public override string ToString()
        {
            return "Pos: {0} - Name: {1}, IsNew: {2}, IsTransient: {3}".FormatInvariant(
                Position,
                EntityDisplayName.EmptyNull(),
                _initialized ? IsNew.ToString() : "-",
                _initialized ? IsTransient.ToString() : "-");
        }

        private TProp GetDefaultValue<TProp>(ColumnMappingItem mapping, TProp defaultValue, ImportResult result = null)
        {
            if (mapping != null && mapping.Default.HasValue())
            {
                try
                {
                    return mapping.Default.Convert<TProp>(_segmenter.Culture);
                }
                catch (Exception ex)
                {
                    result?.AddWarning($"Failed to convert default value '{mapping.Default}'. Please specify a convertable default value. Message: {ex.Message}", RowInfo, mapping.SourceName);
                }
            }

            return defaultValue;
        }
    }
}
