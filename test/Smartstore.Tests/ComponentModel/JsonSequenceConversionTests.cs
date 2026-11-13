using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Smartstore.ComponentModel;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.Tests.ComponentModel;

[TestFixture]
public class JsonSequenceConversionTests
{
    private const string ValuesJson = "[\"A\",\"a\",\"a,b\",\"quoted \\\"value\\\"\"]";

    [TestCase(typeof(string[]))]
    [TestCase(typeof(List<string>))]
    [TestCase(typeof(HashSet<string>))]
    [TestCase(typeof(IEnumerable<string>))]
    [TestCase(typeof(ICollection<string>))]
    [TestCase(typeof(IList<string>))]
    [TestCase(typeof(IReadOnlyCollection<string>))]
    [TestCase(typeof(IReadOnlyList<string>))]
    [TestCase(typeof(ISet<string>))]
    public void SupportedSequencesRoundTripAndNormalizeEmptyValues(Type type)
    {
        var converter = (ITypeConverter)Activator.CreateInstance(typeof(JsonTypeConverter<>).MakeGenericType(type));
        var value = converter.ConvertFrom(CultureInfo.InvariantCulture, ValuesJson);
        string json = (string)converter.ConvertTo(CultureInfo.InvariantCulture, null, value, typeof(string));
        var roundTrip = (IEnumerable<string>)converter.ConvertFrom(CultureInfo.InvariantCulture, json);

        Assert.That(type.IsInstanceOfType(value), Is.True);
        Assert.That(roundTrip, Is.EquivalentTo(new[] { "A", "a", "a,b", "quoted \"value\"" }));

        foreach (string empty in new[] { null, "", "null", "[]" })
        {
            var collection = converter.ConvertFrom(CultureInfo.InvariantCulture, empty);
            Assert.That(type.IsInstanceOfType(collection), Is.True);
            Assert.That((IEnumerable<string>)collection, Is.Empty);
            Assert.That(converter.ConvertTo(CultureInfo.InvariantCulture, null, collection, typeof(string)), Is.EqualTo(string.Empty));
        }

        Assert.That(converter.ConvertTo(CultureInfo.InvariantCulture, null, null, typeof(string)), Is.EqualTo(string.Empty));
    }

    [Test]
    public void SetUsesTheJsonSerializersDefaultComparer()
    {
        var converter = new JsonTypeConverter<HashSet<string>>();
        var value = (HashSet<string>)converter.ConvertFrom(CultureInfo.InvariantCulture, "[\"A\",\"a\",\"A\"]");
        Assert.That(value.Count, Is.EqualTo(2));
        Assert.That(value.Comparer.Equals("A", "a"), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SerializationDoesNotProbeASingleUseEnumerable(bool empty)
    {
        var value = new SingleUseEnumerable(empty ? [] : new[] { "a,b" });
        var converter = new JsonTypeConverter<IEnumerable<string>>();
        string json = (string)converter.ConvertTo(CultureInfo.InvariantCulture, null, value, typeof(string));

        Assert.That(value.Enumerations, Is.EqualTo(1));
        Assert.That((IEnumerable<string>)converter.ConvertFrom(CultureInfo.InvariantCulture, json),
            Is.EqualTo(empty ? Array.Empty<string>() : new[] { "a,b" }));
    }

    [TestCase("")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("[\"value\"]")]
    public void EnumerableWithoutDeserializationSupportIsRejected(string json)
    {
        var converter = new JsonTypeConverter<SingleUseEnumerable>();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(CultureInfo.InvariantCulture, json));
    }

    [Test]
    public void DictionariesAndScalarsKeepTheirJsonRepresentation()
    {
        var dictionaryConverter = new JsonTypeConverter<Dictionary<string, string>>();
        Assert.That(dictionaryConverter.ConvertFrom(CultureInfo.InvariantCulture, "null"), Is.Null);
        Assert.That(dictionaryConverter.ConvertTo(CultureInfo.InvariantCulture, null, new Dictionary<string, string>(), typeof(string)), Is.EqualTo("{}"));

        var stringConverter = new JsonTypeConverter<string>();
        Assert.That(stringConverter.ConvertFrom(CultureInfo.InvariantCulture, "null"), Is.Null);
        Assert.That(stringConverter.ConvertTo(CultureInfo.InvariantCulture, null, "", typeof(string)), Is.EqualTo("\"\""));

        var scalarConverter = new JsonTypeConverter<int>();
        Assert.That(scalarConverter.ConvertFrom(CultureInfo.InvariantCulture, "42"), Is.EqualTo(42));
    }

    [Test]
    public void ByteArraysRetainTheirBase64AndEmptyValueBehavior()
    {
        var converter = new JsonTypeConverter<byte[]>();
        Assert.That(converter.ConvertFrom(CultureInfo.InvariantCulture, "null"), Is.EqualTo(Array.Empty<byte>()));
        Assert.That(converter.ConvertTo(CultureInfo.InvariantCulture, null, Array.Empty<byte>(), typeof(string)), Is.EqualTo(""));
        string json = (string)converter.ConvertTo(CultureInfo.InvariantCulture, null, new byte[] { 1, 2 }, typeof(string));
        Assert.That(json, Is.EqualTo("\"AQI=\""));
        Assert.That(converter.ConvertFrom(CultureInfo.InvariantCulture, json), Is.EqualTo(new byte[] { 1, 2 }));
    }

    public class SingleUseEnumerable(string[] values) : IEnumerable<string>
    {
        public int Enumerations;

        public IEnumerator<string> GetEnumerator()
        {
            if (++Enumerations > 1)
            {
                throw new InvalidOperationException("The sequence may only be enumerated once.");
            }

            return values.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
