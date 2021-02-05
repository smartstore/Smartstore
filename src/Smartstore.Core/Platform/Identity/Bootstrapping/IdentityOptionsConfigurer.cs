using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class IdentityOptionsConfigurer : IConfigureOptions<IdentityOptions>
    {
        public IdentityOptionsConfigurer()
        {
        }

        public void Configure(IdentityOptions options)
        {
            // TODO: (core) Read and apply IdentityOptions from settings.
            // TODO: (core) Update IdentityOptions whenever settings change.
        }
    }
}
