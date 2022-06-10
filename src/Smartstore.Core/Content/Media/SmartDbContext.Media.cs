using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<MediaFolder> MediaFolders { get; set; }
        public DbSet<MediaAlbum> MediaAlbums { get; set; }
        public DbSet<MediaTrack> MediaTracks { get; set; }
        public DbSet<MediaTag> MediaTags { get; set; }
        public DbSet<MediaStorage> MediaStorage { get; set; }
        public DbSet<Download> Downloads { get; set; }
    }
}