namespace Smartstore.Core.Content.Media
{
    // TODO: (mh) Remove this enum
    // TODO: (mh) Use a float number in blog settings instead (0.75, 0.625, 0.5625 etc.)
    // TODO: (mh) Localization: 1:1 (square), 4:3 (standard), 16:9 (widescreen), 16:10 (HD), 21:9 (cinematic)

    /// <summary>
    /// Represents standardized aspect ratios for media content.
    /// </summary>
    public enum AspectRatio
    {
        AR1by1,
        AR4by3,
        AR16by9,
        AR16by10,
        AR21by9
    }
}