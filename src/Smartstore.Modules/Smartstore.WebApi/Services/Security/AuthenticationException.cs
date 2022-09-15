namespace Smartstore.Web.Api.Security
{
    internal class AuthenticationException : UnauthorizedAccessException
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

        public AccessDeniedReason DeniedReason { get; }

        private static string CreateMessage(AccessDeniedReason deniedReason, string publicKey)
        {
            string reason = null;

            switch (deniedReason)
            {
                case AccessDeniedReason.ApiDisabled:
                    reason = "Web API is disabled.";
                    break;
                case AccessDeniedReason.SslRequired:
                    reason = "Web API requests require HTTPS.";
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

            return $"Access to the Web API was denied. Reason: {deniedReason}. {reason.NaIfEmpty()}";
        }
    }
}
