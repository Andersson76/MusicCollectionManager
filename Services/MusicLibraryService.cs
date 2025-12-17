using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusicCollectionManager.Models;
using MusicCollectionManager.Services.Json;
using MusicCollectionManager.Services.Logging;

namespace MusicCollectionManager.Services
{
    public class MusicLibraryService
    {
        private readonly DataStore<Artist> _artistStore;
        private readonly DataStore<Album> _albumStore;
        private readonly DataStore<Song> _songStore;
        private readonly JsonFileService _jsonService;
        private readonly LogService _logService;

        public MusicLibraryService(
            DataStore<Artist> artistStore,
            DataStore<Album> albumStore,
            DataStore<Song> songStore,
            JsonFileService jsonService,
            LogService logService)
        {
            _artistStore = artistStore ?? throw new ArgumentNullException(nameof(artistStore));
            _albumStore = albumStore ?? throw new ArgumentNullException(nameof(albumStore));
            _songStore = songStore ?? throw new ArgumentNullException(nameof(songStore));
            _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _logService.LogInformationAsync("MusicLibraryService", "Initialize", "System",
                    "Starting service initialization");
                
                await LoadArtistsAsync();
                await LoadAlbumsAsync();
                await LoadSongsAsync();
                
                await _logService.LogInformationAsync("MusicLibraryService", "Initialize", "System",
                    $"Service initialized with {_artistStore.Count} artists, {_albumStore.Count} albums, {_songStore.Count} songs");
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("MusicLibraryService", "Initialize", "System",
                    "Failed to initialize service", ex);
                throw new InvalidOperationException("Failed to initialize MusicLibraryService", ex);
            }
        }
        
        public async Task SaveAllDataAsync()
        {
            try
            {
                await _logService.LogInformationAsync("MusicLibraryService", "SaveData", "System",
                    "Starting data save operation");
                
                await SaveArtistsAsync();
                await SaveAlbumsAsync();
                await SaveSongsAsync();
                
                await _logService.LogInformationAsync("MusicLibraryService", "SaveData", "System",
                    "All data saved successfully");
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("MusicLibraryService", "SaveData", "System",
                    "Failed to save data", ex);
                throw;
            }
        }
        
        #region Artist Operations
        
        public async Task<Artist> AddArtistAsync(Artist artist)
        {
            if (artist == null)
                throw new ArgumentNullException(nameof(artist));
            
            if (!artist.IsValid())
                throw new ArgumentException("Artist data is invalid");
            
            try
            {
                var existingArtist = _artistStore.Find(a => 
                    a.Name.Equals(artist.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                
                if (existingArtist != null)
                {
                    await _logService.LogWarningAsync("MusicLibraryService", "AddArtist", "Artist",
                        $"Attempted to add duplicate artist: {artist.Name}", existingArtist.Id);
                    throw new InvalidOperationException($"Artist '{artist.Name}' already exists");
                }
                
                var addedArtist = _artistStore.Add(artist);
                
                await _logService.LogCrudAsync("MusicLibraryService", "Create", "Artist", addedArtist.Id,
                    $"Added artist: {artist.Name}");
                
                return addedArtist;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("MusicLibraryService", "AddArtist", "Artist",
                    $"Failed to add artist: {artist.Name}", ex);
                throw;
            }
        }
        
        public async Task<bool> UpdateArtistAsync(Artist artist)
        {
            if (artist == null)
                throw new ArgumentNullException(nameof(artist));
            
            if (!artist.IsValid())
                throw new ArgumentException("Artist data is invalid");
            
            try
            {
                var existingArtist = _artistStore.GetById(artist.Id);
                if (existingArtist == null)
                {
                    await _logService.LogWarningAsync("MusicLibraryService", "UpdateArtist", "Artist",
                        $"Artist with ID {artist.Id} not found");
                    return false;
                }
                
                var result = _artistStore.Update(artist);
                
                if (result)
                {
                    await _logService.LogCrudAsync("MusicLibraryService", "Update", "Artist", artist.Id,
                        $"Updated artist: {artist.Name}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("MusicLibraryService", "UpdateArtist", "Artist",
                    $"Failed to update artist ID {artist.Id}", ex);
                throw;
            }
        }
        
        public async Task<bool> DeleteArtistAsync(int artistId)
        {
            try
            {
                var artist = _artistStore.GetById(artistId);
                if (artist == null)
                {
                    await _logService.LogWarningAsync("MusicLibraryService", "DeleteArtist", "Artist",
                        $"Artist with ID {artistId} not found");
                    return false;
                }
                
                // Hitta alla album av denna artist
                var artistAlbums = _albumStore.Find(a => a.ArtistId == artistId).ToList();
                
                // Ta bort alla låtar från dessa album först
                foreach (var album in artistAlbums)
                {
                    var albumSongs = _songStore.Find(s => s.AlbumId == album.Id).ToList();
                    foreach (var song in albumSongs)
                    {
                        _songStore.Delete(song.Id);
                    }
                    
                    // Ta bort albumet
                    _albumStore.Delete(album.Id);
                }
                
                // Slutligen ta bort artisten
                var result = _artistStore.Delete(artistId);
                
                if (result)
                {
                    await _logService.LogCrudAsync("MusicLibraryService", "Delete", "Artist", artistId,
                        $"Deleted artist '{artist.Name}' and {artistAlbums.Count} associated albums");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("MusicLibraryService", "DeleteArtist", "Artist",
                    $"Failed to delete artist ID {artistId}", ex);
                throw;
            }
        }
        
        public List<Artist> GetAllArtists()
        {
            return _artistStore.GetAll().ToList();
        }
        
        public List<Artist> SearchArtists(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAllArtists();
            
            return _artistStore.Find(a => 
                a.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                a.Country.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
        
        public Dictionary<Genre, List<Artist>> GetArtistsByGenre()
        {
            var artists = GetAllArtists();
            
            return artists
                .GroupBy(a => a.Genre)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Name).ToList());
        }
        
        #endregion

        #region Album Operations
        
        public async Task<Album> AddAlbumAsync(Album album)
        {
            if (album == null)
                throw new ArgumentNullException(nameof(album));
            
            if (!album.IsValid())
                throw new ArgumentException("Album data is invalid");
            
            try
            {
                // Validera att artisten finns
                var artist = _artistStore.GetById(album.ArtistId);
                if (artist == null)
                {
                    throw new InvalidOperationException($"Artist with ID {album.ArtistId} does not exist");
                }
                
                // Kontrollera för dublett (samma titel för samma artist)
                var existingAlbum = _albumStore.Find(a => 
                    a.ArtistId == album.ArtistId && 
                    a.Title.Equals(album.Title, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                
                if (existingAlbum != null)
                {
                    await _logService.LogWarningAsync("MusicLibraryService", "AddAlbum", "Album",
                        $"Duplicate album '{album.Title}' for artist ID {album.ArtistId}");
                    throw new InvalidOperationException($"Album '{album.Title}' already exists for this artist");
                }
                
                var addedAlbum = _albumStore.Add(album);
                
                await _logService.LogCrudAsync("MusicLibraryService", "Create", "Album", addedAlbum.Id,
                    $"Added album: {album.Title} for artist ID {album.ArtistId}");
                
                return addedAlbum;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("MusicLibraryService", "AddAlbum", "Album",
                    $"Failed to add album: {album.Title}", ex);
                throw;
            }
        }
        
        public List<Album> SearchAlbums(string searchTerm, int? artistId = null, int? year = null, Genre? genre = null)
        {
            var query = _albumStore.GetAll().AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a => 
                    a.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }
            
            if (artistId.HasValue && artistId.Value > 0)
            {
                query = query.Where(a => a.ArtistId == artistId.Value);
            }
            
            if (year.HasValue)
            {
                query = query.Where(a => a.ReleaseYear == year.Value);
            }
            
            if (genre.HasValue)
            {
                query = query.Where(a => a.Genre == genre.Value);
            }
            
            return query.OrderBy(a => a.Title).ToList();
        }
        
        public List<AlbumWithArtist> GetAlbumsWithArtists()
        {
            var albums = _albumStore.GetAll();
            var artists = _artistStore.GetAll();
            
            var result = from album in albums
                        join artist in artists on album.ArtistId equals artist.Id
                        select new AlbumWithArtist
                        {
                            Album = album,
                            Artist = artist
                        };
            
            return result.OrderBy(x => x.Artist.Name).ThenBy(x => x.Album.ReleaseYear).ToList();
        }
        
        public List<AlbumWithArtist> GetAlbumsByArtist(int artistId)
        {
            var artist = _artistStore.GetById(artistId);
            if (artist == null)
                return new List<AlbumWithArtist>();
            
            var albums = _albumStore.Find(a => a.ArtistId == artistId).ToList();
            
            return albums.Select(album => new AlbumWithArtist
            {
                Album = album,
                Artist = artist
            }).OrderBy(a => a.Album.ReleaseYear).ThenBy(a => a.Album.Title).ToList();
        }
        
        public List<Album> GetAllAlbums()
        {
            return _albumStore.GetAll().ToList();
        }
        
        #endregion

        #region Song Operations
        
        public async Task<Song> AddSongAsync(Song song)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song));
            
            if (string.IsNullOrWhiteSpace(song.Title))
                throw new ArgumentException("Song title cannot be empty");
            
            if (song.AlbumId <= 0)
                throw new ArgumentException("Album ID must be valid");
            
            try
            {
                // Validera att albumet finns
                var album = _albumStore.GetById(song.AlbumId);
                if (album == null)
                {
                    throw new InvalidOperationException($"Album with ID {song.AlbumId} does not exist");
                }
                
                var addedSong = _songStore.Add(song);
                
                await _logService.LogCrudAsync("MusicLibraryService", "Create", "Song", addedSong.Id,
                    $"Added song: {song.Title} to album ID {song.AlbumId}");
                
                return addedSong;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("MusicLibraryService", "AddSong", "Song",
                    $"Failed to add song: {song.Title}", ex);
                throw;
            }
        }
        
        public List<Song> GetSongsByAlbum(int albumId)
        {
            return _songStore
                .Find(s => s.AlbumId == albumId)
                .OrderBy(s => s.Title)
                .ToList();
        }
        
        public List<Song> GetAllSongs()
        {
            return _songStore.GetAll().ToList();
        }
        
        #endregion

        #region Private Methods
        
        private async Task LoadArtistsAsync()
        {
            var artists = await _jsonService.LoadFromFileAsync<Artist>("artists");
            if (artists != null)
            {
                foreach (var artist in artists)
                {
                    _artistStore.Add(artist);
                }
            }
        }
        
        private async Task LoadAlbumsAsync()
        {
            var albums = await _jsonService.LoadFromFileAsync<Album>("albums");
            if (albums != null)
            {
                foreach (var album in albums)
                {
                    _albumStore.Add(album);
                }
            }
        }
        
        private async Task LoadSongsAsync()
        {
            var songs = await _jsonService.LoadFromFileAsync<Song>("songs");
            if (songs != null)
            {
                foreach (var song in songs)
                {
                    _songStore.Add(song);
                }
            }
        }
        
        private async Task SaveArtistsAsync()
        {
            await _jsonService.SaveToFileAsync("artists", _artistStore.GetAll());
        }
        
        private async Task SaveAlbumsAsync()
        {
            await _jsonService.SaveToFileAsync("albums", _albumStore.GetAll());
        }
        
        private async Task SaveSongsAsync()
        {
            await _jsonService.SaveToFileAsync("songs", _songStore.GetAll());
        }
        
        #endregion
    }

    #region Supporting Classes
    
    public class AlbumWithArtist
    {
        public Album Album { get; set; } = null!;
        public Artist Artist { get; set; } = null!;
        
        public override string ToString()
        {
            return $"{Album.Title} av {Artist.Name} ({Album.ReleaseYear})";
        }
    }
    
    #endregion
}