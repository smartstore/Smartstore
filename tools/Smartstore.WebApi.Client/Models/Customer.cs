namespace Smartstore.WebApi.Client.Models
{
    public class Customer
    {
        public string Id { get; set; }
        public string CustomerGuid { get; set; }
        public string Email { get; set; }

        public override string ToString()
            => $"Id: {Id}, Guid: {CustomerGuid}, Email: {Email}";
    }
}
