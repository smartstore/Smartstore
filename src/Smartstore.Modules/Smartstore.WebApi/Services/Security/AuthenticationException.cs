using Microsoft.AspNetCore.Diagnostics;

namespace Smartstore.WebApi.Services
{
    internal class AuthenticationException : UnauthorizedAccessException, IExceptionHandlerPathFeature
    {
        public AuthenticationException(AccessDeniedReason deniedReason, string publicKey = null)
            : this(CreateMessage(deniedReason, publicKey), deniedReason)
        {
        }

        public AuthenticationException(string message, AccessDeniedReason deniedReason)
            : base(message)
        {
            DeniedReason = deniedReason;
        }

        #region IExceptionHandlerPathFeature

        public Exception Error => this;
        public string Path => null;

        #endregion

        public AccessDeniedReason DeniedReason { get; }

        private static string CreateMessage(AccessDeniedReason deniedReason, string publicKey)
        {
            string reason = null;

            switch (deniedReason)
            {
                case AccessDeniedReason.ApiDisabled:
                    reason = "Web API is disabled.";
                    break;
                case AccessDeniedReason.InvalidAuthorizationHeader:
                    reason = "Missing or invalid authorization header. Must have the format 'PublicKey:SecretKey'.";
                    break;
                case AccessDeniedReason.InvalidCredentials:
                    reason = $"The credentials sent for user with public key {publicKey.NaIfEmpty()} do not match.";
                    break;
                case AccessDeniedReason.UserUnknown:
                    reason = $"Unknown user. The public key {publicKey.NaIfEmpty()} does not exist.";
                    break;
                case AccessDeniedReason.UserInactive:
                    reason = $"The user with public key {publicKey.NaIfEmpty()} is not active.";
                    break;
                case AccessDeniedReason.UserDisabled:
                    reason = $"Access via Web API is disabled for the user with public key {publicKey.NaIfEmpty()}.";
                    break;
            }

            return $"Access to the API was denied. Reason: {deniedReason}. {reason.NaIfEmpty()}";
        }
    }
}
