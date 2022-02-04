using System.Runtime.CompilerServices;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Messaging;
using Smartstore.Core.Stores;
using Smartstore.Data;

namespace Smartstore.Core.Content.Media
{
    public class SystemAlbumProvider : IAlbumProvider, IMediaTrackDetector
    {
        private readonly SmartDbContext _db;

        public SystemAlbumProvider(SmartDbContext db)
        {
            _db = db;
        }

        public const string Catalog = "catalog";
        public const string Content = "content";
        public const string Downloads = "download";
        public const string Messages = "message";
        public const string Customers = "customer";
        public const string Files = "file";

        #region Album Provider

        public IEnumerable<MediaAlbum> GetAlbums()
        {
            return new[]
            {
                new MediaAlbum
                {
                    Name = Catalog,
                    ResKey = "Admin.Catalog",
                    CanDetectTracks = true,
                    Order = int.MinValue
                },
                new MediaAlbum
                {
                    Name = Content,
                    ResKey = "Admin.Media.Album.Content",
                    CanDetectTracks = true,
                    Order = int.MinValue + 10
                },
                new MediaAlbum
                {
                    Name = Downloads,
                    ResKey = "Common.Downloads",
                    CanDetectTracks = true,
                    Order = int.MinValue + 30
                },
                new MediaAlbum
                {
                    Name = Messages,
                    ResKey = "Admin.Media.Album.Message",
                    CanDetectTracks = true,
                    Order = int.MinValue + 40
                },
                new MediaAlbum
                {
                    Name = Customers,
                    ResKey = "Admin.Customers",
                    CanDetectTracks = true, // TBD
                    Order = int.MinValue + 50
                },
                new MediaAlbum
                {
                    Name = Files,
                    ResKey = "Admin.Media.Album.File",
                    CanDetectTracks = false,
                    IncludePath = true,
                    Order = int.MaxValue
                }
            };
        }

        public AlbumDisplayHint GetDisplayHint(MediaAlbum album)
        {
            if (album.Name == Catalog)
            {
                return new AlbumDisplayHint { OverlayIcon = "fa fa-cube" };
            }
            if (album.Name == Content)
            {
                return new AlbumDisplayHint { OverlayIcon = "fa fa-sitemap" };
            }
            if (album.Name == Downloads)
            {
                return new AlbumDisplayHint { OverlayIcon = "fa fa-download" };
            }
            if (album.Name == Messages)
            {
                return new AlbumDisplayHint { OverlayIcon = "fa fa-envelope" };
            }
            if (album.Name == Customers)
            {
                return new AlbumDisplayHint { OverlayIcon = "fa fa-user" };
            }
            if (album.Name == Files)
            {
                // TODO: var(--success) should be system default.
                return new AlbumDisplayHint { Color = "var(--success)" };
            }

            return null;
        }

        #endregion

        #region Tracking

        public bool MatchAlbum(string albumName)
        {
            return albumName switch
            {
                Catalog or Content or Downloads or Messages or Customers => true,
                _ => false,
            };
        }

        public void ConfigureTracks(string albumName, TrackedMediaPropertyTable table)
        {
            if (albumName == Catalog)
            {
                table.Register<ProductMediaFile>(x => x.MediaFileId);
                table.Register<ProductAttributeOption>(x => x.MediaFileId);
                table.Register<ProductVariantAttributeValue>(x => x.MediaFileId);
                table.Register<CheckoutAttributeValue>(x => x.MediaFileId);
                table.Register<SpecificationAttributeOption>(x => x.MediaFileId);
                table.Register<Category>(x => x.MediaFileId);
                table.Register<Manufacturer>(x => x.MediaFileId);
            }
            else if (albumName == Content)
            {
                table.Register<Store>(x => x.LogoMediaFileId);
                table.Register<Store>(x => x.FavIconMediaFileId);
                table.Register<Store>(x => x.PngIconMediaFileId);
                table.Register<Store>(x => x.AppleTouchIconMediaFileId);
                table.Register<Store>(x => x.MsTileImageMediaFileId);
            }
            else if (albumName == Downloads)
            {
                table.Register<Download>(x => x.MediaFileId);
            }
            else if (albumName == Messages)
            {
                // TODO: (mm) (mc) These props are localizable
                table.Register<MessageTemplate>(x => x.Attachment1FileId);
                table.Register<MessageTemplate>(x => x.Attachment2FileId);
                table.Register<MessageTemplate>(x => x.Attachment3FileId);
            }
        }

        public async IAsyncEnumerable<MediaTrack> DetectAllTracksAsync(string albumName, [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            var entityName = string.Empty;

            // Catalog
            if (albumName == Catalog)
            {
                // Products
                {
                    var name = nameof(ProductMediaFile);
                    var p = new FastPager<ProductMediaFile>(_db.ProductMediaFiles.AsNoTracking(), 5000);
                    while ((await p.ReadNextPageAsync(x => new { x.Id, x.ProductId, x.MediaFileId }, x => x.Id, cancelToken)).Out(out var list))
                    {
                        foreach (var x in list)
                        {
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.MediaFileId, Property = nameof(x.MediaFileId) };
                        }
                        list.Clear();
                    }
                }

                // ProductAttributeOption
                {
                    var name = nameof(ProductAttributeOption);
                    var p = new FastPager<ProductAttributeOption>(_db.ProductAttributeOptions.AsNoTracking().Where(x => x.MediaFileId > 0), 5000);
                    while ((await p.ReadNextPageAsync(x => new { x.Id, x.MediaFileId }, x => x.Id, cancelToken)).Out(out var list))
                    {
                        foreach (var x in list)
                        {
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.MediaFileId, Property = nameof(x.MediaFileId) };
                        }
                        list.Clear();
                    }
                }

                // ProductVariantAttributeValue
                {
                    var name = nameof(ProductVariantAttributeValue);
                    var p = new FastPager<ProductVariantAttributeValue>(_db.ProductVariantAttributeValues.AsNoTracking().Where(x => x.MediaFileId > 0), 5000);
                    while ((await p.ReadNextPageAsync(x => new { x.Id, x.MediaFileId }, x => x.Id, cancelToken)).Out(out var list))
                    {
                        foreach (var x in list)
                        {
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.MediaFileId, Property = nameof(x.MediaFileId) };
                        }
                        list.Clear();
                    }
                }

                // CheckoutAttributeValue
                {
                    var name = nameof(CheckoutAttributeValue);
                    var p = new FastPager<CheckoutAttributeValue>(_db.CheckoutAttributeValues.AsNoTracking().Where(x => x.MediaFileId.HasValue), 5000);
                    while ((await p.ReadNextPageAsync(x => new { x.Id, x.MediaFileId }, x => x.Id, cancelToken)).Out(out var list))
                    {
                        foreach (var x in list)
                        {
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.MediaFileId.Value, Property = nameof(x.MediaFileId) };
                        }
                        list.Clear();
                    }
                }

                // SpecificationAttributeOption
                {
                    var name = nameof(SpecificationAttributeOption);
                    var p = new FastPager<SpecificationAttributeOption>(_db.SpecificationAttributeOptions.AsNoTracking().Where(x => x.MediaFileId > 0), 5000);
                    while ((await p.ReadNextPageAsync(x => new { x.Id, x.MediaFileId }, x => x.Id, cancelToken)).Out(out var list))
                    {
                        foreach (var x in list)
                        {
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.MediaFileId, Property = nameof(x.MediaFileId) };
                        }
                        list.Clear();
                    }
                }

                // Categories
                {
                    var name = nameof(Category);
                    var p = new FastPager<Category>(_db.Categories.AsNoTracking().Where(x => x.MediaFileId.HasValue), 5000);
                    while ((await p.ReadNextPageAsync(x => new { x.Id, x.MediaFileId }, x => x.Id, cancelToken)).Out(out var list))
                    {
                        foreach (var x in list)
                        {
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.MediaFileId.Value, Property = nameof(x.MediaFileId) };
                        }
                        list.Clear();
                    }
                }

                // Brands
                {
                    var name = nameof(Manufacturer);
                    var p = new FastPager<Manufacturer>(_db.Manufacturers.AsNoTracking().Where(x => x.MediaFileId.HasValue), 5000);
                    while ((await p.ReadNextPageAsync(x => new { x.Id, x.MediaFileId }, x => x.Id, cancelToken)).Out(out var list))
                    {
                        foreach (var x in list)
                        {
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.MediaFileId.Value, Property = nameof(x.MediaFileId) };
                        }
                        list.Clear();
                    }
                }

                yield break;
            }

            // Content
            if (albumName == Content)
            {
                // Store
                {
                    var name = nameof(Store);
                    var p = new FastPager<Store>(_db.Stores.AsNoTracking().Where(x => x.LogoMediaFileId > 0));
                    while ((await p.ReadNextPageAsync(x => new { x.Id, x.LogoMediaFileId }, x => x.Id, cancelToken)).Out(out var list))
                    {
                        foreach (var x in list)
                        {
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.LogoMediaFileId, Property = nameof(x.LogoMediaFileId) };
                        }
                        list.Clear();
                    }
                }

                yield break;
            }

            // Downloads
            if (albumName == Downloads)
            {
                var name = nameof(Download);
                var p = new FastPager<Download>(_db.Downloads.AsNoTracking().Where(x => x.MediaFileId.HasValue), 5000);
                while ((await p.ReadNextPageAsync(x => new { x.Id, x.MediaFileId }, x => x.Id)).Out(out var list))
                {
                    foreach (var x in list)
                    {
                        yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.MediaFileId.Value, Property = nameof(x.MediaFileId) };
                    }
                    list.Clear();
                }

                yield break;
            }

            // Messages
            if (albumName == Messages)
            {
                var name = nameof(MessageTemplate);
                var p = new FastPager<MessageTemplate>(_db.MessageTemplates.AsNoTracking());
                while ((await p.ReadNextPageAsync(x => new { x.Id, x.Attachment1FileId, x.Attachment2FileId, x.Attachment3FileId }, x => x.Id, cancelToken)).Out(out var list))
                {
                    foreach (var x in list)
                    {
                        if (x.Attachment1FileId.HasValue)
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.Attachment1FileId.Value, Property = nameof(x.Attachment1FileId) };
                        if (x.Attachment2FileId.HasValue)
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.Attachment2FileId.Value, Property = nameof(x.Attachment2FileId) };
                        if (x.Attachment3FileId.HasValue)
                            yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.Attachment3FileId.Value, Property = nameof(x.Attachment3FileId) };
                    }
                    list.Clear();
                }

                yield break;
            }

            // Customer
            if (albumName == Customers)
            {
                var name = nameof(Customer);
                var key = SystemCustomerAttributeNames.AvatarPictureId;

                // Avatars
                var p = new FastPager<GenericAttribute>(_db.GenericAttributes.AsNoTracking().Where(x => x.KeyGroup == nameof(Customer) && x.Key == key));
                while ((await p.ReadNextPageAsync(x => new { x.Id, x.EntityId, x.Value }, x => x.Id, cancelToken)).Out(out var list))
                {
                    foreach (var x in list)
                    {
                        var id = x.Value.ToInt();
                        if (id > 0)
                        {
                            yield return new MediaTrack { EntityId = x.EntityId, EntityName = name, MediaFileId = id, Property = key };
                        }
                    }
                    list.Clear();
                }

                yield break;
            }
        }

        #endregion
    }
}
