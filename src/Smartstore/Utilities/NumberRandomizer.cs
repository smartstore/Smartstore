using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Utilities
{
    public sealed class NumberRandomizer : RandomNumberGenerator
    {
        private readonly RNGCryptoServiceProvider _provider;

        public NumberRandomizer()
        {
            _provider = new RNGCryptoServiceProvider();
        }

        public int Next()
        {
            var data = new byte[sizeof(int)];
            _provider.GetBytes(data);
            return BitConverter.ToInt32(data, 0) & (int.MaxValue - 1);
        }

        public int Next(int maxValue)
        {
            return Next(0, maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            return (int)Math.Floor(minValue + ((double)maxValue - minValue) * NextDouble());
        }

        public double NextDouble()
        {
            var data = new byte[sizeof(uint)];
            _provider.GetBytes(data);
            var randUint = BitConverter.ToUInt32(data, 0);
            return randUint / (uint.MaxValue + 1.0);
        }

        public override void GetBytes(byte[] data)
        {
            _provider.GetBytes(data);
        }

        public override void GetNonZeroBytes(byte[] data)
        {
            _provider.GetNonZeroBytes(data);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _provider.Dispose();
        }
    }
}