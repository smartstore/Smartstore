using System;
using System.Text.Json.Serialization;


namespace Smartstore.Paystack.Models
{
    public class VerifyTransactionResponseModel
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("domain")]
        public string Domain { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("message")]
        public object Message { get; set; }

        [JsonPropertyName("gateway_response")]
        public string GatewayResponse { get; set; }

        [JsonPropertyName("paid_at")]
        public DateTime PaidAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("channel")]
        public string Channel { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("ip_address")]
        public string IpAddress { get; set; }

        [JsonPropertyName("metadata")]
        public string Metadata { get; set; }

        //[JsonPropertyName("log")]
        //public Log Log { get; set; }

        [JsonPropertyName("fees")]
        public decimal Fees { get; set; }

        [JsonPropertyName("fees_split")]
        public object FeesSplit { get; set; }

        //[JsonPropertyName("authorization")]
        //public Authorization Authorization { get; set; }

        //[JsonPropertyName("customer")]
        //public Customer Customer { get; set; }

        [JsonPropertyName("plan")]
        public object Plan { get; set; }

        //[JsonPropertyName("split")]
        //public Split Split { get; set; }

        [JsonPropertyName("order_id")]
        public object OrderId { get; set; }


        [JsonPropertyName("requested_amount")]
        public decimal RequestedAmount { get; set; }

        [JsonPropertyName("pos_transaction_data")]
        public object PosTransactionData { get; set; }

        [JsonPropertyName("source")]
        public object Source { get; set; }

        [JsonPropertyName("transaction_date")]
        public DateTime TransactionDate { get; set; }

        //[JsonPropertyName("plan_object")]
        //public PlanObject PlanObject { get; set; }

        //[JsonPropertyName("subaccount")]
        //public Subaccount Subaccount { get; set; }
    }
}
