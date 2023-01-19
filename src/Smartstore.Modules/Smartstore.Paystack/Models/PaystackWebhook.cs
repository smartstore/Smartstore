using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Smartstore.Paystack.Models
{
    public class PaystackWebhook
    {
        public string Event { get; set; }
        public Data Data { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    public class History
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public int Time { get; set; }
    }

    public class Log
    {
        public int TimeSpent { get; set; }
        public int Attempts { get; set; }
        public string Authentication { get; set; }
        public int Errors { get; set; }
        public bool Success { get; set; }
        public bool Mobile { get; set; }
        public List<object> Input { get; set; }
        public string Channel { get; set; }
        public List<History> History { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string CustomerCode { get; set; }
        public string Phone { get; set; }
        public object Metadata { get; set; }
        public string RiskAction { get; set; }
    }

    public class Authorization
    {
        public string AuthorizationCode { get; set; }
        public string Bin { get; set; }
        public string Last4 { get; set; }
        public string ExpMonth { get; set; }
        public string ExpYear { get; set; }
        public string CardType { get; set; }
        public string Bank { get; set; }
        public string CountryCode { get; set; }
        public string Brand { get; set; }
        public string AccountName { get; set; }
    }

    public class Plan
    {
    }

    public class Data
    {
        public int Id { get; set; }
        public string Domain { get; set; }
        public string Status { get; set; }
        public string Reference { get; set; }
        public int Amount { get; set; }
        public string Message { get; set; }
        public string GatewayResponse { get; set; }
        public DateTime PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Channel { get; set; }
        public string Currency { get; set; }
        public string IpAddress { get; set; }
        public int Metadata { get; set; }
        public Log Log { get; set; }
        public object Fees { get; set; }
        public Customer Customer { get; set; }
        public Authorization Authorization { get; set; }
        public Plan Plan { get; set; }
    }

}
