namespace Smartstore
{
    public static class DataExceptionExtensions
    {
        /// <summary>
        /// Checks whether the exception indicates attaching an already attached entity (HResult -2146233079)
        /// "Attaching an entity of type 'x' failed because another entity of the same type already has the same primary key value."
        /// </summary>
        /// <param name="exception">Invalid operation exception</param>
        /// <returns></returns>
        public static bool IsAlreadyAttachedEntityException(this InvalidOperationException exception)
        {
            return exception != null && exception.HResult == -2146233079;
        }
    }
}