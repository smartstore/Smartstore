using System;
using System.Collections.Generic;
using System.Threading;

namespace Smartstore.Core.Content.Media
{
    public interface IMediaTrackDetector
    {
        void ConfigureTracks(string albumName, TrackedMediaPropertyTable table);
        IAsyncEnumerable<MediaTrack> DetectAllTracksAsync(string albumName, CancellationToken cancelToken = default);
    }
}