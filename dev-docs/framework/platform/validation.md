# ✔️ Validation

## FluentValidation

Smartstore uses [FluentValidation](https://fluentvalidation.net/) to create strongly-typed validation rules to validate view models on the server-side. This is done by defining rules for properties of the associated view model. When the value of a property is changed via an edit page, it is validated against its rule and, if necessary, an error message is issued if the rule is not fulfilled. Typically, the validation class is located directly below the view model in the same file as the model.

The class of a validator inherits from `AbstractValidator`, to which the view model type is passed as a generic parameter.

```csharp
public partial class CustomerValidator : AbstractValidator<CustomerModel>
{
    public CustomerValidator(CustomerSettings customerSettings)
    {
        RuleFor(x => x.Password).NotEmpty().When(x => x.Id == 0);

        if (customerSettings.FirstNameRequired)
            RuleFor(x => x.FirstName).NotEmpty();

        if (customerSettings.LastNameRequired)
            RuleFor(x => x.LastName).NotEmpty();

        if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
            RuleFor(x => x.Company).NotEmpty();

        if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
            RuleFor(x => x.Phone).NotEmpty();

        // Further code has been omitted for clarity.
    }
}
```

`FluentMigrator` provides a number of ways to specify complex validation rules.

```csharp
public AddressValidator(Localizer T, AddressSettings addressSettings)
{
    // Validate email address.
    RuleFor(x => x.Email).EmailAddress();

    if (addressSettings.ValidateEmailAddress)
    {
        // Validate additional input field to confirm the entered e-mail address.
        RuleFor(x => x.EmailMatch)
            .NotEmpty()
            .EmailAddress()
            .Equal(x => x.Email)
            .WithMessage(T("Admin.Address.Fields.EmailMatch.MustMatchEmail"));
    }
}
```

```csharp
public ConfigurationValidator(Localizer T)
{
    // Validate a list of entered IP addresses.
    RuleFor(x => x.WebhookIps)
        .Must(ips =>
        {
            if (ips != null)
            {
                foreach (var ip in ips)
                {
                    if (!IPAddress.TryParse(ip, out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        })
        .WithMessage(T("Plugins.SmartStore.PostFinance.InvalidIp"));
}
```

### SmartValidator

Typically, the properties of view models have the same names as the corresponding object that is to be modified (e.g. an entity such as a category or a settings class). This way, the values of the properties can be copied from an object to its model and vice versa with a single `MiniMapper` statement. This allows Smartstore to provide additional utilities.

`SmartValidator.ApplyEntityRules` copies common validation rules from the entity type over to the corresponding view model type. Common rules are `Required` and `MaxLength` rules on string properties (either fluently mapped or annotated). It also adds the `Required` rule to non-nullable intrinsic model property types to bypass MVC's non-localized `RequiredAttributeAdapter`.

```csharp
public partial class CategoryValidator : SmartValidator<CategoryModel>
{
    public CategoryValidator(SmartDbContext db)
    {
        ApplyEntityRules<Category>(db);
    }
}
```

{% hint style="info" %}
Validation errors and support requests can be avoided by automatically trimming certain data before saving it. Especially when it comes to data that never starts or ends with a space, such as data for an API access, a bank account or passwords:
{% endhint %}

```csharp
model.ApiPassword = model.ApiPassword.TrimSafe();
model.ApiSecretWord = model.ApiSecretWord.TrimSafe();
MiniMapper.Map(model, settings);
```

### SettingModelValidator

The `SettingModelValidator` is an abstract validator that can ignore rules for unchecked setting properties in a store-specific edit session.

```csharp
public class SearchSettingValidator : SettingModelValidator<SearchSettingsModel, SearchSettings>
{
    public SearchSettingValidator(Localizer T)
    {
        RuleFor(x => x.InstantSearchNumberOfProducts)
            .Must(x => x >= 1 && x <= 16)
            .WhenSettingOverriden((m, x) => m.InstantSearchEnabled)
            .WithMessage(T("Admin.Validation.ValueRange", 1, 16));
    }
}
```

The rule validates `InstantSearchNumberOfProducts` with a value between 1 and 16 under one of the following conditions:

* In case of store-agnostic setting mode: the result of the predicate parameter returns `true`
* In case of store-specific setting mode: the validated `InstantSearchNumberOfProducts` setting is overridden for the current store.

{% hint style="info" %}
`SettingModelValidator` does not support manual validation using `IValidator` directly.
{% endhint %}

### Manual validation

Use `IValidator` to manually validate a view model.

```csharp
[SystemName("Payments.IPaymentCreditCard")]
[FriendlyName("ipayment Credit Card")]
public class CreditCardProvider : IonosProviderBase<IonosPaymentCreditCardSettings>, IConfigurable
{
    private readonly IValidator<CCPaymentInfoModel> _validator;
    
    public override async Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
    {
        var model = new CCPaymentInfoModel
        {
            CardholderName = form["CardholderName"],
            CardNumber = form["CardNumber"],
            CardCode = form["CardCode"]
        };

        var result = await _validator.ValidateAsync(model);
        return new PaymentValidationResult(result);
    }
}
```

## MVC model validation <a href="#model-validation-in-aspnet-core-mvc-and-razor-pages" id="model-validation-in-aspnet-core-mvc-and-razor-pages"></a>

Since Smartstore is based on ASP.NET Core MVC, the server-side and client-side validations of this framework are also available.

### Server-side validation

The model state represents errors that come from the MVC model binding or from model validation. If the model state is not valid (i.e. it contains errors), no data should be saved and instead the edit page should be reloaded with the model state errors.

```csharp
[AuthorizeAdmin, Permission(DevToolsPermissions.Read), LoadSetting]
public IActionResult Configure(ProfilerSettings settings)
{
    var model = MiniMapper.Map<ProfilerSettings, ConfigurationModel>(settings);
    return View(model);
}

[HttpPost, AuthorizeAdmin, Permission(DevToolsPermissions.Update), SaveSetting]
public IActionResult Configure(ConfigurationModel model, ProfilerSettings settings)
{
    if (!ModelState.IsValid)
    {
        return Configure(settings);
    }

    ModelState.Clear();
    MiniMapper.Map(model, settings);

    return RedirectToAction(nameof(Configure));
}
```

{% hint style="info" %}
In the case of an invalid model state, the `GET` Configure method must be called directly without redirecting, otherwise the validation errors will be lost.
{% endhint %}

Data annotation attributes let you specify validation rules for model properties. The most common built-in validation attributes are:

<table><thead><tr><th width="252"> Attribute</th><th>Description</th></tr></thead><tbody><tr><td>Compare</td><td>Validates that two properties in a model match.</td></tr><tr><td>EmailAddress</td><td>Validates that the property has an email format.</td></tr><tr><td>Range</td><td>Validates that the property value falls within a specified range.</td></tr><tr><td>RegularExpression</td><td>Validates that the property value matches a specified regular expression.</td></tr><tr><td>Required</td><td>Validates that the field is not null, is not an empty string and does not only contain white-space characters.</td></tr><tr><td>StringLength</td><td>Validates that a string property value does not exceed a specified length limit.</td></tr><tr><td>Url</td><td>Validates that the property has a URL format.</td></tr><tr><td>ValidateNever</td><td>Indicates that a property or parameter should be excluded from validation.</td></tr></tbody></table>

Custom validation can be added to action methods.

```csharp
if (model.Email.IsEmpty())
{
    ModelState.AddModelError(nameof(model.Email),
        T("Account.Register.Errors.EmailIsNotProvided"));
}
```

The key (first parameter) of `AddModelError` can also be `string.Empty`. In this case, the error message will be displayed in the error summary and not directly at the related input field.

The validation summary tag helper targets the HTML `div` element inside your razor view, and is used to render a summary of form validation error messages.

```cshtml
<div asp-validation-summary="All"></div>
```

Possible `ValidationSummary` enumeration values are:

<table><thead><tr><th width="185">Value</th><th>Summarize</th></tr></thead><tbody><tr><td>None</td><td>Nothing.</td></tr><tr><td>ModelOnly</td><td>Model-level errors only (excludes all property errors).</td></tr><tr><td>All</td><td>Model and property validation errors.</td></tr></tbody></table>

{% hint style="info" %}
For forms placed in tabs, `ValidationSummary.All` should be used, otherwise the user may miss the error message.
{% endhint %}

The validation tag helper targets the HTML `span` element inside your razor view, and is used to render property-specific validation error messages.

```cshtml
<span asp-validation-for="ParentCategoryId"></span>
```

### Client-side validation

Client-side validation prevents an HTML form from being submitted until it is valid. The Submit button runs JavaScript that either submits the form or displays error messages. This avoids unnecessary round-trips to the server when there are input errors on a form. The validation messages correspond to the validation attributes specified for the model property. For instance, data type validation is based on the .NET type of a property, unless that is overridden by a `DataType` attribute.

{% hint style="info" %}
Client-side and server-side validation may differ. Whitespace in a string field is considered valid input in client-side validation. However, server-side validation considers a required string field invalid if only white-space characters are entered.
{% endhint %}
