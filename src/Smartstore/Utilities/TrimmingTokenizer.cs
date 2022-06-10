// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using Microsoft.Extensions.Primitives;

namespace Smartstore.Utilities
{
    /// <summary>
    /// Splits a <see cref="string"/> or <see cref="StringSegment"/> into trimmed <see cref="StringSegment"/>s. Also
    /// skips empty <see cref="StringSegment"/>s.
    /// </summary>
    public struct TrimmingTokenizer : IEnumerable<StringSegment>
    {
        private readonly int _maxCount;
        private readonly StringSegment _originalString;
        private readonly StringTokenizer _tokenizer;

        /// <summary>
        /// Instantiates a new <see cref="TrimmingTokenizer"/> with given <paramref name="value"/>. Will split segments
        /// using given <paramref name="separators"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to split and trim.</param>
        /// <param name="separators">The collection of separator <see cref="char"/>s controlling the split.</param>
        public TrimmingTokenizer(string value, char[] separators)
            : this(value, separators, maxCount: int.MaxValue)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="TrimmingTokenizer"/> with given <paramref name="value"/>. Will split up to
        /// <paramref name="maxCount"/> segments using given <paramref name="separators"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to split and trim.</param>
        /// <param name="separators">The collection of separator <see cref="char"/>s controlling the split.</param>
        /// <param name="maxCount">The maximum number of <see cref="StringSegment"/>s to return.</param>
        public TrimmingTokenizer(string value, char[] separators, int maxCount)
            : this(new StringSegment(value), separators, maxCount)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="TrimmingTokenizer"/> with given <paramref name="value"/>. Will split segments
        /// using given <paramref name="separators"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to split and trim.</param>
        /// <param name="separators">The collection of separator <see cref="char"/>s controlling the split.</param>
        public TrimmingTokenizer(StringSegment value, char[] separators)
            : this(value, separators, maxCount: int.MaxValue)
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="TrimmingTokenizer"/> with given <paramref name="value"/>. Will split up to
        /// <paramref name="maxCount"/> segments using given <paramref name="separators"/>.
        /// </summary>
        /// <param name="value">The <see cref="StringSegment"/> to split and trim.</param>
        /// <param name="separators">The collection of separator <see cref="char"/>s controlling the split.</param>
        /// <param name="maxCount">The maximum number of <see cref="StringSegment"/>s to return.</param>
        public TrimmingTokenizer(StringSegment value, char[] separators, int maxCount)
        {
            // !HasValue matches odd-looking (for a struct) value==null check in StringTokenizer(...).
            if (!value.HasValue)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (separators == null)
            {
                throw new ArgumentNullException(nameof(separators));
            }
            if (maxCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            _maxCount = maxCount;
            _originalString = value;
            _tokenizer = new StringTokenizer(value, separators);
        }

        /// <summary>
        /// Gets the number of elements in this <see cref="TrimmingTokenizer"/>.
        /// </summary>
        /// <remarks>
        /// Provided to avoid either (or both) <c>System.Linq</c> use or boxing the <see cref="TrimmingTokenizer"/>.
        /// </remarks>
        public int Count
        {
            get
            {
                var enumerator = GetEnumerator();
                var count = 0;
                while (enumerator.MoveNext())
                {
                    count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Returns an <see cref="Enumerator"/> that iterates through the split and trimmed
        /// <see cref="StringSegment"/>s.
        /// </summary>
        /// <returns>
        /// An <see cref="Enumerator"/> that iterates through the split and trimmed <see cref="StringSegment"/>s.
        /// </returns>
        public Enumerator GetEnumerator() => new(this);

        /// <inheritdoc />
        IEnumerator<StringSegment> IEnumerable<StringSegment>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// An <see cref="IEnumerator{StringSegment}"/> wrapping <see cref="StringTokenizer.Enumerator"/> and providing
        /// trimmed <see cref="StringSegment"/>s.
        /// </summary>
        public struct Enumerator : IEnumerator<StringSegment>, IEnumerator, IDisposable
        {
            private readonly TrimmingTokenizer _tokenizer;

            private int _count;
            private StringTokenizer.Enumerator _enumerator;
            private StringSegment _remainder;

            /// <summary>
            /// Instantiates a new <see cref="Enumerator"/> instance for <paramref name="tokenizer"/>.
            /// </summary>
            /// <param name="tokenizer">The containing <see cref="TrimmingTokenizer"/>.</param>
            public Enumerator(TrimmingTokenizer tokenizer)
            {
                _tokenizer = tokenizer;
                _count = 0;
                _enumerator = tokenizer._tokenizer.GetEnumerator();
                _remainder = StringSegment.Empty;
            }

            /// <inheritdoc />
            public StringSegment Current
            {
                get
                {
                    if (_count < _tokenizer._maxCount)
                    {
                        return _enumerator.Current.Trim();
                    }

                    return _remainder;
                }
            }

            /// <inheritdoc />
            object IEnumerator.Current => Current;

            /// <inheritdoc />
            public void Dispose() => _enumerator.Dispose();

            /// <inheritdoc />
            public bool MoveNext()
            {
                // Do nothing except return false if _maxCount == 0.
                var result = false;
                if (_count < _tokenizer._maxCount)
                {
                    // Keep moving until enumeration is done or we find a non-empty (and non-whitespace) segment.
                    do
                    {
                        result = _enumerator.MoveNext();
                    }
                    while (result && StringSegment.IsNullOrEmpty(Current));

                    if (result)
                    {
                        if (_count + 1 >= _tokenizer._maxCount)
                        {
                            _remainder = _tokenizer._originalString
                                .Subsegment(Current.Offset - _tokenizer._originalString.Offset)
                                .Trim();
                        }

                        _count++;
                    }
                }

                return result;
            }

            /// <inheritdoc />
            public void Reset()
            {
                _count = 0;
                _enumerator.Reset();
                _remainder = StringSegment.Empty;
            }
        }
    }
}