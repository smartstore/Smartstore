#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using NUnit.Framework;
using Smartstore.ComponentModel;

namespace Smartstore.Tests.ComponentModel;

[TestFixture]
public class HybridExpandoTests
{
    private sealed class Poco
    {
        public string Name { get; set; } = "initial";
        public int Age { get; set; } = 10;

        public string Echo(string input) => "Echo:" + input;
    }

    [Test]
    public void Can_read_write_dynamic_properties()
    {
        dynamic exp = new HybridExpando();

        exp.Foo = "bar";
        Assert.That((string)exp.Foo, Is.EqualTo("bar"));

        exp.Count = 3;
        Assert.That((int)exp.Count, Is.EqualTo(3));
    }

    [Test]
    public void Indexer_reads_dynamic_properties_and_throws_for_missing_key()
    {
        var exp = new HybridExpando();
        exp["Foo"] = "bar";

        Assert.That(exp["Foo"], Is.EqualTo("bar"));
        Assert.That(() => _ = exp["Missing"], Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void Can_access_wrapped_instance_properties_via_dynamic()
    {
        dynamic exp = new HybridExpando(new Poco());

        Assert.That((string)exp.Name, Is.EqualTo("initial"));

        exp.Name = "changed";
        Assert.That((string)exp.Name, Is.EqualTo("changed"));
    }

    [Test]
    public void Can_override_instance_property_with_dictionary_value()
    {
        // Dictionary values win because TryGetMemberCore checks Properties first.
        dynamic exp = new HybridExpando(new Poco());
        exp.Name = "from-instance";

        exp.Override("Name", "from-dictionary");
        Assert.That((string)exp.Name, Is.EqualTo("from-dictionary"));
    }

    [Test]
    public void ReturnNullWhenFalsy_returns_null_for_falsy_values()
    {
        dynamic exp = new HybridExpando(returnNullWhenFalsy: true);

        exp.Zero = 0;
        exp.Empty = "";
        exp.False = false;

        Assert.That(exp.Zero, Is.Null);
        Assert.That(exp.Empty, Is.Null);
        Assert.That(exp.False, Is.Null);
    }

    [Test]
    public void Can_invoke_wrapped_instance_method()
    {
        dynamic exp = new HybridExpando(new Poco());

        var result = (string)exp.Echo("x");
        Assert.That(result, Is.EqualTo("Echo:x"));
    }

    [Test]
    public void PropertyChanged_is_raised_on_set_when_value_changes()
    {
        dynamic exp = new HybridExpando();
        var notify = (INotifyPropertyChanged)exp;

        string? lastName = null;
        notify.PropertyChanged += (_, e) => lastName = e.PropertyName;

        exp.Foo = "bar";

        Assert.That(lastName, Is.EqualTo("Foo"));
    }

    [Test]
    public void PropertyChanged_is_not_raised_when_setting_same_value()
    {
        dynamic exp = new HybridExpando();
        var notify = (INotifyPropertyChanged)exp;

        var count = 0;
        notify.PropertyChanged += (_, __) => count++;

        exp.Foo = "bar";
        exp.Foo = "bar";

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void OptMembers_allow_filters_instance_properties()
    {
        var obj = new Poco { Name = "n", Age = 1 };

        dynamic exp = new HybridExpando(
            obj,
            optMembers: new[] { "Name" },
            optMethod: MemberOptMethod.Allow);

        Assert.That((string)exp.Name, Is.EqualTo("n"));
        Assert.That(() => _ = exp.Age, Throws.Exception); // RuntimeBinderException (type differs per runtime)
    }

    [Test]
    public void OptMembers_disallow_filters_instance_properties()
    {
        var obj = new Poco { Name = "n", Age = 1 };

        dynamic exp = new HybridExpando(
            obj,
            optMembers: new[] { "Age" },
            optMethod: MemberOptMethod.Disallow);

        Assert.That((string)exp.Name, Is.EqualTo("n"));
        Assert.That(() => _ = exp.Age, Throws.Exception); // RuntimeBinderException (type differs per runtime)
    }

    [Test]
    public void IDictionary_TryGetValue_does_not_throw_and_returns_correct_result()
    {
        IDictionary<string, object?> dict = new HybridExpando();
        dict["Foo"] = "bar";

        Assert.That(dict.TryGetValue("Foo", out var v1), Is.True);
        Assert.That(v1, Is.EqualTo("bar"));

        Assert.That(dict.TryGetValue("Missing", out var v2), Is.False);
        Assert.That(v2, Is.Null);
    }

    [Test]
    public void GetDynamicMemberNames_includes_dictionary_and_instance_members_without_duplicates()
    {
        var obj = new Poco();
        var exp = new HybridExpando(obj);

        // Add a dictionary entry that shadows an instance property name.
        exp.Properties["Name"] = "shadow";

        var names = exp.GetDynamicMemberNames().ToList();

        Assert.That(names, Does.Contain("Name"));
        Assert.That(names, Does.Contain("Age"));
        Assert.That(names.Count(n => n == "Name"), Is.EqualTo(1));
    }
}