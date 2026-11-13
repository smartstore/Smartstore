using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using Smartstore.ComponentModel;
using Smartstore.ComponentModel.TypeConverters;
using Smartstore.Domain;
using Smartstore.Utilities;

namespace Smartstore.Tests.ComponentModel;

[TestFixture]
public class PropertyConversionTests
{
    [Test]
    public void JsonPropertyRoundTripsWithoutChangingOtherArrayProperties()
    {
        var source = new StoredOptions { Values = "[\"a,b\",\"quoted \\\"value\\\"\"]", OtherValues = "one,two" };
        var model = MiniMapper.Map<StoredOptions, OptionsModel>(source);

        Assert.That(model.Values, Is.EqualTo(new[] { "a,b", "quoted \"value\"" }));
        Assert.That(model.OtherValues, Is.EqualTo(new[] { "one", "two" }));
        var stored = MiniMapper.Map<OptionsModel, StoredOptions>(model);
        Assert.That(MiniMapper.Map<StoredOptions, OptionsModel>(stored).Values, Is.EqualTo(model.Values));
    }

    [TestCase("")]
    [TestCase("null")]
    [TestCase("[]")]
    public void EmptyJsonProducesAnEmptyArray(string json)
    {
        var model = MiniMapper.Map<StoredOptions, OptionsModel>(new StoredOptions { Values = json });
        Assert.That(model.Values, Is.Empty);
        Assert.That(MiniMapper.Map<OptionsModel, StoredOptions>(model).Values, Is.Empty);
    }

    [Test]
    public void InvalidJsonLeavesTheTargetPropertyUntouched()
    {
        var target = new OptionsModel { Values = new[] { "keep" } };
        MiniMapper.Map(new StoredOptions { Values = "invalid JSON" }, target);
        Assert.That(target.Values, Is.EqualTo(new[] { "keep" }));
    }

    [Test]
    public void SystemPropertyConverterWorksInBothDirections()
    {
        var source = new StoredNumber { Value = "0xff" };
        var model = MiniMapper.Map<StoredNumber, NumberModel>(source);
        Assert.That(model.Value, Is.EqualTo(255));
        Assert.That(MiniMapper.Map<NumberModel, StoredNumber>(model).Value, Is.EqualTo("0xff"));
    }

    [Test]
    public void JsonConverterCanBeDeclaredOnAType()
    {
        var value = "{\"Number\":42}".Convert<JsonValue>();
        Assert.That(value.Number, Is.EqualTo(42));
        Assert.That(value.Convert<string>(), Is.EqualTo("{\"Number\":42}"));
    }

    [Test]
    public void SystemConverterWithTypeConstructorIsCreatedLazilyAndCached()
    {
        TypedEnumConverter.Instances = 0;
        var property = typeof(ConverterOptions).GetProperty(nameof(ConverterOptions.Day));
        var converter = TypeConverterFactory.GetConverter(property);

        Assert.That(TypedEnumConverter.Instances, Is.Zero);
        Assert.That(converter.ConvertFrom(CultureInfo.InvariantCulture, "Monday"), Is.EqualTo(DayOfWeek.Monday));
        Assert.That(converter.ConvertTo(CultureInfo.InvariantCulture, null, DayOfWeek.Monday, typeof(string)), Is.EqualTo("Monday"));
        Assert.That(TypeConverterFactory.GetConverter(property), Is.SameAs(converter));
        Assert.That(TypedEnumConverter.Instances, Is.EqualTo(1));
    }

    [Test]
    public void OverriddenPropertyInheritsItsConverter()
    {
        var converter = TypeConverterFactory.GetConverter(typeof(DerivedOptions).GetProperty(nameof(DerivedOptions.Value)));
        Assert.That(converter.ConvertTo(CultureInfo.InvariantCulture, null, 255, typeof(string)), Is.EqualTo("0xff"));
    }

    [TestCase(nameof(ConverterOptions.Unknown))]
    [TestCase(nameof(ConverterOptions.Empty))]
    [TestCase(nameof(ConverterOptions.Unrelated))]
    public void InvalidConverterDeclarationFallsBackToPropertyType(string propertyName)
    {
        var converter = TypeConverterFactory.GetConverter(typeof(ConverterOptions).GetProperty(propertyName));
        Assert.That(converter.ConvertFrom(CultureInfo.InvariantCulture, "42"), Is.EqualTo(42));
    }

    [Test]
    public void ConverterNameCanBeRelativeToTheDeclaringAssembly()
    {
        var converter = TypeConverterFactory.GetConverter(typeof(ConverterOptions).GetProperty(nameof(ConverterOptions.Relative)));
        Assert.That(converter.ConvertTo(CultureInfo.InvariantCulture, null, 255, typeof(string)), Is.EqualTo("0xff"));
    }

    [Test]
    public void PropertyWithoutAttributeHasNoPropertyConverter()
    {
        Assert.That(TypeConverterFactory.GetConverter(typeof(StoredNumber).GetProperty(nameof(StoredNumber.Value))), Is.Null);
    }

    [Test]
    public void SystemConstructorFailureRemainsDeferred()
    {
        var converter = TypeConverterFactory.GetConverter(typeof(ConverterOptions).GetProperty(nameof(ConverterOptions.Broken)));
        Assert.That(converter, Is.Not.Null);
        Assert.Catch(() => converter.CanConvertFrom(typeof(string)));
    }

    public class ConverterOptions
    {
        [TypeConverter(typeof(TypedEnumConverter))]
        public DayOfWeek Day { get; set; }

        [TypeConverter("Missing.Converter, Missing.Assembly")]
        public int Unknown { get; set; }

        [TypeConverter("")]
        public int Empty { get; set; }

        [TypeConverter(typeof(string))]
        public int Unrelated { get; set; }

        [TypeConverter("Smartstore.Tests.ComponentModel.PropertyConversionTests+HexConverter")]
        public int Relative { get; set; }

        [TypeConverter(typeof(ThrowingConverter))]
        public int Broken { get; set; }
    }

    public class BaseOptions
    {
        [TypeConverter(typeof(HexConverter))]
        public virtual int Value { get; set; }
    }

    public class DerivedOptions : BaseOptions
    {
        public override int Value { get; set; }
    }

    public class TypedEnumConverter : EnumConverter
    {
        public static int Instances;

        public TypedEnumConverter(Type type) : base(type)
        {
            Instances++;
        }
    }

    public class ThrowingConverter : TypeConverter
    {
        public ThrowingConverter()
        {
            throw new InvalidOperationException("Converter construction failed.");
        }
    }

    [TestCase(typeof(int))]
    [TestCase(typeof(DayOfWeek))]
    [TestCase(typeof(object))]
    public void ExplicitSystemConverterCannotBeBypassedByDefaultFastPaths(Type type)
    {
        var converter = new DefaultTypeConverter(type, () => new RejectingConverter());
        object value = type == typeof(DayOfWeek) ? DayOfWeek.Monday : 42;

        Assert.That(converter.CanConvertFrom(typeof(int)), Is.False);
        Assert.That(converter.CanConvertTo(typeof(int)), Is.False);
        Assert.That(converter.CanConvertTo(typeof(string)), Is.False);
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(CultureInfo.InvariantCulture, value));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(CultureInfo.InvariantCulture, 1));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(CultureInfo.InvariantCulture, null));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(CultureInfo.InvariantCulture, null, value, typeof(int)));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(CultureInfo.InvariantCulture, null, value, typeof(string)));
    }

    public class RejectingConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => false;
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => false;

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => throw new NotSupportedException();

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            => throw new NotSupportedException();
    }

    [Test]
    public void PropertyConversionReadsOnceAndUsesDestinationConverter()
    {
        var source = new CountedSource<string> { Data = "0xff" };
        bool success = ConvertUtility.TryConvert(source,
            typeof(CountedSource<string>).GetProperty(nameof(source.Value)),
            typeof(NumberModel).GetProperty(nameof(NumberModel.Value)),
            CultureInfo.InvariantCulture, out var result, out var original);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(255));
        Assert.That(original, Is.EqualTo("0xff"));
        Assert.That(source.Reads, Is.EqualTo(1));
    }

    [Test]
    public void PropertyConversionUsesSourceConverter()
    {
        bool success = ConvertUtility.TryConvert(new NumberModel { Value = 255 },
            typeof(NumberModel).GetProperty(nameof(NumberModel.Value)),
            typeof(StoredNumber).GetProperty(nameof(StoredNumber.Value)),
            null, out var result, out var original);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo("0xff"));
        Assert.That(original, Is.EqualTo(255));
    }

    [Test]
    public void PropertyConversionHonorsCulture()
    {
        var source = new CountedSource<string> { Data = "1,5" };
        bool success = ConvertUtility.TryConvert(source,
            typeof(CountedSource<string>).GetProperty(nameof(source.Value)),
            typeof(ValueTarget<decimal>).GetProperty(nameof(ValueTarget<decimal>.Value)),
            CultureInfo.GetCultureInfo("de-DE"), out var result, out _);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(1.5m));
        Assert.That(source.Reads, Is.EqualTo(1));
    }

    [TestCase(null)]
    [TestCase("unchanged")]
    public void PropertyConversionPreservesNullAndAssignableValues(string value)
    {
        var source = new CountedSource<string> { Data = value };
        bool success = ConvertUtility.TryConvert(source,
            typeof(CountedSource<string>).GetProperty(nameof(source.Value)),
            typeof(StoredNumber).GetProperty(nameof(StoredNumber.Value)),
            null, out var result, out var original);

        Assert.That(success, Is.True);
        Assert.That(result, Is.SameAs(value));
        Assert.That(original, Is.SameAs(value));
        Assert.That(source.Reads, Is.EqualTo(1));
    }

    [Test]
    public void PropertyConversionPreservesDBNullAsOriginalValue()
    {
        var source = new CountedSource<object> { Data = DBNull.Value };
        bool success = ConvertUtility.TryConvert(source,
            typeof(CountedSource<object>).GetProperty(nameof(source.Value)),
            typeof(StoredNumber).GetProperty(nameof(StoredNumber.Value)),
            null, out var result, out var original);

        Assert.That(success, Is.True);
        Assert.That(result, Is.Null);
        Assert.That(original, Is.SameAs(DBNull.Value));
    }

    [TestCase("0xff", 255)]
    [TestCase("invalid", 17)]
    public void MappingReadsGetterOnceEvenWhenConversionFails(string value, int expected)
    {
        var source = new CountedSource<string> { Data = value };
        var target = new NumberModel { Value = 17 };
        MiniMapper.Map(source, target);

        Assert.That(target.Value, Is.EqualTo(expected));
        Assert.That(source.Reads, Is.EqualTo(1));
    }

    [TestCase(0, null)]
    [TestCase(7, 7)]
    public void ForeignKeyNormalizationReadsEachGetterOnce(int value, int? expected)
    {
        var source = new CountedForeignKeys { Data = value };
        var target = new ForeignKeyEntity();
        MiniMapper.Map(source, target);

        Assert.That(target.ParentId, Is.EqualTo(expected));
        Assert.That(target.OtherId, Is.EqualTo(expected));
        Assert.That(source.IntReads, Is.EqualTo(1));
        Assert.That(source.NullableReads, Is.EqualTo(1));
    }

    [Test]
    public void RecursiveFallbackReusesSourceAfterConverterFailure()
    {
        var source = new CountedSource<StoredNumber> { Data = new StoredNumber { Value = "nested" } };
        var target = new RecursiveTarget();
        MiniMapper.Map(source, target);

        Assert.That(target.Value.Value, Is.EqualTo("nested"));
        Assert.That(source.Reads, Is.EqualTo(1));
    }

    [Test]
    public void SourceGetterExceptionsStillPropagate()
    {
        Assert.Throws<TargetInvocationException>(() => MiniMapper.Map(new ThrowingSource(), new NumberModel()));
    }

    public class CountedSource<T>
    {
        public T Data;
        public int Reads;
        public T Value
        {
            get
            {
                Reads++;
                return Data;
            }
        }
    }

    public class ValueTarget<T>
    {
        public T Value { get; set; }
    }

    public class CountedForeignKeys
    {
        public int Data;
        public int IntReads;
        public int NullableReads;
        public int ParentId
        {
            get
            {
                IntReads++;
                return Data;
            }
        }
        public int? OtherId
        {
            get
            {
                NullableReads++;
                return Data;
            }
        }
    }

    public class ForeignKeyEntity : BaseEntity
    {
        public int? ParentId { get; set; }
        public int? OtherId { get; set; }
    }

    public class RecursiveTarget
    {
        [TypeConverter(typeof(FailingConversion))]
        public ValueTarget<string> Value { get; set; }
    }

    public class FailingConversion : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => true;
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => throw new FormatException();
    }

    public class ThrowingSource
    {
        public string Value => throw new InvalidOperationException("Getter failed.");
    }

    public class StoredOptions
    {
        public string Values { get; set; }
        public string OtherValues { get; set; }
    }

    public class OptionsModel
    {
        [TypeConverter(typeof(JsonTypeConverter<string[]>))]
        public string[] Values { get; set; } = [];
        public string[] OtherValues { get; set; }
    }

    public class StoredNumber
    {
        public string Value { get; set; }
    }

    public class NumberModel
    {
        [TypeConverter(typeof(HexConverter))]
        public int Value { get; set; }
    }

    public class HexConverter : Int32Converter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            => destinationType == typeof(string)
                ? "0x" + ((int)value).ToString("x", culture)
                : base.ConvertTo(context, culture, value, destinationType);
    }

    [TypeConverter(typeof(JsonTypeConverter<JsonValue>))]
    public class JsonValue
    {
        public int Number { get; set; }
    }
}
