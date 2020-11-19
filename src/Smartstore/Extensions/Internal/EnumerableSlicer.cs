using System;
using System.Collections.Generic;

namespace Smartstore.Extensions.Internal
{
	internal sealed class EnumerableSlicer<T>
	{
		private readonly IEnumerator<T> _iterator;
		private readonly int[] _sizes;
		private volatile bool _hasNext;
		private volatile int _currentSize;
		private volatile int _index;

		public EnumerableSlicer(IEnumerator<T> iterator, int[] sizes)
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

		public IEnumerable<IEnumerable<T>> Slice()
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
					_hasNext = MoveNext();
				}

				if (_hasNext)
				{
					yield return new List<T>(SliceInternal());
					index += size;
				}
			}
		}

		private IEnumerable<T> SliceInternal()
		{
			if (_currentSize == -1) yield break;
			yield return _iterator.Current;

			for (var count = 0; count < _currentSize && _hasNext; ++count)
			{
				_hasNext = MoveNext();

				if (_hasNext)
				{
					yield return _iterator.Current;
				}
			}
		}

		private bool MoveNext()
		{
			++_index;
			return _iterator.MoveNext();
		}
	}
}
