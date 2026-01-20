#nullable enable

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Smartstore.Json;

namespace Smartstore.Tests.Json;

[TestFixture]
public class DefaultJsonSerializerTests
{
    private DefaultJsonSerializer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new DefaultJsonSerializer();
    }

    [Test]
    public void CanSerialize_Null_ReturnsTrue()
    {
        Assert.That(_sut.CanSerialize((object?)null), Is.True);
    }

    [Test]
    public void CanSerialize_Task_IsFalse()
    {
        Assert.That(_sut.CanSerialize(typeof(Task)), Is.False);
    }

    [Test]
    public void CanSerialize_TaskOfT_IsFalse()
    {
        Assert.That(_sut.CanSerialize(typeof(Task<int>)), Is.False);
    }

    [Test]
    public void CanDeserialize_Task_IsFalse()
    {
        Assert.That(_sut.CanDeserialize(typeof(Task)), Is.False);
    }

    [Test]
    public void CanDeserialize_TaskOfT_IsFalse()
    {
        Assert.That(_sut.CanDeserialize(typeof(Task<int>)), Is.False);
    }

    [Test]
    public void TrySerialize_Null_NotCompressed_ReturnsLiteralNullBytes()
    {
        var ok = _sut.TrySerialize(null, compress: false, out var bytes);

        Assert.That(ok, Is.True);
        Assert.That(bytes, Is.Not.Null);
        Assert.That(bytes, Is.EqualTo(Encoding.UTF8.GetBytes("null")));
    }

    [Test]
    public void TryDeserialize_LiteralNull_ReturnsNull()
    {
        var ok = _sut.TryDeserialize(typeof(object), Encoding.UTF8.GetBytes("null"), uncompress: false, out var result);

        Assert.That(ok, Is.True);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryDeserialize_InvalidJson_ReturnsFalse()
    {
        var ok = _sut.TryDeserialize(typeof(Dictionary<string, object?>), Encoding.UTF8.GetBytes("{"), uncompress: false, out var result);

        Assert.That(ok, Is.False);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TrySerialize_ThenTryDeserialize_Roundtrip_Works_ForSimpleObject()
    {
        var input = new TestPoco { Name = "abc", Age = 5 };

        var ok1 = _sut.TrySerialize(input, compress: false, out var bytes);
        var ok2 = _sut.TryDeserialize(typeof(TestPoco), bytes!, uncompress: false, out var result);

        Assert.That(ok1, Is.True);
        Assert.That(ok2, Is.True);
        Assert.That(result, Is.Not.Null);

        var output = (TestPoco)result!;
        Assert.That(output.Name, Is.EqualTo(input.Name));
        Assert.That(output.Age, Is.EqualTo(input.Age));
    }

    [Test]
    public void TrySerialize_PolymorphicDictionary_WrapArrays_Roundtrip_Works()
    {
        var dict = new Dictionary<string, object?>
        {
            ["str"] = "x",
            ["bool"] = true,
            ["arr"] = new List<object?> { 1, "a" },
            ["obj"] = new TestPoco { Name = "n", Age = 1 }
        };

        var ok1 = _sut.TrySerialize(dict, compress: false, out var bytes);
        var ok2 = _sut.TryDeserialize(typeof(Dictionary<string, object?>), bytes!, uncompress: false, out var result);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ok1, Is.True);
            Assert.That(ok2, Is.True);
            Assert.That(result, Is.Not.Null);
        }

        var output = (Dictionary<string, object?>)result!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(output["str"], Is.EqualTo("x"));
            Assert.That(output["bool"], Is.EqualTo(true));
            Assert.That(output.ContainsKey("arr"), Is.True);
            Assert.That(output.ContainsKey("obj"), Is.True);
        }
    }

    private sealed class TestPoco
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}
