namespace Smartstore.Core.Web
{
    /// <summary>
    /// User agent device info.
    /// </summary>
    public readonly struct UserAgentDevice
    {
        internal readonly static UserAgentDevice UnknownDevice = new("Unknown", UserAgentDeviceType.Unknown);

        /// <summary>
        /// Creates a new instance of <see cref="UserAgentDevice"/>
        /// </summary>
        public UserAgentDevice(string name, UserAgentDeviceType type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Device name of agent, e.g. "Android", "Apple iPhone", "BlackBerry", "Samsung", "PlayStation", "Windows CE" etc.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Type of device.
        /// </summary>
        public UserAgentDeviceType Type { get; }

        public bool IsUnknown() => Name == "Unknown";
    }

    /// <summary>
    /// User Agent device types
    /// </summary>
    public enum UserAgentDeviceType : byte
    {
        /// <summary>
        /// Unknown / not mapped
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Unsupported (for future use)
        /// </summary>
        Wearable,
        /// <summary>
        /// Unsupported (for future use)
        /// </summary>
        Camera,
        /// <summary>
        /// Mobile phone
        /// </summary>
        Smartphone,
        /// <summary>
        /// Table, Phablet
        /// </summary>
        Tablet,
        /// <summary>
        /// Unsupported (for future use)
        /// </summary>
        Desktop,
        /// <summary>
        /// Unsupported (for future use)
        /// </summary>
        MediaPlayer,
        /// <summary>
        /// Unsupported (for future use)
        /// </summary>
        TV
    }
}
