namespace Smartstore.Core.Security
{
    public static partial class Permissions
    {
        public static partial class Configuration
        {
            public static class Country
            {
                public const string Self = "configuration.country";
                public const string Read = "configuration.country.read";
                public const string Update = "configuration.country.update";
                public const string Create = "configuration.country.create";
                public const string Delete = "configuration.country.delete";
            }

            public static class Currency
            {
                public const string Self = "configuration.currency";
                public const string Read = "configuration.currency.read";
                public const string Update = "configuration.currency.update";
                public const string Create = "configuration.currency.create";
                public const string Delete = "configuration.currency.delete";
                public const string EditExchangeRate = "configuration.currency.editexchangerate";
            }

            public static class DeliveryTime
            {
                public const string Self = "configuration.deliverytime";
                public const string Read = "configuration.deliverytime.read";
                public const string Update = "configuration.deliverytime.update";
                public const string Create = "configuration.deliverytime.create";
                public const string Delete = "configuration.deliverytime.delete";
            }

            public static class Measure
            {
                public const string Self = "configuration.measure";
                public const string Read = "configuration.measure.read";
                public const string Update = "configuration.measure.update";
                public const string Create = "configuration.measure.create";
                public const string Delete = "configuration.measure.delete";
            }

            public static class PriceLabel
            {
                public const string Self = "configuration.pricelabel";
                public const string Read = "configuration.pricelabel.read";
                public const string Update = "configuration.pricelabel.update";
                public const string Create = "configuration.pricelabel.create";
                public const string Delete = "configuration.pricelabel.delete";
            }
        }
    }
}
