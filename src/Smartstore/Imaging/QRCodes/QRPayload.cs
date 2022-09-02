namespace Smartstore.Imaging.QRCodes
{
    /// <summary>
    /// Interface for payload implementation for QR code generation.
    /// </summary>
    public abstract class QRPayload : IEquatable<QRPayload>
    {
        /// <summary>
        /// Serializes the payload object.
        /// </summary>
        /// <returns>Payload as string.</returns>
        public abstract string Serialize();

        /// <summary>
        /// Serializes the payload object.
        /// </summary>
        /// <returns>Payload as string.</returns>
        public override string ToString()
        {
            return Serialize();
        }

        public bool Equals(QRPayload other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return false;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as QRPayload);
        }

        public override int GetHashCode()
            => Serialize().GetHashCode();
    }
}
