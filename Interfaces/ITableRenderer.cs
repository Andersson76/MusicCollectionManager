using MusicCollectionManager.Models;
using System.Collections.Generic;

namespace MusicCollectionManager.Services
{
    public interface ITableRenderer
    {
        void RenderArtistTable(IEnumerable<Artist> artists);
        void RenderAlbumTable(IEnumerable<Album> albums, IEnumerable<Artist> artists);
        void RenderSongTable(IEnumerable<Song> songs, IEnumerable<Album> albums, IEnumerable<Artist> artists);
    }
}