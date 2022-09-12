namespace Smartstore.WebApi.Services
{
    public enum AccessDeniedReason
    {
        InvalidAuthorizationHeader,
        InvalidCredentials,
        UserUnknown,
        UserInactive,
        UserDisabled,
        UserHasNoPermission
    }

    public class WebApiAuthorizationException : UnauthorizedAccessException
    {
        public WebApiAuthorizationException(AccessDeniedReason deniedReason, string publicKey = null, string permission = null)
            : this(CreateMessage(deniedReason, publicKey, permission), deniedReason)
        {
        }

        public WebApiAuthorizationException(string message, AccessDeniedReason deniedReason)
            : base(message)
        {
            DeniedReason = deniedReason;
        }

        public AccessDeniedReason DeniedReason { get; }

        private static string CreateMessage(AccessDeniedReason deniedReason, string publicKey, string permission)
        {
            string reason = null;

            switch (deniedReason)
            {
                case AccessDeniedReason.InvalidAuthorizationHeader:
                    reason = "Missing or invalid authorization header. Must have the format 'PublicKey:SecretKey'.";
                    break;
                case AccessDeniedReason.InvalidCredentials:
                    reason = $"The credentials for user '{publicKey.NaIfEmpty()}' do not match.";
                    break;
                case AccessDeniedReason.UserUnknown:
                    reason = $"The user '{publicKey.NaIfEmpty()}' is unknown.";
                    break;
                case AccessDeniedReason.UserInactive:
                    reason = $"The user '{publicKey.NaIfEmpty()}' is not active.";
                    break;
                case AccessDeniedReason.UserDisabled:
                    reason = $"The user '{publicKey.NaIfEmpty()}' is disabled for the Web API.";
                    break;
                case AccessDeniedReason.UserHasNoPermission:
                    reason = $"The user '{publicKey.NaIfEmpty()}' has no authorization. Permission: {permission.NaIfEmpty()}.";
                    break;
            }

            return $"Access to the API was denied. Reason: {deniedReason}. {reason.NaIfEmpty()}";
        }
    }
}
