namespace Smartstore.IO
{
    public class FileSystemException : Exception
    {
        public FileSystemException(string message) : base(message)
        {
        }

        public FileSystemException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
