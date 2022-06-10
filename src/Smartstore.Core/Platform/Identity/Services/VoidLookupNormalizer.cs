using Microsoft.AspNetCore.Identity;

namespace Smartstore.Core.Identity
{
    public sealed class VoidLookupNormalizer : ILookupNormalizer
    {
        // Currently we don't support e-mail and username normalization, so we replace the normalizer impl (which makes data uppercase).
        public string NormalizeEmail(string email) => email;
        public string NormalizeName(string name) => name;
    }
}
