# Type conversion

Smartstore ships with a flexible conversion framework so strings, numbers, collections and even complex objects can be converted at runtime. The system is built around `ITypeConverter` implementations that are discovered through a factory.

## Built‑in converters
`TypeConverterFactory` maintains a list of provider classes that supply converters for a target type. By default it wires up providers for attribute based converters, simple primitives, dictionaries and collections【F:src/Smartstore/ComponentModel/TypeConverterFactory.cs†L16-L21】. A request for a converter iterates these providers and falls back to `DefaultTypeConverter` when none matches【F:src/Smartstore/ComponentModel/TypeConverterFactory.cs†L45-L59】.

`ConvertUtility.TryConvert` uses that factory to perform the actual conversion. It first looks for a converter for the destination type and, if none fits, tries the source type while honoring the supplied culture【F:src/Smartstore/Utilities/ConvertUtility.cs†L71-L88】. For convenience, every object exposes `Convert<T>` extension methods that call into `ConvertUtility`【F:src/Smartstore/Extensions/ObjectExtensions.cs†L11-L19】.

## Example
```csharp
// comma separated string to a list of integers
var ids = "1,2,3".Convert<IList<int>>();

// dictionary to a POCO
var dict = new Dictionary<string, object?> { ["Name"] = "Demo", ["Age"] = 42 };
var person = dict.Convert<Person>();
```

## Custom converters
Implementing `ITypeConverter` allows converting arbitrary types. Converters can be supplied by a custom `ITypeConverterProvider` or attached to a type via `[TypeConverter]` so the factory can discover them automatically. A converter should report supported source/target types via `CanConvertFrom`/`CanConvertTo` and implement the conversion logic in `ConvertFrom`/`ConvertTo`.

Dictionary converters, for example, can materialize `IDictionary<string, object>` from a `JObject` or plain object and also project a dictionary back into a POCO with a default constructor【F:src/Smartstore/ComponentModel/TypeConverters/DictionaryTypeConverter.cs†L19-L31】.

## Extending
Additional providers may be added to `TypeConverterFactory.Providers` at application start to register module specific converters. This makes it possible to support domain specific formats or legacy representations without touching core code.