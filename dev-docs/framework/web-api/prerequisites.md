# ✔ Prerequisites

The **Smartstore Web API** requires configuration by the store owner in order to start working. First, the store owner needs to install the Web API module in the Smartstore backend. The module technology gives him the ability to enable or disable the entire Web API at any time without affecting the online store.

The next step is to configure the API on the configuration page of the module. The main thing is to give individual members access to the API and the data of the online store. The store owner can create a public and a secret key for each registered member. Only a registered member with both keys can access the API. To exclude a member from the API, the store owner can either delete the member's keys (permanent exclusion) or disable the keys (temporary exclusion). A member's roles and permissions are taken into account when accessing data via the API.

The secret key should only be known to the store owner and the member accessing the Web API.

{% hint style="success" %}
To develop a custom Web API consumer, implementers should be familiar with REST API consumer implementation, particularly the OData provider.
{% endhint %}
