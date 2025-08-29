# ✔ Examples

### Partially update an address

```
PATCH http://localhost:59318/odata/v1/Addresses(1)
?SmApiFulfillCountry=US&SmApiFulfillStateProvince=NY

{ 
    "City": "New York",
    "Address1": "21 West 52nd Street",
    "ZipPostalCode": "10021",
    "FirstName": "John", 
    "LastName": "Doe"
}
```

The `SmApiFulfillCountry` and `SmApiFulfillStateProvince` options are used to update the country (USA) and province (New York). This avoids extra querying of the country and province records and passing the IDs in the request body.

### Get the store ID from its name

```
GET http://localhost:59318/odata/v1/Stores
?$top=1&$filter=Name eq 'my nice store'&$select=Id
```

Note the `select` option, that tells OData to simply return the `Id` property.

### Getting localized property values

```
GET http://localhost:59318/odata/v1/LocalizedProperties?$filter=LocaleKeyGroup eq
'Product' and EntityId eq 224 and Language/LanguageCulture eq 'de-DE'
```

This is where the OData filter enters the equation. The request filters all German properties of a product with the ID `224`. LocaleKeyGroup is typically the entity name (Product, Category, Manufacturer, ProductBundleItem, DeliveryTime etc.). The result looks like this:

```json
{
  "@odata.context": "http://localhost:59318/odata/v1/$metadata#LocalizedProperties",
  "value": [
    {
      "EntityId": 224,
      "LanguageId": 1,
      "LocaleKeyGroup": "Product",
      "LocaleKey": "ShortDescription",
      "LocaleValue": "Meine Kurzbeschreibung",
      "IsHidden": false,
      "CreatedOnUtc": "2022-11-03T11:01:13.713Z",
      "UpdatedOnUtc": null,
      "CreatedBy": null,
      "UpdatedBy": null,
      "TranslatedOnUtc": null,
      "MasterChecksum": null,
      "Id": 1529
    },
    {
      "EntityId": 224,
      "LanguageId": 1,
      "LocaleKeyGroup": "Product",
      "LocaleKey": "Name",
      "LocaleValue": "Eine andere Kurzbeschreibung",
      "IsHidden": false,
      "CreatedOnUtc": "2022-12-05T10:18:45.63997Z",
      "UpdatedOnUtc": null,
      "CreatedBy": null,
      "UpdatedBy": null,
      "TranslatedOnUtc": null,
      "MasterChecksum": null,
      "Id": 2584
    }
  ]
}
```

`LocaleKey` is the context property name (the name and full description of the product entity). `LocaleValue` offers the localized value.
