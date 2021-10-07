using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Modelling;

namespace Smartstore.Forums.Models
{
    [LocalizedDisplay("Admin.Customers.Customers.SendPM.")]
    public partial class SendPmModel : ModelBase
    {
        public int ToCustomerId { get; set; }

        [Required]
        [LocalizedDisplay("*Subject")]
        public string Subject { get; set; }

        [Required]
        [LocalizedDisplay("*Message")]
        public string Message { get; set; }
    }
}
