using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Responsible for resolving the current customer's working language.
    /// </summary>
    public interface ILanguageResolver
    {
        Task<Language> ResolveLanguageAsync(Customer currentCustomer, HttpContext httpContext);
    }
}
