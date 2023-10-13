using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Newtonsoft.Json;
using Sys = System;

namespace Smartstore.Core.Common
{
    [JsonConverter(typeof(MoneyJsonConverter))]
    public readonly struct Money : IHtmlContent, IConvertible, IFormattable, IComparable, IComparable<Money>, IEquatable<Money>
    {
        public readonly static Money Zero;

        // Key: string = Currency.DisplayLocale, bool = useIsoCodeAsSymbol, int = DecimalDigits
        private readonly static ConcurrentDictionary<(string, bool, int), NumberFormatInfo> _numberFormatClones = new();

        public Money(Currency currency)
            : this(0m, currency)
        {
        }

        public Money(decimal amount, Currency currency)
            : this(amount, currency, false)
        {
        }

        public Money(decimal amount, Currency currency, bool hideCurrency, string postFormat = null)
        {
            Guard.NotNull(currency);

            Amount = amount;
            Currency = currency;
            HideCurrency = hideCurrency;
            PostFormat = postFormat;
        }

        [IgnoreDataMember]
        public bool HideCurrency
        {
            get;
            init;
        }

        [IgnoreDataMember]
        public Currency Currency
        {
            get;
            init;
        }

        /// <summary>
        /// Gets the number of decimal digits for the associated currency.
        /// </summary>
        public int DecimalDigits
        {
            get => Currency?.RoundNumDecimals ?? 2;
        }

        /// <summary>
        /// The internal unrounded raw amount
        /// </summary>
        public decimal Amount
        {
            get;
        }

        /// <summary>
        /// Rounds the amount using <see cref="Currency.RoundNumDecimals"/> and <see cref="Currency.MidpointRounding"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="RoundedAmount"/> is for display only. If an amount is to be rounded according to all currency settings and properties,
        /// the amount rounded by IRoundingHelper must be passed to <see cref="Money"/>! In this case <see cref="Amount"/> and <see cref="RoundedAmount"/> are identical.
        /// </remarks>
        internal decimal RoundedAmount
        {
            get => decimal.Round(Amount, DecimalDigits, 
                Currency == null || Currency.MidpointRounding == CurrencyMidpointRounding.AwayFromZero ? MidpointRounding.AwayFromZero : MidpointRounding.ToEven);
        }

        /// <summary>
        /// Truncates the amount to the number of significant decimal digits
        /// of the associated currency.
        /// </summary>
        public decimal TruncatedAmount
        {
            get => (decimal)((long)Math.Truncate(Amount * DecimalDigits)) / DecimalDigits;
        }

        /// <summary>
        /// The format string to apply AFTER amount has been formatted (amount + currency symbol), 
        /// e.g. "{0} incl. tax" (where {0} is replaced by currency formatted amount).
        /// </summary>
        public string PostFormat
        {
            get;
            init;
        }

        private static void GuardCurrencyEquality(Money a, Money b)
        {
            if (a.Currency != b.Currency)
            {
                throw new InvalidOperationException("Cannot operate on money values with different currencies.");
            }
        }

        #region Change & Assign

        /// <summary>
        /// Changes the underlying amount.
        /// </summary>
        /// <param name="amount">The new amount.</param>
        /// <param name="currency">New optional currency.</param>
        public Money WithAmount(float amount, Currency currency = null)
            => WithAmount((decimal)amount, currency);

        /// <summary>
        /// Changes the underlying amount.
        /// </summary>
        /// <param name="amount">The new amount.</param>
        /// <param name="currency">New optional currency.</param>
        public Money WithAmount(double amount, Currency currency = null)
            => WithAmount((decimal)amount, currency);


        /// <summary>
        /// Changes the underlying amount.
        /// </summary>
        /// <param name="amount">The new amount.</param>
        /// <param name="currency">New optional currency.</param>
        public Money WithAmount(decimal amount, Currency currency = null)
            => new(amount, currency ?? Currency, HideCurrency, PostFormat);

        /// <summary>
        /// Changes the underlying currency.
        /// </summary>
        /// <param name="currency">The currency to switch to.</param>
        public Money WithCurrency(Currency currency)
            => new(Amount, currency, HideCurrency, PostFormat);

        /// <summary>
        /// Applies a second format string AFTER amount has been formatted (amount + currency symbol), 
        /// e.g. "{0} incl. tax" (where {0} is replaced by currency formatted amount).
        /// </summary>
        /// <param name="format">The post format string.</param>
        public Money WithPostFormat(string format)
            => new(Amount, Currency, HideCurrency, format);

        /// <summary>
        /// Sets a value specifying whether the currency symbol should be displayed during formatting.
        /// </summary>
        /// <param name="showSymbol"><c>true</c> = render symbol, <c>false</c> = hide symbol.</param>
        public Money WithSymbol(bool showSymbol)
            => new(Amount, Currency, !showSymbol, PostFormat);

        #endregion

        #region Compare

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Currency);
        }

        int IComparable.CompareTo(object obj)
        {
            if (obj == null || obj is not Money other)
                return 1;

            return CompareTo(other);
        }

        public int CompareTo(Money other)
        {
            if (Currency == null)
                return 0m.CompareTo(other.Amount);

            if (other.Currency == null)
                return Amount.CompareTo(0m);

            GuardCurrencyEquality(this, other);

            return Amount.CompareTo(other.Amount);
        }

        public override bool Equals(object obj)
        {
            if (obj is Money money)
            {
                return ((IEquatable<Money>)this).Equals(money);
            }

            return false;
        }

        bool IEquatable<Money>.Equals(Money other)
        {
            if (other.Amount == 0 && this.Amount == 0)
                return true;

            return other.Amount == this.Amount && other.Currency == this.Currency;
        }

        public static bool operator ==(Money a, Money b) => a.Equals(b);
        public static bool operator !=(Money a, Money b) => !a.Equals(b);
        public static bool operator >(Money a, Money b) => a.CompareTo(b) > 0;
        public static bool operator <(Money a, Money b) => a.CompareTo(b) < 0;
        public static bool operator <=(Money a, Money b) => a.CompareTo(b) <= 0;
        public static bool operator >=(Money a, Money b) => a.CompareTo(b) >= 0;

        public static bool operator ==(Money a, int b) => a.Amount == b;
        public static bool operator !=(Money a, int b) => a.Amount != b;
        public static bool operator >(Money a, int b) => a.Amount > b;
        public static bool operator <(Money a, int b) => a.Amount < b;
        public static bool operator <=(Money a, int b) => a.Amount <= b;
        public static bool operator >=(Money a, int b) => a.Amount >= b;

        public static bool operator ==(Money a, float b) => a.Amount == (decimal)b;
        public static bool operator !=(Money a, float b) => a.Amount != (decimal)b;
        public static bool operator >(Money a, float b) => a.Amount > (decimal)b;
        public static bool operator <(Money a, float b) => a.Amount < (decimal)b;
        public static bool operator <=(Money a, float b) => a.Amount <= (decimal)b;
        public static bool operator >=(Money a, float b) => a.Amount >= (decimal)b;

        public static bool operator ==(Money a, double b) => a.Amount == (decimal)b;
        public static bool operator !=(Money a, double b) => a.Amount != (decimal)b;
        public static bool operator >(Money a, double b) => a.Amount > (decimal)b;
        public static bool operator <(Money a, double b) => a.Amount < (decimal)b;
        public static bool operator <=(Money a, double b) => a.Amount <= (decimal)b;
        public static bool operator >=(Money a, double b) => a.Amount >= (decimal)b;

        public static bool operator ==(Money a, decimal b) => a.Amount == b;
        public static bool operator !=(Money a, decimal b) => a.Amount != b;
        public static bool operator >(Money a, decimal b) => a.Amount > b;
        public static bool operator <(Money a, decimal b) => a.Amount < b;
        public static bool operator <=(Money a, decimal b) => a.Amount <= b;
        public static bool operator >=(Money a, decimal b) => a.Amount >= b;

        #endregion

        #region Format

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IHtmlContent.WriteTo(TextWriter writer, HtmlEncoder encoder)
            => writer.Write(ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        string IFormattable.ToString(string format, IFormatProvider formatProvider)
            => ToString(!HideCurrency, false, PostFormat);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        string IConvertible.ToString(IFormatProvider provider)
            => ToString(!HideCurrency, false, PostFormat);

        /// <summary>
        /// Creates the string representation of the rounded amount.
        /// </summary>
        /// <returns>The formatted rounded amount.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => ToString(!HideCurrency, false, PostFormat);

        /// <summary>
        /// Creates the string representation of the rounded amount.
        /// </summary>
        /// <param name="showCurrency">Whether to render currency symbol. If <c>null</c>, falls back to negated <see cref="HideCurrency"/>.</param>
        /// <param name="useISOCodeAsSymbol">Whether to render currency symbol as ISO code. Only relevant if currency symbol should be rendered.</param>
        /// <param name="postFormat">Applies a second format string AFTER amount has been formatted (amount + currency symbol), e.g. "{0} incl. tax" (where {0} is replaced by currency formatted amount).</param>
        /// <returns>The formatted rounded amount.</returns>
        public string ToString(bool? showCurrency = null, bool useISOCodeAsSymbol = false, string postFormat = null)
        {
            if (Currency == null)
            {
                return postFormat == null 
                    ? RoundedAmount.ToStringInvariant()
                    : string.Format(postFormat, RoundedAmount.ToStringInvariant());
            }

            postFormat ??= PostFormat;

            var nf = Currency.NumberFormat;
            string formatted = null;

            if (!string.IsNullOrEmpty(Currency.CustomFormatting))
            {
                formatted = RoundedAmount.ToString(Currency.CustomFormatting, nf);
            }
            else
            {
                showCurrency ??= !HideCurrency;

                if (showCurrency == false || useISOCodeAsSymbol || DecimalDigits != nf.CurrencyDecimalDigits)
                {
                    var currencyCode = Currency.CurrencyCode;
                    var decimalDigits = DecimalDigits;

                    nf = _numberFormatClones.GetOrAdd((Currency.DisplayLocale, useISOCodeAsSymbol, decimalDigits), key =>
                    {
                        var clone = (NumberFormatInfo)nf.Clone();
                        clone.CurrencySymbol = showCurrency == false ? string.Empty : (useISOCodeAsSymbol ? currencyCode : nf.CurrencySymbol);
                        clone.CurrencyDecimalDigits = decimalDigits;
                        return clone;
                    });
                }

                formatted = RoundedAmount.ToString("C", nf);
            }

            return postFormat == null || postFormat == "{0}" ? formatted : string.Format(postFormat, formatted);
        }

        #endregion

        #region Convert

        public static explicit operator bool(Money money) => money.Amount != 0; // For truthy checks in templating
        public static explicit operator string(Money money) => money.ToString(true, false);
        public static explicit operator byte(Money money) => Convert.ToByte(money.RoundedAmount);
        public static explicit operator decimal(Money money) => money.RoundedAmount;
        public static explicit operator double(Money money) => Convert.ToDouble(money.RoundedAmount);
        public static explicit operator float(Money money) => Convert.ToSingle(money.RoundedAmount);
        public static explicit operator int(Money money) => Convert.ToInt32(money.RoundedAmount);
        public static explicit operator long(Money money) => Convert.ToInt64(money.RoundedAmount);
        public static explicit operator sbyte(Money money) => Convert.ToSByte(money.RoundedAmount);
        public static explicit operator short(Money money) => Convert.ToInt16(money.RoundedAmount);
        public static explicit operator ushort(Money money) => Convert.ToUInt16(money.RoundedAmount);
        public static explicit operator uint(Money money) => Convert.ToUInt32(money.RoundedAmount);
        public static explicit operator ulong(Money money) => Convert.ToUInt64(money.RoundedAmount);

        TypeCode IConvertible.GetTypeCode() => TypeCode.Decimal;
        object IConvertible.ToType(Type conversionType, IFormatProvider provider) => Sys.Convert.ChangeType(this.RoundedAmount, conversionType, provider);
        bool IConvertible.ToBoolean(IFormatProvider provider) => Amount != 0;
        char IConvertible.ToChar(IFormatProvider provider) => throw Error.InvalidCast(typeof(Money), typeof(char));
        DateTime IConvertible.ToDateTime(IFormatProvider provider) => throw Error.InvalidCast(typeof(Money), typeof(DateTime));
        byte IConvertible.ToByte(IFormatProvider provider) => (byte)RoundedAmount;
        decimal IConvertible.ToDecimal(IFormatProvider provider) => RoundedAmount;
        double IConvertible.ToDouble(IFormatProvider provider) => (double)RoundedAmount;
        short IConvertible.ToInt16(IFormatProvider provider) => (short)RoundedAmount;
        int IConvertible.ToInt32(IFormatProvider provider) => (int)RoundedAmount;
        long IConvertible.ToInt64(IFormatProvider provider) => (long)RoundedAmount;
        sbyte IConvertible.ToSByte(IFormatProvider provider) => (sbyte)RoundedAmount;
        float IConvertible.ToSingle(IFormatProvider provider) => (float)RoundedAmount;
        ushort IConvertible.ToUInt16(IFormatProvider provider) => (ushort)RoundedAmount;
        uint IConvertible.ToUInt32(IFormatProvider provider) => (uint)RoundedAmount;
        ulong IConvertible.ToUInt64(IFormatProvider provider) => (ulong)RoundedAmount;

        #endregion

        #region Add

        public static Money operator ++(Money a)
        {
            var amount = a.Amount;
            return a.WithAmount(amount++);
        }

        public static Money operator +(Money a, Money b)
        {
            if (a.Currency == null)
                return b;

            if (b.Currency == null)
                return a;

            GuardCurrencyEquality(a, b);
            return a.WithAmount(a.Amount + b.Amount);
        }

        public static Money operator +(Money a, int b) => a + (decimal)b;
        public static Money operator +(Money a, float b) => a + (decimal)b;
        public static Money operator +(Money a, double b) => a + (decimal)b;
        public static Money operator +(Money a, decimal b) => a.WithAmount(a.Amount + b);

        #endregion

        #region Substract

        public static Money operator --(Money a)
        {
            var amount = a.Amount;
            return a.WithAmount(amount--);
        }

        public static Money operator -(Money a, Money b)
        {
            if (b.Currency == null)
                return a;

            if (a.Currency == null)
                return b.WithAmount(b.Amount * -1);

            GuardCurrencyEquality(a, b);
            return a.WithAmount(a.Amount - b.Amount);
        }

        public static Money operator -(Money a, int b) => a + (decimal)b;
        public static Money operator -(Money a, float b) => a + (decimal)b;
        public static Money operator -(Money a, double b) => a + (decimal)b;
        public static Money operator -(Money a, decimal b) => a.WithAmount(a.Amount - b);

        #endregion

        #region Multiply

        public static Money operator *(Money a, Money b)
        {
            GuardCurrencyEquality(a, b);
            return a.WithAmount(a.Amount * b.Amount);
        }

        public static Money operator *(Money a, int b) => a * (decimal)b;
        public static Money operator *(Money a, float b) => a * (decimal)b;
        public static Money operator *(Money a, double b) => a * (decimal)b;
        public static Money operator *(Money a, decimal b) => a.WithAmount(a.Amount * b);

        #endregion

        #region Divide

        public static Money operator /(Money a, Money b)
        {
            GuardCurrencyEquality(a, b);
            return a.WithAmount(a.Amount / b.Amount);
        }

        public static Money operator /(Money a, int b) => a / (decimal)b;
        public static Money operator /(Money a, float b) => a / (decimal)b;
        public static Money operator /(Money a, double b) => a / (decimal)b;
        public static Money operator /(Money a, decimal b) => a.WithAmount(a.Amount / b);

        #endregion

        #region Exchange & Math

        //public Money ExchangeTo(Currency toCurrency)
        //{
        //    if (Currency == toCurrency || Amount == 0)
        //        return this;

        //    return new Money(Amount * Currency.Rate / toCurrency.Rate, toCurrency, HideCurrency, PostFormat);
        //}

        /// <summary>
        /// Exchanges <see cref="Amount"/> by multiplying with given <paramref name="exchangeRate"/>.
        /// </summary>
        /// <param name="exchangeRate">Exchange rate.</param>
        public Money Exchange(decimal exchangeRate)
            => WithAmount(Amount * exchangeRate);

        /// <summary>
        /// Exchanges <see cref="Amount"/> to <paramref name="toCurrency"/> using <paramref name="toCurrency"/> as primary exchange currency.
        /// </summary>
        /// <param name="toCurrency">The target currency</param>
        public Money ExchangeTo(Currency toCurrency)
            => ExchangeTo(toCurrency, toCurrency);

        /// <summary>
        /// Exchanges <see cref="Amount"/> to <paramref name="toCurrency"/> using <paramref name="exchangeCurrency"/> as primary exchange currency.
        /// </summary>
        /// <param name="toCurrency">The target currency</param>
        /// <param name="exchangeCurrency">Primary exchange currency.</param>
        public Money ExchangeTo(Currency toCurrency, Currency exchangeCurrency)
        {
            Guard.NotNull(toCurrency);
            Guard.NotNull(exchangeCurrency);

            if (Currency == toCurrency)
                return this;

            if (Amount == 0)
                return WithCurrency(toCurrency);

            var amount = Amount;

            // Source --> Exchange
            if (Currency != exchangeCurrency)
            {
                amount /= Currency.Rate;
            }

            // Exchange --> Target
            if (toCurrency != exchangeCurrency)
            {
                amount *= toCurrency.Rate;
            }

            return new Money(amount, toCurrency, HideCurrency, PostFormat);
        }

        /// <summary>
        /// Evenly distributes the amount over n parts, resolving remainders that occur due to rounding 
        /// errors, thereby garuanteeing the postcondition: result->sum(r|r.amount) = this.amount and
        /// x elements in result are greater than at least one of the other elements, where x = amount mod n.
        /// </summary>
        /// <param name="n">Number of parts over which the amount is to be distibuted.</param>
        /// <returns>Array with distributed Money amounts.</returns>
        public Money[] Allocate(int n)
        {
            var cents = Math.Pow(10, DecimalDigits);
            var lowResult = ((long)Math.Truncate((double)Amount / n * cents)) / cents;
            var highResult = lowResult + 1.0d / cents;
            var results = new Money[n];
            var remainder = (int)(((double)Amount * cents) % n);

            for (var i = 0; i < remainder; i++)
                results[i] = WithAmount(highResult);

            for (var i = remainder; i < n; i++)
                results[i] = WithAmount(lowResult);

            return results;
        }

        /// <summary>
        /// Gets the ratio of one money to another.
        /// </summary>
        /// <param name="numerator">The numerator of the operation.</param>
        /// <param name="denominator">The denominator of the operation.</param>
        /// <returns>A decimal from 0.0 to 1.0 of the ratio between the two money values.</returns>
        public static decimal GetRatio(Money numerator, Money denominator)
        {
            if (numerator == 0)
                return 0;

            if (denominator == 0)
                throw new DivideByZeroException("Attempted to divide by zero!");

            GuardCurrencyEquality(numerator, denominator);

            return numerator.Amount / denominator.Amount;
        }

        /// <summary>
        /// Gets the smallest money, given the two values.
        /// </summary>
        /// <param name="a">The first money to compare.</param>
        /// <param name="b">The second money to compare.</param>
        /// <returns>The smallest money value of the arguments.</returns>
        public static Money Min(Money a, Money b)
        {
            GuardCurrencyEquality(a, b);

            if (a == b)
                return a;
            else if (a > b)
                return b;
            else
                return a;
        }

        /// <summary>
        /// Gets the largest money, given the two values.
        /// </summary>
        /// <param name="a">The first money to compare.</param>
        /// <param name="b">The second money to compare.</param>
        /// <returns>The largest money value of the arguments.</returns>
        public static Money Max(Money a, Money b)
        {
            GuardCurrencyEquality(a, b);

            if (a == b)
                return a;
            else if (a > b)
                return a;
            else
                return b;
        }

        /// <summary>
        /// Gets the absolute value of the <see cref="Money"/>.
        /// </summary>
        /// <param name="value">The value of money to convert.</param>
        /// <returns>The money value as an absolute value.</returns>
        public static Money Abs(Money value)
        {
            return value.WithAmount(Math.Abs(value.Amount));
        }

        #endregion
    }

    internal sealed class MoneyJsonConverter : JsonConverter<Money>
    {
        public override bool CanRead
            => false;

        public override bool CanWrite
            => true;

        public override Money ReadJson(JsonReader reader, Type objectType, Money existingValue, bool hasExistingValue, JsonSerializer serializer)
            => throw new NotSupportedException();

        public override void WriteJson(JsonWriter writer, Money value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToString());
        }
    }
}
