using System.Linq;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Tests.Platform.Identity;

[TestFixture]
public class CustomerUsernameIndexTests : ServiceTestBase
{
    [Test]
    public void Customer_username_has_a_unique_database_index()
    {
        var customer = DbContext.Model.FindEntityType(typeof(Customer));
        Assert.That(customer, Is.Not.Null);

        var usernameIndexes = customer.GetIndexes().Where(x =>
            x.Properties.Count == 1 && x.Properties[0].Name == nameof(Customer.Username)).ToArray();

        Assert.That(usernameIndexes, Has.Length.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(usernameIndexes[0].IsUnique, Is.True,
                "The database must prevent two concurrent registrations from creating duplicate usernames.");
            Assert.That(usernameIndexes[0].GetDatabaseName(), Is.EqualTo("IX_Customer_Username"));
            Assert.That(customer.FindProperty(nameof(Customer.Username))?.IsNullable, Is.True,
                "Anonymous/guest customers must still be allowed without usernames.");
        }
    }

    [Test]
    public void Username_unique_index_does_not_alter_email_index()
    {
        var customer = DbContext.Model.FindEntityType(typeof(Customer));
        var email = customer.GetIndexes().Single(x =>
            x.Properties.Count == 1 && x.Properties[0].Name == nameof(Customer.Email));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(email.IsUnique, Is.False, "This issue only changes username uniqueness.");
            Assert.That(email.GetDatabaseName(), Is.EqualTo("IX_Customer_Email"));
        }
    }
}
