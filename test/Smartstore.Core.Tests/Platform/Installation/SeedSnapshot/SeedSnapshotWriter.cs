using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smartstore.Core.Data;

namespace Smartstore.Core.Tests.Platform.Installation.SeedSnapshot;

/// <summary>
/// Creates a normalized, ID-independent JSON snapshot of all rows in a <see cref="SmartDbContext"/>.
/// </summary>
/// <remarks>
/// Every row gets a stable label ("$key") derived from natural keys. Primary keys are dropped and every
/// reference to another row (foreign keys, polymorphic EntityName/EntityId pairs, conventional *Id columns,
/// IDs embedded in strings) is replaced by the label of the referenced row. Volatile values such as GUIDs,
/// timestamps and password hashes are masked. This way the snapshot only changes when seeded content changes,
/// not when insertion order or generated values change.
/// </remarks>
internal sealed partial class SeedSnapshotWriter
{
    private static readonly Dictionary<string, Func<Row, string>> KeySpecs = new(StringComparer.Ordinal)
    {
        ["AclRecord"] = r => r.Join("EntityName", "EntityId", "CustomerRoleId"),
        ["ActivityLogType"] = r => r.Get("SystemKeyword"),
        ["Address"] = r => r.Join("Email", "LastName", "City"),
        ["Campaign"] = r => r.Get("Name"),
        ["Category"] = r => r.First("Alias", "Name"),
        ["CategoryTemplate"] = r => r.Get("ViewPath"),
        ["Country"] = r => r.Get("TwoLetterIsoCode"),
        ["Currency"] = r => r.Get("CurrencyCode"),
        ["Customer"] = r => r.First("SystemName", "Email"),
        ["CrossSellProduct"] = r => r.Join("ProductId1", "ProductId2"),
        ["CustomerRole"] = r => r.Get("SystemName"),
        ["CustomerRoleMapping"] = r => r.Join("CustomerId", "CustomerRoleId"),
        ["DeliveryTime"] = r => r.Get("Name"),
        ["Discount"] = r => r.Get("Name"),
        ["Download"] = r => r.Join("EntityName", "EntityId", "MediaFileId"),
        ["EmailAccount"] = r => r.Get("Email"),
        ["GenericAttribute"] = r => r.Join("KeyGroup", "EntityId", "Key"),
        ["Language"] = r => r.Get("LanguageCulture"),
        ["Manufacturer"] = r => r.Get("Name"),
        ["ManufacturerTemplate"] = r => r.Get("ViewPath"),
        ["MeasureDimension"] = r => r.Get("SystemKeyword"),
        ["MeasureWeight"] = r => r.Get("SystemKeyword"),
        ["MediaFile"] = r => r.Get("Name"),
        ["MenuEntity"] = r => r.Get("SystemName"),
        ["MenuItemEntity"] = r => r.Join("MenuId", "ProviderName", "Model", "Title"),
        ["PriceLabel"] = r => r.Get("ShortName"),
        ["Product"] = r => r.First("Sku", "Name"),
        ["ProductAttribute"] = r => r.Get("Alias"),
        ["ProductAttributeOption"] = r => r.Join("ProductAttributeOptionsSetId", "Alias"),
        ["ProductAttributeOptionsSet"] = r => r.Join("ProductAttributeId", "Name"),
        ["ProductBundleItem"] = r => r.Join("BundleProductId", "ProductId"),
        ["ProductCategory"] = r => r.Join("ProductId", "CategoryId"),
        ["ProductManufacturer"] = r => r.Join("ProductId", "ManufacturerId"),
        ["ProductMediaFile"] = r => r.Join("ProductId", "MediaFileId"),
        ["ProductSpecificationAttribute"] = r => r.Join("ProductId", "SpecificationAttributeOptionId"),
        ["ProductTag"] = r => r.Get("Name"),
        ["ProductTemplate"] = r => r.Get("ViewPath"),
        ["ProductVariantAttribute"] = r => r.Join("ProductId", "ProductAttributeId"),
        ["ProductVariantAttributeCombination"] = r => r.Get("Sku"),
        ["ProductVariantAttributeValue"] = r => r.Join("ProductVariantAttributeId", "Alias", "Name"),
        ["QuantityUnit"] = r => r.Get("Name"),
        ["RelatedProduct"] = r => r.Join("ProductId1", "ProductId2"),
        ["RuleEntity"] = r => r.Join("RuleSetId", "RuleType"),
        ["RuleSetEntity"] = r => r.Get("Name"),
        ["Setting"] = r => r.Join("Name", "StoreId"),
        ["ShippingMethod"] = r => r.Get("Name"),
        ["SpecificationAttribute"] = r => r.Get("Name"),
        ["SpecificationAttributeOption"] = r => r.Join("SpecificationAttributeId", "Name"),
        ["StateProvince"] = r => r.Join("CountryId", "Abbreviation"),
        ["Store"] = r => r.Get("Name"),
        ["TaskDescriptor"] = r => r.Get("Type"),
        ["TaxCategory"] = r => r.Get("Name"),
        ["TierPrice"] = r => r.Join("ProductId", "Quantity", "CustomerRoleId", "StoreId"),
        ["Topic"] = r => r.Get("SystemName"),
        ["UrlRecord"] = r => r.Join("EntityName", "EntityId", "LanguageId", "Slug")
    };

    // Polymorphic references: (group property, id property).
    private static readonly (string Group, string Id)[] PolymorphicRefs =
    [
        ("EntityName", "EntityId"),
        ("KeyGroup", "EntityId"),
        ("LocaleKeyGroup", "EntityId")
    ];

    // Name stems of conventional *Id columns that do not end with the referenced type name.
    private static readonly Dictionary<string, string> ConventionAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Picture"] = "MediaFile",
        ["Dimension"] = "MeasureDimension",
        ["Weight"] = "MeasureWeight",
        ["TaxClass"] = "TaxCategory"
    };

    // Tables that are seeded from other sources than the seed data (resource files).
    private static readonly HashSet<string> ExcludedTables = new(StringComparer.Ordinal) { "LocaleStringResource" };

    private static readonly HashSet<string> SecretProperties = new(StringComparer.Ordinal) { "Password", "PasswordSalt" };

    // Settings that get a random value on every installation.
    private static readonly HashSet<string> RandomSettings = new(StringComparer.OrdinalIgnoreCase) { "SecuritySettings.EncryptionKey" };

    // Values derived from database IDs (e.g. the hash over a raw attribute selection).
    private static readonly HashSet<(string Table, string Property)> IdDerivedProperties =
    [
        ("ProductVariantAttributeCombination", "HashCode")
    ];

    private readonly SmartDbContext _db;
    private readonly DateTime _seededAtUtc;
    private readonly List<Table> _tables = [];
    private readonly Dictionary<IEntityType, Table> _tablesByType = [];
    private readonly Dictionary<string, Table> _tablesByClrName = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<Table> _labeling = [];

    private SeedSnapshotWriter(SmartDbContext db, DateTime seededAtUtc)
    {
        _db = db;
        _seededAtUtc = seededAtUtc;
    }

    /// <summary>
    /// Creates the snapshot as a map of file name to file content (CRLF line endings).
    /// </summary>
    /// <param name="seededAtUtc">Point in time the data was seeded. Timestamps close to it are masked.</param>
    public static SortedDictionary<string, string> CreateSnapshot(SmartDbContext db, DateTime seededAtUtc)
    {
        var writer = new SeedSnapshotWriter(db, seededAtUtc);
        writer.LoadTables();

        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var table in writer._tables.Where(x => x.Rows.Count > 0))
        {
            writer.EnsureLabeled(table);

            var array = new JArray(table.Rows
                .OrderBy(x => x.Label, StringComparer.Ordinal)
                .Select(writer.SerializeRow));

            var json = array.ToString(Formatting.Indented).Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n";
            result[table.FileName + ".json"] = json;
        }

        return result;
    }

    #region Loading

    private void LoadTables()
    {
        var loadMethod = typeof(SeedSnapshotWriter).GetMethod(nameof(LoadEntities), BindingFlags.NonPublic | BindingFlags.Instance);
        var loadSharedMethod = typeof(SeedSnapshotWriter).GetMethod(nameof(LoadSharedEntities), BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var entityType in _db.Model.GetEntityTypes().Where(x => !x.IsOwned() && x.BaseType == null && !ExcludedTables.Contains(x.ClrType.Name)))
        {
            List<object> entities;
            if (entityType.HasSharedClrType)
            {
                entities = (List<object>)loadSharedMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [entityType.Name]);
            }
            else if (!entityType.ClrType.IsAbstract)
            {
                entities = (List<object>)loadMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, null);
            }
            else
            {
                continue;
            }

            var pk = entityType.FindPrimaryKey();
            var hasIntId = pk != null && pk.Properties.Count == 1 && pk.Properties[0].ClrType == typeof(int);
            var name = entityType.HasSharedClrType ? entityType.Name : entityType.ClrType.Name;

            var table = new Table(name, entityType, hasIntId ? pk.Properties[0] : null);

            foreach (var entity in entities)
            {
                var entry = _db.Entry(entity);
                var rowType = entry.Metadata;
                var values = rowType.GetProperties().ToDictionary(p => p, p => entry.Property(p.Name).CurrentValue);
                var row = new Row(this, table, rowType, values);
                table.Rows.Add(row);

                if (table.IdProperty != null)
                {
                    table.RowsById[(int)values[table.IdProperty]] = row;
                }
            }

            _tables.Add(table);
            _tablesByType[entityType] = table;
            _tablesByClrName[name] = table;
        }
    }

    private List<object> LoadEntities<T>() where T : class
        => _db.Set<T>().IgnoreQueryFilters().Cast<object>().ToList();

    private List<object> LoadSharedEntities<T>(string name) where T : class
        => _db.Set<T>(name).IgnoreQueryFilters().Cast<object>().ToList();

    #endregion

    #region Labeling

    private void EnsureLabeled(Table table)
    {
        if (table.IsLabeled)
        {
            return;
        }

        if (!_labeling.Add(table))
        {
            throw new InvalidOperationException($"Cyclic key specification detected for table '{table.Name}'.");
        }

        KeySpecs.TryGetValue(table.Name, out var spec);

        foreach (var row in table.Rows)
        {
            row.Label = spec != null ? spec(row) : CreateFallbackLabel(row);
        }

        // Make labels unique in a deterministic way.
        foreach (var group in table.Rows.GroupBy(x => x.Label, StringComparer.Ordinal).Where(x => x.Count() > 1))
        {
            var i = 0;
            foreach (var row in group.OrderBy(x => x.ContentHash, StringComparer.Ordinal))
            {
                row.Label += "#" + (++i);
            }
        }

        _labeling.Remove(table);
        table.IsLabeled = true;
    }

    private string CreateFallbackLabel(Row row)
    {
        // Join tables and other keyless types are fully described by their references.
        if (row.Table.IdProperty == null)
        {
            return string.Join("|", row.Type.GetProperties().OrderBy(x => x.Name, StringComparer.Ordinal).Select(p => row.Get(p.Name)));
        }

        return "#" + row.ContentHash[..12];
    }

    private string LabelOf(string tableName, int id)
    {
        if (!_tablesByClrName.TryGetValue(tableName, out var table) || table.IdProperty == null)
        {
            return null;
        }

        if (!table.RowsById.TryGetValue(id, out var row))
        {
            return $"<missing {tableName}:{id}>";
        }

        EnsureLabeled(table);
        return $"{tableName}:{row.Label}";
    }

    private string LabelOf(IEntityType principalType, int id)
    {
        var root = principalType.GetRootType();
        var table = _tablesByType[root];

        if (!table.RowsById.TryGetValue(id, out var row))
        {
            return $"<missing {table.Name}:{id}>";
        }

        EnsureLabeled(table);
        return $"{table.Name}:{row.Label}";
    }

    /// <summary>
    /// Resolves a non-FK int column by naming convention, e.g. "LogoMediaFileId" or "PrimaryExchangeRateCurrencyId".
    /// </summary>
    private string LabelByConvention(string propertyName, int id)
    {
        if (id == 0)
        {
            return null;
        }

        var table = FindConventionTable(propertyName);
        return table != null ? LabelOf(table.Name, id) : null;
    }

    private Table FindConventionTable(string propertyName)
    {
        // Numbered references like "ProductId1" and "ProductId2" of related and cross-sell products.
        propertyName = propertyName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

        if (!propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var stem = propertyName[..^2];
        var alias = ConventionAliases.FirstOrDefault(x => stem.EndsWith(x.Key, StringComparison.OrdinalIgnoreCase));
        if (alias.Key != null)
        {
            stem = alias.Value;
        }

        return _tablesByClrName.Values
            .Where(x => x.IdProperty != null && stem.EndsWith(x.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Name.Length)
            .FirstOrDefault();
    }

    #endregion

    #region Serialization

    private JObject SerializeRow(Row row)
    {
        var obj = new JObject { ["$key"] = row.Label };

        foreach (var property in row.Type.GetProperties().OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (property == row.Table.IdProperty)
            {
                continue;
            }

            obj[property.Name] = row.Normalize(property);
        }

        return obj;
    }

    private JToken NormalizeDate(DateTime value)
    {
        var diff = value - _seededAtUtc;
        if (Math.Abs(diff.TotalDays) <= 2)
        {
            return "<now>";
        }

        if (Math.Abs(diff.TotalDays) <= 400)
        {
            var months = (int)Math.Round(diff.TotalDays / 30.44);
            return $"<now{months:+0;-0}M>";
        }

        return value.ToString("yyyy-MM-ddTHH:mm:ss");
    }

    [GeneratedRegex(@"^(?<prefix>.*?)(?<name>[a-z]+id)(?<id>\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex IdSuffixRegex();

    private string NormalizeSettingName(string name)
    {
        // E.g. "tax.taxprovider.fixedrate.taxcategoryid5"
        var match = IdSuffixRegex().Match(name);
        if (match.Success)
        {
            var label = LabelByConvention(match.Groups["name"].Value, int.Parse(match.Groups["id"].Value));
            if (label != null)
            {
                return match.Groups["prefix"].Value + match.Groups["name"].Value + "{" + label + "}";
            }
        }

        return name;
    }

    private JToken NormalizeSettingValue(Row row, string value)
    {
        var name = (string)row.Values.First(x => x.Key.Name == "Name").Value;
        if (RandomSettings.Contains(name))
        {
            return "<random>";
        }

        var propertyName = name[(name.LastIndexOf('.') + 1)..];

        if (int.TryParse(value, out var id))
        {
            var label = LabelByConvention(propertyName, id);
            if (label != null)
            {
                return label;
            }
        }

        return value;
    }

    private string NormalizeIdList(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return string.Join(",", value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var id) ? (LabelOf("MediaFile", id) ?? x) : x));
    }

    /// <summary>
    /// Replaces attribute and attribute value IDs in a raw attribute selection (JSON or XML).
    /// </summary>
    private string NormalizeRawAttributes(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.TrimStart().StartsWith('{'))
        {
            return value;
        }

        var token = JToken.Parse(value);
        var attributes = token["Attributes"] ?? token["attributes"];
        if (attributes is not JArray array)
        {
            return value;
        }

        foreach (var attribute in array.OfType<JObject>())
        {
            var keyToken = attribute["Key"] ?? attribute["key"];
            var valueToken = attribute["Value"] ?? attribute["value"];

            if (keyToken?.Type == JTokenType.Integer)
            {
                attribute[keyToken.Path.EndsWith("key") ? "key" : "Key"] = LabelOf("ProductVariantAttribute", keyToken.Value<int>());
            }

            if (valueToken is JArray values)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    if (values[i].Type == JTokenType.Integer)
                    {
                        values[i] = LabelOf("ProductVariantAttributeValue", values[i].Value<int>());
                    }
                    else if (values[i].Type == JTokenType.String && int.TryParse(values[i].Value<string>(), out var valueId))
                    {
                        values[i] = LabelOf("ProductVariantAttributeValue", valueId);
                    }
                }
            }
        }

        return token.ToString(Formatting.None);
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    #endregion

    #region Nested types

    private sealed class Table(string name, IEntityType entityType, IProperty idProperty)
    {
        public string Name { get; } = name;
        public string FileName { get; } = name;
        public IEntityType EntityType { get; } = entityType;
        public IProperty IdProperty { get; } = idProperty;
        public List<Row> Rows { get; } = [];
        public Dictionary<int, Row> RowsById { get; } = [];
        public bool IsLabeled { get; set; }
    }

    private sealed class Row
    {
        private readonly SeedSnapshotWriter _writer;
        private string _contentHash;

        public Row(SeedSnapshotWriter writer, Table table, IEntityType type, Dictionary<IProperty, object> values)
        {
            _writer = writer;
            Table = table;
            Type = type;
            Values = values;
        }

        public Table Table { get; }
        public IEntityType Type { get; }
        public Dictionary<IProperty, object> Values { get; }
        public string Label { get; set; }

        /// <summary>
        /// Hash over all plain values (no references), used for fallback labels and tie-breaking.
        /// </summary>
        public string ContentHash
        {
            get
            {
                if (_contentHash == null)
                {
                    var sb = new StringBuilder();
                    foreach (var property in Type.GetProperties().OrderBy(x => x.Name, StringComparer.Ordinal))
                    {
                        if (property == Table.IdProperty
                            || property.IsForeignKey()
                            || IsReference(property)
                            || IdDerivedProperties.Contains((Table.Name, property.Name)))
                        {
                            continue;
                        }

                        // Normalized, so that IDs embedded in strings (e.g. RawAttributes) do not affect the hash.
                        sb.Append(property.Name).Append('=').Append(Normalize(property)?.ToString(Formatting.None)).Append(';');
                    }

                    _contentHash = Hash(sb.ToString());
                }

                return _contentHash;
            }
        }

        public string Get(string propertyName)
        {
            var property = Type.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Key property '{propertyName}' not found on '{Type.Name}'.");

            var token = Normalize(property);
            return token == null || token.Type == JTokenType.Null ? string.Empty : token.ToString(Formatting.None).Trim('"');
        }

        public string First(params string[] propertyNames)
            => propertyNames.Select(Get).FirstOrDefault(x => x.Length > 0) ?? string.Empty;

        public string Join(params string[] propertyNames)
            => string.Join("|", propertyNames.Select(Get));

        public JToken Normalize(IProperty property)
        {
            var value = Values[property];
            if (value == null)
            {
                return JValue.CreateNull();
            }

            if (IdDerivedProperties.Contains((Table.Name, property.Name)))
            {
                return "<id-derived>";
            }

            if (value is string text)
            {
                value = text.Replace("\r\n", "\n");
            }

            // Foreign keys.
            var fk = property.GetContainingForeignKeys().FirstOrDefault();
            if (fk != null && value is int fkId)
            {
                return fkId == 0 ? 0 : _writer.LabelOf(fk.PrincipalEntityType, fkId);
            }

            // Polymorphic references.
            foreach (var (group, idName) in PolymorphicRefs)
            {
                if (property.Name == idName && value is int entityId && Type.FindProperty(group) is IProperty groupProperty)
                {
                    var groupName = Values[groupProperty] as string;
                    return (groupName != null ? _writer.LabelOf(groupName, entityId) : null) ?? (JToken)entityId;
                }
            }

            // Conventional, non-FK ID columns.
            if (value is int conventionalId && property != Table.IdProperty)
            {
                var label = _writer.LabelByConvention(property.Name, conventionalId);
                if (label != null)
                {
                    return label;
                }
            }

            if (Table.Name == "Setting" && value is string settingString)
            {
                return property.Name switch
                {
                    "Name" => _writer.NormalizeSettingName(settingString),
                    "Value" => _writer.NormalizeSettingValue(this, settingString),
                    _ => settingString
                };
            }

            if (Table.Name == "ProductVariantAttributeCombination" && value is string combinationString)
            {
                return property.Name switch
                {
                    "RawAttributes" => _writer.NormalizeRawAttributes(combinationString),
                    "AssignedMediaFileIds" => _writer.NormalizeIdList(combinationString),
                    _ => combinationString
                };
            }

            return NormalizePlain(property);
        }

        private bool IsReference(IProperty property)
        {
            if (PolymorphicRefs.Any(x => x.Id == property.Name && Type.FindProperty(x.Group) != null))
            {
                return true;
            }

            return Values[property] is int && _writer.FindConventionTable(property.Name) != null;
        }

        private JToken NormalizePlain(IProperty property)
        {
            var value = Values[property];

            return value switch
            {
                null => JValue.CreateNull(),
                _ when SecretProperties.Contains(property.Name) => "<secret>",
                Guid guid => guid == Guid.Empty ? Guid.Empty.ToString() : "<guid>",
                DateTime dt => _writer.NormalizeDate(dt),
                DateTimeOffset dto => _writer.NormalizeDate(dto.UtcDateTime),
                byte[] bytes => "<bytes:" + Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant() + ">",
                // Multiline literals inherit the line endings of the seed source files (CRLF on Windows, LF elsewhere).
                string s => s.Replace("\r\n", "\n"),
                Enum e => e.ToString(),
                _ => JToken.FromObject(value)
            };
        }
    }

    #endregion
}
