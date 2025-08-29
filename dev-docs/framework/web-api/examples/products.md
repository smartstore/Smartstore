# ✔️ Products

### **Get a product with name "iPhone"**

```
GET http://localhost:59318/odata/v1/Products?$top=1&$filter=Name eq 'iPhone'
```

### **Get child products of grouped product with ID 210**

```
GET http://localhost:59318/odata/v1/Products?$filter=ParentGroupedProductId eq 210
```

### Calculate the final price of a product

```
POST http://localhost:59318/odata/v1/Products(123)/CalculatePrice
{ "forListing": false, "quantity": 1 }
```

{% code title="Response" %}
```json
{
    "@odata.context": "http://localhost:59318/odata/v1/$metadata
    #Smartstore.Web.Api.Models.Catalog.CalculatedProductPrice",
    "ProductId": 123,
    "CurrencyId": 5,
    "CurrencyCode": "EUR",
    "FinalPrice": 43.07800000,
    "RegularPrice": null,
    "RetailPrice": 47.58810000,
    "OfferPrice": null,
    "ValidUntilUtc": null,
    "PreselectedPrice": null,
    "LowestPrice": null,
    "DiscountAmount": 0,
    "Saving": {
        "HasSaving": true,
        "SavingPrice": 47.58810000,
        "SavingPercent": 9,
        "SavingAmount": 4.51010000
    }
}
```
{% endcode %}

### **Assign category with ID 9 to product with ID 1**

```
POST http://localhost:59318/odata/v1/Products(1)/ProductCategories(9)
{ "DisplayOrder": 5, "IsFeaturedProduct": true }
```

### **Assign manufacturer with ID 12 to product with ID 1**

```
POST http://localhost:59318/odata/v1/Products(1)/ProductManufacturers(12)
{ "DisplayOrder": 1, "IsFeaturedProduct": false }
```

{% hint style="info" %}
* The request body is optional but sending the content type header with _application/json_ is required otherwise `404 Not Found` is returne&#x64;_._
* Use the DELETE method to remove an assignment.
* Omit the category / manufacturer identifier if you want to remove all related assignments for a product.
* It doesn't matter if one of the assignments already exists. The Web API automatically ensures that a product has no duplicate category or manufacturer assignments.
* Such navigation links are only available for a few navigation properties at the moment.
{% endhint %}

### **Delete assignment of image 66 to product 1**

```
DELETE http://localhost:59318/odata/v1/Products(1)/ProductMediaFiles(66)
```

### **Update display order of image assignment 66 at product 1**

```
PATCH http://localhost:59318/odata/v1/Products(1)/ProductMediaFiles(66)
{ "DisplayOrder": 5 }
```

{% hint style="info" %}
66 is the ID of the _ProductMediaFile_, not the ID of the _MediaFile_. _ProductMediaFile_ is a mapping between a product and a media file (image).
{% endhint %}

### Upload product images

Multiple images can be uploaded for a product by a single multipart form data POST request. The product ID can be `0` and the product can be identified by query string parameter _sku_, _gtin_ or _mpn_.

```http
POST http://localhost:59318/odata/v1/Products(1)/SaveFiles

Content-Type: image/jpeg
Content-Disposition: form-data; name="my-file-1"; filename="my-file1.jpg"
<Binary data for my-file1.jpg here (length 503019 bytes)…>

Content-Type: image/jpeg
Content-Disposition: form-data; name="my-file-2"; filename="my-file2.jpg"
<Binary data for my-file2.jpg here (length 50934 bytes)…>

Content-Type: image/jpeg
Content-Disposition: form-data; name="my-file-3"; filename="my-file3.jpg"
<Binary data for my-file3.jpg here (length 175939 bytes)…>
```

{% hint style="info" %}
It doesn't matter if one of the uploaded images already exists. The Web API automatically ensures that a product has no duplicate images by comparing both binary data streams.
{% endhint %}

It is also possible to update or replace an existing image. To do so simply add the file identifier as the `fileId` attribute in the _content disposition_ header of the file. The example updates the picture entity with the Id `6166`.

```
POST http://localhost:59318/odata/v1/Products(0)/SaveFiles?sku=p9658742

Content-Type: image/jpeg
Content-Disposition: form-data; name="img"; filename="new-image.jpg"; fileId="6166"
<Binary data for new-image.jpg here (length 4108730 bytes)…>
```

{% hint style="info" %}
Image uploading can be a resource-intensive process. We recommend the use of the `async` and `await` syntax or any other parallel asynchronous mechanism targeting payload efficiency.
{% endhint %}

### Managing attributes

You can use the following endpoints: `ProductAttributes` (types of attributes), `ProductVariantAttributes` (attribute types mapped to a product), `ProductVariantAttributeValues` (attribute values assigned to a product) and optionally `ProductVariantAttributeCombinations` (additional information for particular attribute combinations). Because managing attributes that way can lead to some extra work, there is an action method `ManageAttributes` that sums up the most important steps.

```json
POST http://localhost:59318/odata/v1/Products(211)/ManageAttributes
{
  "synchronize": true,
  "attributes": [
	{ "name": "Color", "isRequired": false, "values": [
		{ "name": "Red"},
		{ "name": "Green", "isPreSelected": true},
		{ "name": "Blue"}
	]},
	{ "name": "Size", "values": [
		{ "name": "Large"},
		{ "name": "X-Large", "isPreSelected": true }
	]}
]}
```

The request configures a product with the ID `211` with two attributes _Color_ and _Size,_ and its values: _Red, Green, Blue_ and _Large, X-Large_. If **synchronize** is set to `false`, only missing attributes and attribute values are inserted. If set to `true`, existing records are also updated and values not included in the request body are removed from the database. If you pass an empty value array, the attribute and all its values are removed from the product.

### Create attribute combinations

```
POST http://localhost:59318/odata/v1/Products(211)/CreateAttributeCombinations
```

This creates all possible attribute combinations for a product with the ID `211`. As a first step, this action always deletes all existing attribute combinations for the given product.

### Search products

```
POST http://localhost:59318/odata/v1/Products/Search?q=notebook
```

Searches the catalog for products with the term _notebook_. The API expects the same query string parameters used for searching in the frontend of the shop. See [Search query](../../platform/search.md#search-query) for a complete list of all query string parameters.

{% hint style="info" %}
The paging parameters `$top` and `$skip` are ignored. Instead use the query string parameters mentioned in [Search query](../../platform/search.md#search-query) for _page index_ and _page size_. The maximum page size is determined by the same configuration setting that is used for all other API requests.
{% endhint %}
