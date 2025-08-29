# ✔ Orders

### **Get the shipping address for an order**

```
GET http://localhost:59318/odata/v1/Orders(145)/ShippingAddress
```

### **Get order items from an order including product data**

```
GET http://localhost:59318/odata/v1/OrderItems?$filter=OrderId eq 145&$expand=Product
```

### **Get orders with the note "mystring"**

```
GET http://localhost:59318/odata/v1/OrderNotes
?$filter=Note eq 'mystring'&$expand=Order
```

### **Get orders without the note "mystring"**

```
GET http://localhost:59318/odata/v1/OrderNotes
?$filter=Note ne 'mystring'&$expand=Order
```

### **Mark an order as paid**

```
POST http://localhost:59318/odata/v1/Orders(145)/PaymentPaid
{ "paymentMethodName": "Payments.Sofortueberweisung" }
```

The example also sets the system name of the payment method for the order to `Payments.Sofortueberweisung`.

### Refund an order

```
POST http://localhost:59318/odata/v1/Orders(146)/PaymentRefund
{ "online": true }
```

The **online** parameter indicates whether to call the related payment gateway to refund the payment. `True` would refund against the payment gateway. `False` just sets the status offline without calling any payment gateway.

### Complete an order

```
POST http://localhost:59318/odata/v1/Orders(147)/CompleteOrder
```

### Download an order as PDF

```
GET http://localhost:59318/odata/v1/Orders/DownloadPdf(id=150)
```

### Add shipment to an order

```
POST http://localhost:59318/odata/v1/Orders(150)/AddShipment
{ "trackingNumber": "987654321", "isShipped": true }
```

The method also adds shipment items for all order items. For example, an order that consists of **product A** with a quantity of 1 and **product B** with a quantity of 2. `AddShipment` then adds a shipment with two shipment items, one for **product A** with a quantity of 1 and one for **product B** with a quantity of 2.

`isShipped` with a value of `true` marks order and shipment as shipped, adds an order note and sends a notification message to the customer that the shipment has been sent.

### Get shipment info

```
GET http://localhost:1260/odata/v1/Orders/GetShipmentInfo(id=150)
```

{% code title="Response" %}
```json
{
    "@odata.context": "http://localhost:59318/odata/v1/$metadata
    #Smartstore.Web.Api.Models.Checkout.OrderShipmentInfo",
    "HasItemsToDispatch": false,
    "HasItemsToDeliver": false,
    "CanAddItemsToShipment": true
}
```
{% endcode %}
