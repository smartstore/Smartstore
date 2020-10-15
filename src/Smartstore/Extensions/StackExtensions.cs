using System.Collections.Generic;

namespace Smartstore
{
    public static class StackExtensions
    {
		public static bool TryPeek<T>(this Stack<T> stack, out T value)
		{
			value = default;

			if (stack.Count > 0)
			{
				value = stack.Peek();
				return true;
			}

			return false;
		}

		public static bool TryPop<T>(this Stack<T> stack, out T value)
		{
			value = default;

			if (stack.Count > 0)
			{
				value = stack.Pop();
				return true;
			}

			return false;
		}
	}
}
