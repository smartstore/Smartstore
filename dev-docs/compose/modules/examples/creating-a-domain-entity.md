# 🐥 Creating a Domain entity

Domain entities provide a way to add your own tables to the Smartstore database. In this tutorial, you will add a simple notification system to your [Hello World](../tutorials/building-a-simple-hello-world-module.md) module.

{% hint style="info" %}
This tutorial describes working in _modules_, but domain entities can also be added to the _core_.
{% endhint %}

## Preparing the table

### Table schema

A simple notification might have the following properties:

* A unique identifier (`Id`, type: number)
* An author, represented by the customer ID (`AuthorId`, type: number)
* A timestamp (`Published`, type: date-time)
* A message (`Message`, type: string)

Here is what the table might look like:

<table><thead><tr><th width="81">Id</th><th width="93">Author</th><th width="271">Published</th><th>Messaage</th></tr></thead><tbody><tr><td>1</td><td>543</td><td>2022-12-12 11:48:08.9937258</td><td>Hello World!</td></tr><tr><td>2</td><td>481</td><td>2022-12-13 19:02:55.7695421</td><td>What a beautiful day it is <span data-gb-custom-inline data-tag="emoji" data-code="1f604">😄</span></td></tr></tbody></table>

### Create the Domain entity

#### Overview

The _domain_ object is an abstract data structure that has all the properties of the entity it describes. _Entity Framework_ automates the mapping between domain objects and database tables.

Specify the table name and the indexes using [Code First Data Annotations](https://learn.microsoft.com/en-us/ef/ef6/modeling/code-first/data-annotations) and add the properties that represent your database columns.

```csharp
// Outside the class.
// Specify your table name. By convention, the entity name is used.
[Table("TableNameInDatabase")]

// Declare an index.
[Index(nameof(PropertyName), Name = "IX_ClassName_PropertyName")]

// Inside the class.
// Define some columns.
public int ColumnA { get; set; }

public bool ColumnB { get; set; } = true;
```

#### Implementation

Add the _Notification.cs_ file to the new _Domain_ directory and do the following

* Specify the table name `Notification`.
* Declare `AuthorId` and `Published` as indexes.
* Implement the abstract `BaseEntity` class.
* Add the `AuthorId`, `Published` and `Message` properties.

{% hint style="info" %}
There is no need to declare an identifier property, because implementing the abstract `BaseEntity` class automatically adds an `Id` property.
{% endhint %}

Your `Notification` class should look something like this:

{% code title="Notification.cs" %}
```csharp
[Table("Notification")]
[Index(nameof(AuthorId), Name = "IX_Notification_AuthorId")]
[Index(nameof(Published), Name = "IX_Notification_Published")]
public class Notification : BaseEntity
{
    public int AuthorId { get; set; }

    public DateTime Published { get; set; }

    [MaxLength]
    public string Message { get; set; }
}
```
{% endcode %}

This represents the `Notification` table with the three columns: `AuthorId`, `Published` and `Message`. The `MaxLength` attribute will truncate `Message` to the maximum supported length of strings allowed in a property. Because you will often search for notifications based on either `AuthorId` or `Published`, these are defined as indexes.

### Create the Migration

To add the `Notification` table to the Smartstore database, you must create a migration. The migration framework creates the table at application startup. In this tutorial, you will only override the `Up` method of the abstract `MigrationBase` class.

{% hint style="info" %}
To learn more, see [Migrations](../../../framework/platform/database-migrations.md).
{% endhint %}

Create the _Migrations_ directory and add the migration class whose name includes the current date, _YYYYMMDDHHMMSS\_Initial.cs_. Add the following attribute to each class

```csharp
// Use the current date and time [YYYY-MM-DD HH:MM:SS]
[MigrationVersion("2022-12-14 10:34:22", "HelloWorld: Initial")]
```

The class must inherit from the `Migration` base class to have access to the SQL database schema helper methods such as `Create`, `Remove`, or `Update`. You can now use these methods to check if the table already exists, and if it does not, to create it.

```csharp
var tableName = "Notification";

if (!Schema.Table(tableName).Exists())
{
    Create.Table(tableName);
}
```

To add columns, set indexes, and specify primary keys, you can simply chain the following _FluentMigrator_ methods:

| Method         | Description                                            |
| -------------- | ------------------------------------------------------ |
| `WithColumn`   | Defines a new column.                                  |
| `WithIdColumn` | Defines an id column that will act as the primary key. |

You can define a column type (Boolean, Integer, String, Date, Currency, etc.) and declare it as (not) nullable, unique, a primary key, indexed, etc.

```csharp
Create.Table(tableName)
    .WithIdColumn()
    .WithColumn(nameof(Notification.AuthorId))
        .AsInt32()
        .NotNullable()
        .Indexed("IX_Notification_AuthorId")
    .WithColumn(nameof(Notification.Published))
        .AsDateTime2()
        .NotNullable()
        .Indexed("IX_Notification_Published")
    .WithColumn(nameof(Notification.Message))
        .AsMaxString()
        .NotNullable();
```

{% hint style="info" %}
It's best to use the `DateTime2` type instead of `DateTime`. It has an extended range and higher precision.
{% endhint %}

The class should look like this:

{% code title="20221214103422_Initial.cs" %}
```csharp
[MigrationVersion("2022-12-14 10:34:22", "HelloWorld: Initial")]
public class _20221214103422_Initial : Migration
{
    public override void Up()
    {
        // The table name is taken from Domain->Attribute->Table
        var tableName = "Notification";
        
        if (!Schema.Table(tableName).Exists())
        {
            Create.Table(tableName)
                .WithIdColumn() // Adds the Id property as the primary key.
                .WithColumn(nameof(Notification.AuthorId))
                    .AsInt32()
                    .NotNullable()
                    .Indexed("IX_Notification_AuthorId")
                .WithColumn(nameof(Notification.Published))
                    .AsDateTime2()
                    .NotNullable()
                    .Indexed("IX_Notification_Published")
                .WithColumn(nameof(Notification.Message))
                    .AsMaxString()
                    .NotNullable();
        }
    }

    public override void Down()
    {
        // Ignore this for now.
    }
}
```
{% endcode %}

## Providing table access in modules

Now that you have the table set up, you need to give your module access to it. To access the table from a `SmartDbContext` instance you need to add the following two files:

* _Startup.cs_ in the root of your directory
* _SmartDbContextExtensions.cs_ in the _Extensions_ directory (must be created)

Create the static `SmartDbContextExtension` class and add the following method:

```csharp
public static DbSet<Notification> Notifications(this SmartDbContext db)
    => db.Set<Notification>();
```

The Startup class inherits from the `StarterBase` class and contains the following lines:

{% code title="Startup.cs" %}
```csharp
public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
{
    services.AddTransient<IDbContextConfigurationSource<SmartDbContext>, SmartDbContextConfigurer>();
}

private class SmartDbContextConfigurer : IDbContextConfigurationSource<SmartDbContext>
{
    public void Configure(IServiceProvider services, DbContextOptionsBuilder builder)
    {
        builder.UseDbFactory(b => 
        {
            b.AddModelAssembly(GetType().Assembly);
        });
    }
}
```
{% endcode %}

Now you can access the entities stored in your table using the `SmartDbContext`.

```csharp
var messages = await _db.Notifications()
    .Where(x => x.Message.Length > 0)
    .FirstOrDefaultAsync();
```

{% hint style="info" %}
It is not necessary to add the `SmartDbContext` extension. You can just as easily access the entity set using `SmartDbContext.Set<TEntity>()` method.
{% endhint %}

## Next steps

The following steps are included in the module code:

1. Add a [configuration view](../tutorials/building-a-simple-hello-world-module.md#adding-configuration). Configure the number of days to display a notification.
2. Display the notification as [a widget](creating-a-widget-provider.md). This way, you can place it anywhere you want in the store.
3. Add a **new notification** button. Let the current admin create a message.
4. Schedule a task to purge the table. This will increase database speed by removing old, unnecessary messages from your table.

### Further ideas

There are many other things you can do with this module:

* Add a _Moderator_ `CustomerRole` and a separate view to allow certain users to create notifications.
* Add categories to your notifications. Display specific notifications on different pages.
* Make your entities accessible through the [Web API](../../../framework/web-api/web-api-in-detail.md#web-api-and-modules).

## Conclusion

In this tutorial, you learned how to:

* Adding a Table to the Smartstore Database
* Create a migration for it
* Extend `SmartDbContext` with your tables

{% hint style="info" %}
The code for [this tutorial](https://github.com/smartstore/dev-docs-code-examples/tree/main/src/MyOrg.DomainTutorial) can be found in the examples repository.
{% endhint %}
