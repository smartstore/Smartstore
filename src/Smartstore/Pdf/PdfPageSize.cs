namespace Smartstore.Pdf
{
    /// <summary>
    /// All page sizes from http://doc.qt.io/archives/qt-4.8/qprinter.html#PaperSize-enum
    /// </summary>
    public enum PdfPageSize
    {
        A0 = 5, //841 x 1189 mm
        A1 = 6, //594 x 841 mm
        A2 = 7, //420 x 594 mm
        A3 = 8, //297 x 420 mm
        A4 = 0, //210 x 297 mm, 8.26 x 11.69 inches, Default
        A5 = 9, //148 x 210 mm
        A6 = 10, //105 x 148 mm
        A7 = 11, //74 x 105 mm
        A8 = 12, //52 x 74 mm
        A9 = 13, //37 x 52 mm
        B0 = 14, //1000 x 1414 mm
        B1 = 15, //707 x 1000 mm
        B2 = 17, //500 x 707 mm
        B3 = 18, //353 x 500 mm
        B4 = 19, //250 x 353 mm
        B5 = 1, //176 x 250 mm, 6.93 x 9.84 inches
        B6 = 20, //125 x 176 mm
        B7 = 21, //88 x 125 mm
        B8 = 22, //62 x 88 mm
        B9 = 23, //33 x 62 mm
        B10 = 16, //31 x 44 mm
        C5E = 24, //163 x 229 mm
        Comm10E = 25, //105 x 241 mm, U.S. Common 10 Envelope
        DLE = 26, //110 x 220 mm
        Executive = 4, //7.5 x 10 inches, 190.5 x 254 mm
        Folio = 27, //210 x 330 mm
        Ledger = 28, //431.8 x 279.4 mm
        Legal = 3, //8.5 x 14 inches, 215.9 x 355.6 mm
        Letter = 2, //8.5 x 11 inches, 215.9 x 279.4 mm
        Tabloid = 29 //279.4 x 431.8 mm
    }
}