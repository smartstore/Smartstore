namespace Smartstore.Engine
{
    /// <summary>
    /// Represent current OS user (either WindowsIdentity or LinuxUser).
    /// </summary>
    public interface IOSIdentity
    {
        /// <summary>
        /// Gets OS user name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets Windows domain name or Linux group.
        /// </summary>
        string Domain { get; }

        /// <summary>
        /// Gets full OS user name.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets user groups that the current OS user is assigned to.
        /// </summary>
        IReadOnlyCollection<string> Groups { get; }

        /// <summary>
        /// Gets Windows user name or Linux user id.
        /// </summary>
        string UserId { get; }
    }
}
