//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Smartstore.Caching
//{
//    // TODO: (core) implement this properly (Timing/duration, prefixes etc.)

//    public struct CacheKey : IEquatable<CacheKey>
//    {
//        public CacheKey(string key)
//        {
//            KeyValue = key;
//        }

//        internal string KeyValue { get; }

//        public bool IsNull =>
//            KeyValue == null;

//        public bool IsEmpty
//        {
//            get
//            {
//                return string.IsNullOrEmpty(KeyValue);
//            }
//        }

//        public static bool operator ==(CacheKey x, CacheKey y) =>
//            x.KeyValue == y.KeyValue;

//        public static bool operator !=(CacheKey x, CacheKey y) =>
//            !(x == y);

//        public static bool operator ==(CacheKey x, string y) =>
//            x.KeyValue == y;

//        public static bool operator !=(CacheKey x, string y) =>
//            !(x == y);

//        public static bool operator ==(string x, CacheKey y) =>
//            x == y.KeyValue;

//        public static bool operator !=(string x, CacheKey y) =>
//            !(x == y);

//        public override bool Equals(object obj)
//        { 
//            if (obj is CacheKey key)
//            {
//                return key == this;
//            }
//            else if (obj is string str)
//            {
//                return str == this;
//            }

//            return false;
//        }

//        public bool Equals(CacheKey other) =>
//            other == this;

//        public override int GetHashCode()
//        {
//            unchecked
//            {
//                return this.KeyValue?.GetHashCode() ?? 0;
//            }
//        }

//        public static implicit operator string(CacheKey key)
//        {
//            return key.KeyValue;
//        }

//        public static implicit operator CacheKey(string key)
//        {
//            if (key == null)
//            {
//                return new CacheKey();
//            }

//            return new CacheKey(key);
//        }
//    }
//}
