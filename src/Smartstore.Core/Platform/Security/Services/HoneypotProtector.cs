using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Smartstore.Utilities;

namespace Smartstore.Core.Security
{
    public class HoneypotField
    {
        public string Name { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }

    public class HoneypotProtector
    {
        public const string TokenFieldName = "__hpToken";

        private static readonly string[] _fieldNames = new[] { "Phone", "Fax", "Email", "Age", "Name", "FirstName", "LastName", "Type", "Custom", "Reason", "Pet", "Question", "Region" };
        private static readonly string _fieldSuffix = CommonHelper.GenerateRandomDigitCode(5);

        private readonly IDataProtector _protector;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HoneypotProtector(IDataProtectionProvider provider, IHttpContextAccessor httpContextAccessor)
        {
            _protector = provider.CreateProtector("HoneypotProtector");
            _httpContextAccessor = httpContextAccessor;
        }

        public HoneypotField CreateToken()
        {
            var r = new Random();

            // Create a rondom field name with pattern "[random1]-[random2][suffix]"
            var len = _fieldNames.Length;
            var fieldName = string.Concat(_fieldNames[r.Next(0, len)], "-", _fieldNames[r.Next(0, len)], _fieldSuffix);

            return new HoneypotField
            {
                Name = fieldName,
                CreatedOnUtc = DateTime.UtcNow
            };
        }

        public string SerializeToken(HoneypotField token)
        {
            Guard.NotNull(token, nameof(token));

            var json = JsonConvert.SerializeObject(token);
            var encoded = _protector.Protect(json.GetBytes());

            var result = Convert.ToBase64String(encoded);
            return result;
        }

        public HoneypotField DeserializeToken(string token)
        {
            Guard.NotEmpty(token, nameof(token));

            var encoded = Convert.FromBase64String(token);
            var decoded = _protector.Unprotect(encoded);
            var json = Encoding.UTF8.GetString(decoded);

            var result = JsonConvert.DeserializeObject<HoneypotField>(json);
            return result;
        }

        public bool IsBot()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null || !request.HasFormContentType)
            {
                return false;
            }

            string tokenString = request.Form[TokenFieldName];
            if (tokenString.IsEmpty())
            {
                throw new InvalidOperationException("The required honeypot form field is missing. Please render the field with the honeypot tag helper.");
            }

            var token = DeserializeToken(tokenString);
            string trap = request.Form[token.Name];
            var isBot = trap == null || trap.Length > 0 || (DateTime.UtcNow - token.CreatedOnUtc).TotalMilliseconds < 2000;

            return isBot;
        }
    }
}
