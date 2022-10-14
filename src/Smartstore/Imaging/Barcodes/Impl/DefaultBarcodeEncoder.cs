namespace Smartstore.Imaging.Barcodes.Impl
{
    internal class DefaultBarcodeEncoder : IBarcodeEncoder
    {
        public IBarcode EncodeBarcode(BarcodePayload payload)
        {
            Guard.NotNull(payload, nameof(payload));

            var type = payload.Type;
            var data = payload.Data;

            var code = type switch
            {
                BarcodeType.Aztec       => Barcoder.Aztec.AztecEncoder.Encode(data),
                BarcodeType.Codabar     => Barcoder.Codebar.CodabarEncoder.Encode(data),
                BarcodeType.Code128     => Barcoder.Code128.Code128Encoder.Encode(data),
                BarcodeType.Code39      => Barcoder.Code39.Code39Encoder.Encode(data, true, true),
                BarcodeType.Code93      => Barcoder.Code93.Code93Encoder.Encode(data, true, true),
                BarcodeType.DataMatrix  => Barcoder.DataMatrix.DataMatrixEncoder.Encode(data),
                BarcodeType.KixCode     => Barcoder.Kix.KixEncoder.Encode(data),
                BarcodeType.PDF417      => Barcoder.Pdf417.Pdf417Encoder.Encode(data, 8),
                BarcodeType.Qr          => Barcoder.Qr.QrEncoder.Encode(data, Barcoder.Qr.ErrorCorrectionLevel.Q, Barcoder.Qr.Encoding.Auto),
                BarcodeType.RoyalMail   => Barcoder.RoyalMail.RoyalMailFourStateCodeEncoder.Encode(data),
                BarcodeType.TwoToFive   => Barcoder.TwoToFive.TwoToFiveEncoder.Encode(data, true, true),
                BarcodeType.UPCA        => Barcoder.UpcA.UpcAEncoder.Encode(data),
                BarcodeType.UPCE        => Barcoder.UpcE.UpcEEncoder.Encode(data, Barcoder.UpcE.UpcENumberSystem.Zero),
                _                       => Barcoder.Ean.EanEncoder.Encode(data)
            };

            return new DefaultBarcode(code, payload);
        }
    }
}
