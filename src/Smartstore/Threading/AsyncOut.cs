namespace Smartstore.Threading
{
    /// <summary>
    /// Mimics the neat and tidy <code>bool TryXyz(out T value)</code> pattern in async methods.
    /// </summary>
    /// <example>
    /// if ((await TryGetValueAsync()).Out(out var value)) { ... }
    /// </example>
    /// <typeparam name="TOut"></typeparam>
    public struct AsyncOut<TOut>
    {
        public static AsyncOut<TOut> Empty = new(false);

        public AsyncOut(bool success, TOut value = default)
        {
            Success = success;
            Value = value;
        }

        public bool Success { get; }
        public TOut Value { get; }

        public bool Out(out TOut value)
        {
            value = Value;
            return Success;
        }
    }
}
