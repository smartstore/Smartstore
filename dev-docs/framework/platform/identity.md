# Identity

## Overview

Smartstore builds on ASP.NET Core Identity to manage user accounts, roles and authentication. The platform models users with the [`Customer`](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Identity/Domain/Customer.cs) entity and groups permissions through [`CustomerRole`](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Identity/Domain/CustomerRole.cs) records. Custom implementations of `SignInManager`, `UserStore` and `PasswordHasher` adapt the framework to Smartstore's business rules.

## Customers and roles

- **Customer**: primary user record storing credentials, profile data and activity flags.
- **CustomerRole**: represents membership groups and the permission set assigned to customers.
- **ExternalAuthenticationRecord** persists links between a customer and third-party identities.
- Background tasks like [`DeleteGuestsTask`](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Identity/Tasks/DeleteGuestsTask.cs) keep the identity store tidy.

### Getting the current customer

The current customer can be retrieved through [`IWorkContext`](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/IWorkContext.cs):

```csharp
public class MyService
{
    private readonly IWorkContext _workContext;

    public MyService(IWorkContext workContext)
    {
        _workContext = workContext;
    }

    public Customer GetCurrentCustomer()
        => _workContext.CurrentCustomer
}
```


## Authentication

[`SmartSignInManager`](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Identity/Services/SmartSignInManager.cs) extends ASP.NET Core's `SignInManager` to support login by email or username, generate authentication cookies and enforce lockout.

```csharp
var result = await _signInManager.PasswordSignInAsync(userNameOrEmail, password, isPersistent: false, lockoutOnFailure: true);
if (result.Succeeded)
{
    // user logged in
}
```

`UserManager<Customer>` exposes helpers for password reset, email confirmation and role assignment.

## External authentication

Modules may register external login providers by implementing [`IExternalAuthenticationMethod`](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Identity/Services/IExternalAuthenticationMethod.cs). Each provider registers its configuration and callback routes in its `Startup` class. Smartstore ships modules for popular platforms like Facebook and Google, and custom modules can add additional providers in the same way.