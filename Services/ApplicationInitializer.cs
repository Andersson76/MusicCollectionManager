using System;
using System.Threading.Tasks;
using MusicCollectionManager.Models;
using MusicCollectionManager.Services.Json;
using MusicCollectionManager.Services.Logging;

namespace MusicCollectionManager.Services
{
    /// <summary>
    /// Handles application initialization, service composition, and error handling.
    /// Centralizes startup logic away from Program.cs.
    /// </summary>
    public class ApplicationInitializer
    {
        /// <summary>
        /// Result of application initialization containing all required services.
        /// </summary>
        public class InitializationResult
        {
            public MusicLibraryService MusicLibrary { get; set; } = null!;
            public LogService LogService { get; set; } = null!;
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public Exception? Exception { get; set; }
        }

        /// <summary>
        /// Initializes all application services and returns them in a result object.
        /// </summary>
        public async Task<InitializationResult> InitializeAsync()
        {
            var result = new InitializationResult();
            
            try
            {
                Console.WriteLine("🔄 Initializing application services...\n");
                
                // Step 1: Create core services
                var jsonService = new JsonFileService();
                var logService = new LogService(jsonService);
                
                // Step 2: Initialize logging first
                await logService.InitializeAsync();
                await logService.LogInformationAsync("ApplicationInitializer", "Startup", "Application", 
                    "Starting application initialization");
                
                // Step 3: Create data stores
                var artistStore = new DataStore<Artist>();
                var albumStore = new DataStore<Album>();
                var trackStore = new DataStore<Song>();
                
                // Step 4: Create main library service
                var musicLibrary = new MusicLibraryService(
                    artistStore, 
                    albumStore, 
                    trackStore, 
                    jsonService, 
                    logService);
                
                // Step 5: Load or create data
                await LoadOrCreateDataAsync(musicLibrary, logService);
                
                // Step 6: Return successful result
                result.MusicLibrary = musicLibrary;
                result.LogService = logService;
                result.Success = true;
                
                await logService.LogInformationAsync("ApplicationInitializer", "Startup", "Application", 
                    "Application initialized successfully");
                
                Console.WriteLine("✅ Application initialized successfully!\n");
                await Task.Delay(500); // Brief pause
                
                return result;
            }
            catch (Exception ex)
            {
                // Create minimal services for error logging if possible
                try
                {
                    var jsonService = new JsonFileService();
                    var logService = new LogService(jsonService);
                    await logService.InitializeAsync();
                    await logService.LogErrorAsync("ApplicationInitializer", "Startup", "Application", 
                        "Failed to initialize application", ex);
                }
                catch
                {
                    // If even error logging fails, write to console
                    Console.WriteLine($"❌ Critical initialization error: {ex.Message}");
                }
                
                // Return failure result
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
                
                return result;
            }
        }

        /// <summary>
        /// Loads existing data or creates sample data if none exists.
        /// </summary>
        private async Task LoadOrCreateDataAsync(MusicLibraryService musicLibrary, LogService logService)
        {
            try
            {
                Console.WriteLine("📂 Loading library data...");
                
                // Try to load existing data
                await musicLibrary.InitializeAsync();
                
                // Check if we have data
                var artists = musicLibrary.GetAllArtists();
                var albums = musicLibrary.GetAllAlbums();
                
                if (!artists.Any() && !albums.Any())
                {
                    Console.WriteLine("📝 No existing data found. Creating sample data...");
                    await logService.LogInformationAsync("ApplicationInitializer", "Initialize", "Data", 
                        "No data found, creating sample data");
                    
                    await CreateSampleDataAsync(musicLibrary, logService);
                    
                    Console.WriteLine($"✅ Created {musicLibrary.GetAllArtists().Count} artists and {musicLibrary.GetAllAlbums().Count} albums");
                }
                else
                {
                    Console.WriteLine($"✅ Loaded {artists.Count} artists and {albums.Count} albums");
                    await logService.LogInformationAsync("ApplicationInitializer", "Initialize", "Data", 
                        $"Loaded {artists.Count} artists and {albums.Count} albums from storage");
                }
            }
            catch (Exception ex)
            {
                await logService.LogErrorAsync("ApplicationInitializer", "Initialize", "Data", 
                    "Failed to load or create data", ex);
                throw new InvalidOperationException("Failed to initialize library data", ex);
            }
        }

        /// <summary>
        /// Creates sample data for new users.
        /// </summary>
private async Task CreateSampleDataAsync(MusicLibraryService musicLibrary, LogService logService)
{
    try
    {
        // Exempelartister
        var sampleArtists = new[]
        {
            new Artist { Name = "Kent", Country = "Sverige", Genre = Genre.Rock },
            new Artist { Name = "Håkan Hellström", Country = "Sverige", Genre = Genre.Pop },
            new Artist { Name = "Veronica Maggio", Country = "Sverige", Genre = Genre.Pop },
            new Artist { Name = "ABBA", Country = "Sverige", Genre = Genre.Pop },
            new Artist { Name = "Meshuggah", Country = "Sverige", Genre = Genre.Metal }
        };

        foreach (var artist in sampleArtists)
        {
            await musicLibrary.AddArtistAsync(artist);
        }

        // Exempelalbum
        var sampleAlbums = new[]
        {
            new Album { Title = "Vapen & ammunition", ArtistId = 1, ReleaseYear = 2002, Genre = Genre.Rock },
            new Album { Title = "Du & jag döden", ArtistId = 1, ReleaseYear = 2005, Genre = Genre.Rock },
            new Album { Title = "Känn ingen sorg för mig Göteborg", ArtistId = 2, ReleaseYear = 2000, Genre = Genre.Pop },
            new Album { Title = "Och vinnaren är...", ArtistId = 3, ReleaseYear = 2008, Genre = Genre.Pop },
            new Album { Title = "Arrival", ArtistId = 4, ReleaseYear = 1976, Genre = Genre.Pop },
            new Album { Title = "Destroy Erase Improve", ArtistId = 5, ReleaseYear = 1995, Genre = Genre.Metal }
        };

        foreach (var album in sampleAlbums)
        {
            // Sätt betyg för vissa album
            if (album.Title.Contains("Vapen"))
                album.UpdateRating(5);
            else if (album.Title.Contains("Arrival"))
                album.UpdateRating(4);
            
            await musicLibrary.AddAlbumAsync(album);
        }

        await logService.LogInformationAsync("ApplicationInitializer", "SampleData", "Data", 
            $"Skapade {sampleArtists.Length} exempelartister och {sampleAlbums.Length} exempelalbum");
    }
    catch (Exception ex)
    {
        await logService.LogErrorAsync("ApplicationInitializer", "SampleData", "Data", 
            "Kunde inte skapa exempeldata", ex);
        throw;
    }
}
        /// <summary>
        /// Shuts down all services gracefully.
        /// </summary>
        public async Task ShutdownAsync(LogService? logService, MusicLibraryService? musicLibrary)
        {
            try
            {
                Console.WriteLine("\n🔄 Shutting down application...");
                
                if (musicLibrary != null)
                {
                    Console.WriteLine("💾 Saving data...");
                    await musicLibrary.SaveAllDataAsync();
                }
                
                if (logService != null)
                {
                    await logService.LogInformationAsync("ApplicationInitializer", "Shutdown", "Application", 
                        "Application shutting down");
                    await logService.ShutdownAsync();
                }
                
                Console.WriteLine("✅ Application shut down gracefully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error during shutdown: {ex.Message}");
            }
        }
    }
}