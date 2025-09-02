using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Smartstore.Apple.Auth;
using Smartstore.Engine;

namespace Smartstore.Apple
{
    internal sealed class AppleOptionsConfigurer : IConfigureOptions<AuthenticationOptions>, IConfigureNamedOptions<AppleAuthenticationOptions>
    {
        private string _precomputedSecret;
        private readonly IApplicationContext _appContext;

        public AppleOptionsConfigurer(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void Configure(AuthenticationOptions options)
        {
            options.AddScheme(AppleAuthenticationDefaults.AuthenticationScheme, builder =>
            {
                builder.DisplayName = "Apple";
                builder.HandlerType = typeof(AppleAuthenticationHandler);
            });
        }

        public void Configure(string name, AppleAuthenticationOptions options)
        {
            if (name.HasValue() && !string.Equals(name, AppleAuthenticationDefaults.AuthenticationScheme))
            {
                return;
            }

            var settings = _appContext.Services.Resolve<AppleExternalAuthSettings>();
            if (!settings.ClientId.HasValue() || !settings.KeyId.HasValue() || !settings.TeamId.HasValue() || !settings.PrivateKey.HasValue())
            {
                // Not configured.
                return; 
            }

            // INFO: If generation of the client secret fails (maybe because of wrong configuration data)
            // we get out of the Configure method before setting up options so they are not in a volatile state.
            try
            {
                _precomputedSecret = CreateAppleClientSecret(settings, daysValid: 30);

                // Important to supress the default client secret generation.
                options.GenerateClientSecret = false;
                // Instead we generate it ourselves :-)
                options.ClientSecret = _precomputedSecret;

                options.ClientId = settings.ClientId;
                options.KeyId = settings.KeyId;
                options.TeamId = settings.TeamId;

                // INFO: This was the proposed way by the library devs. But it's even commented out in their sample code. 
                // So I guess they couldn't get it to work either. 
                //options.PrivateKey = (keyId, cancellationToken) => Task.FromResult(settings.PrivateKey.AsMemory());

                // 15 minutes should be enough for the user to enter his Apple creds.
                options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(15);

                options.ValidateTokens = false;
                options.SecurityTokenHandler ??= new JsonWebTokenHandler();

                options.Events.OnRemoteFailure ??= context =>
                {
                    var errorUrl = context.Request.PathBase.Value + $"/identity/externalerrorcallback?provider=apple&errorMessage={context.Failure.Message}";
                    context.Response.Redirect(errorUrl);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            }
            catch (Exception ex)
            {
                _appContext.Logger.LogError(ex, "Failed to create Apple client secret.");
            }
        }

        public void Configure(AppleAuthenticationOptions options)
            => Debug.Fail("This infrastructure method shouldn't be called.");

        private static string CreateAppleClientSecret(AppleExternalAuthSettings settings, int daysValid = 30)
        {
            // Normalize PEM: turn literal \n into real newlines, strip CR, trim
            var pem = settings.PrivateKey.Replace("\r", "").Replace("\\n", "\n").Trim();
            var base64 = ExtractPkcs8Base64(pem);
            var keyBytes = Convert.FromBase64String(base64);

            using var ecdsaImport = ECDsa.Create();
            ecdsaImport.ImportPkcs8PrivateKey(keyBytes, out _);
            var parameters = ecdsaImport.ExportParameters(includePrivateParameters: true);
            using var ecdsa = ECDsa.Create(parameters);

            var credentials = new SigningCredentials(new ECDsaSecurityKey(ecdsa) { KeyId = settings.KeyId }, SecurityAlgorithms.EcdsaSha256);

            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                issuer: settings.TeamId,                          
                audience: "https://appleid.apple.com",   
                claims: [new Claim("sub", settings.ClientId)],
                notBefore: now,
                expires: now.AddDays(daysValid),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Extracts the Base64 payload from a PKCS#8 PEM block
        /// </summary>
        /// <param name="pem"></param>
        /// <returns></returns>
        private static string ExtractPkcs8Base64(string pem)
        {
            const string header = "-----BEGIN PRIVATE KEY-----";
            const string footer = "-----END PRIVATE KEY-----";

            if (pem.Contains(header) && pem.Contains(footer))
            {
                var body = pem.Replace(header, string.Empty).Replace(footer, string.Empty);
                return new string([.. body.Where(c => !char.IsWhiteSpace(c))]);
            }

            // Fallback: treat input as raw base64
            return new string([.. pem.Where(c => !char.IsWhiteSpace(c))]);
        }
    }
}

