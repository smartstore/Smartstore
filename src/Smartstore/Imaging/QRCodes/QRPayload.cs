namespace Smartstore.Imaging.QRCodes
{
    public abstract class QRPayload
    {
        // TODO: Should implement IEquatable<QrPayload>, GetHashCode etc.
        public abstract string Serialize();

        public override string ToString()
        {
            return Serialize();
        }
    }
}
