using System.Security.Cryptography;

namespace Smartstore.Utilities
{
    public sealed class NumberRandomizer : RandomNumberGenerator
    {
        private readonly RandomNumberGenerator _impl;

        public NumberRandomizer()
        {
            _impl = Create();
        }

        public int Next()
        {
            var data = new byte[sizeof(int)];
            _impl.GetBytes(data);
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
            _impl.GetBytes(data);
            var randUint = BitConverter.ToUInt32(data, 0);
            return randUint / (uint.MaxValue + 1.0);
        }

        public override void GetBytes(byte[] data)
        {
            _impl.GetBytes(data);
        }

        public override void GetNonZeroBytes(byte[] data)
        {
            _impl.GetNonZeroBytes(data);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _impl.Dispose();
        }
    }
}