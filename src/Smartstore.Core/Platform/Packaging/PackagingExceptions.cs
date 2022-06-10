namespace Smartstore.Core.Packaging
{
    public class InvalidExtensionException : Exception
    {
        public InvalidExtensionException()
            : base()
        {
        }

        public InvalidExtensionException(string message)
            : base(message)
        {
        }

        public InvalidExtensionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class InvalidExtensionPackageException : Exception
    {
        public InvalidExtensionPackageException()
            : base()
        {
        }

        public InvalidExtensionPackageException(string message)
            : base(message)
        {
        }

        public InvalidExtensionPackageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
