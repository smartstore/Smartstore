namespace Smartstore.Imaging.QRCodes
{
    /// <summary>
    /// Abstract base class for QR code payloads.
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
            => Serialize();

        public bool Equals(QRPayload other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            // TODO: (core) Perf?!
            return this.GetType() == other.GetType() && 
                this.Serialize() == other.Serialize();
        }

        public override bool Equals(object obj)
            => Equals(obj as QRPayload);

        public override int GetHashCode()
            => Serialize().GetHashCode();
    }
}
