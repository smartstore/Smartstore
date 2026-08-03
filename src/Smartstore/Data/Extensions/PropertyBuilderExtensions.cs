using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore;

public static class PropertyBuilderExtensions
{
    /// <summary>
    /// Configures the property so that the property value is converted into a JSON string before
    /// writing to the database and converted back into an array of type <typeparamref name="T" /> 
    /// when reading from the database.
    /// </summary>
    /// <param name="property">The property builder for the array property.</param>
    public static PropertyBuilder<T[]> HasJsonConversion<T>(this PropertyBuilder<T[]> property) 
        where T : class
    {
        return property.HasConversion(
            v => JsonSerializer.Serialize(v),
            v => JsonSerializer.Deserialize<T[]>(v),
            new ValueComparer<T[]>(
                (left, right) => left.SequenceEqual(right),
                values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
                values => values.ToArray()));
    }
}
