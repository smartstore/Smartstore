# 🐣 Cookie Consent

In e-commerce, cookies are critical for tracking user sessions, maintaining shopping carts, and personalizing the user experience. They enable persistent state management across pages and sessions, enabling features such as user authentication, preference storage, and targeted marketing. Developers must handle cookies securely, implementing consent controls, proper encryption, and adhering to privacy regulations such as GDPR to protect user data and ensure compliance.

As developers we can access cookie information using `Smartstore.Core.Identity.ICookieConsentManager` and provide our own information using `Smartstore.Core.Identity.ICookiePublisher`.

`CookieType` categorizes cookies into six types:

| Cookie type                | Use case                                               |
| -------------------------- | ------------------------------------------------------ |
| `None`                     | No cookies                                             |
| `Required`                 | Essential for core functionalities like authentication |
| `Analytics`                | Used for tracking and performance metrics              |
| `ThirdParty`               | Cookies set by external services                       |
| `ConsentAdUserData`        | Tracks consented user data for ads                     |
| `ConsentAdPersonalization` | Enables personalized advertising                       |

## Provide consent information

In order for your module to provide cookie information, it must implement the `ICookiePublisher` interface. This allows Smartstore to retrieve this information using `CookieInfo`. It provides a `Name`, `Description`, `SelectedStoreIds` and `CookieType` for each.

This information is displayed in the frontend via the cookie manager so that the user can personalize their consent.

## Retrieve consent information

To find out if consent has been given for a particular type of cookie, use the Cookie Consent Manager's `IsCookieAllowedAsync` method.

{% code overflow="wrap" %}
```csharp
var hasUserConsent = await _cookieConsentManager.IsCookieAllowedAsync(CookieType.ThirdParty);
```
{% endcode %}

There are several ways to add scripts that rely on cookie consent.

### Load scripts after approval

Prior to version 6.0.0, these scripts could only be loaded after a page refresh if the user agreed.

#### Razor

We can use the `sm-consent-type` tag helper to load a script directly after consent is given.

```razor
<script sm-consent-type="Analytics"></script>
```

#### C\#

When a script tag is injected using C#, we need to simulate the work of the tag helper and generate the HTML string with the `data-consent` (lower case) and `data-src` attributes set. The `data-src` attribute is automatically renamed to `src` when consent is given.

```csharp
// Verify that consent has been given.
var consented = await _cookieConsentManager.IsCookieAllowedAsync(CookieType.Required);

// Add the attribute accordingly.
var scriptIncludeTag = new HtmlString($"<script id=\"payment-method-js\" {(consented ? string.Empty : "data-consent=\"required\" data-")}src=\https://js.payment-method.com/v3/\ async></script>");

// Include the script in the head section.
_widgetProvider.RegisterHtml("head", scriptIncludeTag);
```

#### Code block

When injecting using a code block, a `<template>` tag must be used in conjunction with the `data-consent` attribute, the value of which must be in lower case.

```razor
<template data-consent="thirdparty">
    // Code block that needs consent.
</template>
```

#### JavaScript

When using JS to check for consent, you can use the following snippet:

```js
// Use the lower case value of CookieType.
let hasConsent = Smartstore.Cmp.checkConsent('analytics');
```

If a script is to be processed only after consent, add the `type` attribute with `text/plain` in conjunction with the `data-consent` attribute, the value of which must be in lower case.

```html
<script type="text/plain" data-consent="thirdparty">
   // Code that is only executed after consent is given.
</script>
```
