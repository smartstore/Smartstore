using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;

namespace Smartstore.Extensions.Internal
{
    internal sealed class AsyncEnumerableSlicer<T>
    {
        private readonly IAsyncEnumerator<T> _iterator;
        private readonly int[] _sizes;
        private volatile bool _hasNext;
        private volatile int _currentSize;
        private volatile int _index;

        public AsyncEnumerableSlicer(IAsyncEnumerator<T> iterator, int[] sizes)
        {
            _iterator = iterator;
            _sizes = sizes;
            _index = 0;
            _currentSize = 0;
            _hasNext = true;
        }

        public int Index
        {
            get { return _index; }
        }

        public async IAsyncEnumerable<List<T>> SliceAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            var length = _sizes.Length;
            var index = 1;
            var size = 0;

            for (var i = 0; _hasNext; ++i)
            {
                if (i < length)
                {
                    size = _sizes[i];
                    _currentSize = size - 1;
                }

                while (_index < index && _hasNext)
                {
                    _hasNext = await MoveNextAsync();
                }

                if (_hasNext)
                {
                    var slice = await SliceInternalAsync().ToListAsync(cancelToken);
                    yield return slice;
                    index += size;
                }
            }
        }

        private async IAsyncEnumerable<T> SliceInternalAsync()
        {
            if (_currentSize == -1) yield break;
            yield return _iterator.Current;

            for (var count = 0; count < _currentSize && _hasNext; ++count)
            {
                _hasNext = await MoveNextAsync();

                if (_hasNext)
                {
                    yield return _iterator.Current;
                }
            }
        }

        private ValueTask<bool> MoveNextAsync()
        {
            ++_index;
            return _iterator.MoveNextAsync();
        }
    }
}
