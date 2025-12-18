using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusicCollectionManager.Services;
using MusicCollectionManager.Services.Json;

namespace MusicCollectionManager.Tests
{
    /// <summary>
    /// Test class for JsonFileService with Artist model.
    /// </summary>
    public class JsonServiceTester
    {
        private readonly JsonFileService _jsonService = new JsonFileService();
        
        // Artist model for testing
        public class Artist
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Genre { get; set; }
            public DateTime Formed { get; set; }
            public List<string> Members { get; set; } = new();
        }

        /// <summary>
        /// Test loading and saving artists.
        /// </summary>
        public async Task RunTests()
        {
            Console.WriteLine("=== Testing JsonFileService ===");
            
            // Create sample artists
            var artists = new List<Artist>
            {
                new Artist 
                { 
                    Id = 1, 
                    Name = "The Beatles", 
                    Genre = "Rock",
                    Formed = new DateTime(1960, 1, 1),
                    Members = new List<string> { "John Lennon", "Paul McCartney", "George Harrison", "Ringo Starr" }
                },
                new Artist 
                { 
                    Id = 2, 
                    Name = "Michael Jackson", 
                    Genre = "Pop",
                    Formed = new DateTime(1964, 1, 1),
                    Members = new List<string> { "Michael Jackson" }
                },
                new Artist 
                { 
                    Id = 3, 
                    Name = "Queen", 
                    Genre = "Rock",
                    Formed = new DateTime(1970, 1, 1),
                    Members = new List<string> { "Freddie Mercury", "Brian May", "Roger Taylor", "John Deacon" }
                }
            };

            try
            {
                // Test 1: Save artists
                Console.WriteLine("\n1. Saving artists to Artist.json...");
                bool saveResult = await _jsonService.SaveToFileAsync("Artist", artists);
                Console.WriteLine($"Save successful: {saveResult}");

                // Test 2: Load artists
                Console.WriteLine("\n2. Loading artists from Artist.json...");
                var loadedArtists = await _jsonService.LoadFromFileAsync<Artist>("Artist");
                Console.WriteLine($"Loaded {loadedArtists.Count} artists:");
                
                foreach (var artist in loadedArtists)
                {
                    Console.WriteLine($"  - {artist.Name} ({artist.Genre})");
                }

                // Test 3: Check file exists
                Console.WriteLine("\n3. Checking file existence...");
                bool exists = _jsonService.FileExists("Artist");
                Console.WriteLine($"Artist.json exists: {exists}");

                // Test 4: Add new artist and save again
                Console.WriteLine("\n4. Adding new artist and saving...");
                artists.Add(new Artist 
                { 
                    Id = 4, 
                    Name = "ABBA", 
                    Genre = "Pop",
                    Formed = new DateTime(1972, 1, 1),
                    Members = new List<string> { "Agnetha Fältskog", "Björn Ulvaeus", "Benny Andersson", "Anni-Frid Lyngstad" }
                });
                
                await _jsonService.SaveToFileAsync("Artist", artists);
                Console.WriteLine("Artists updated successfully.");

                // Test 5: Load again to verify
                Console.WriteLine("\n5. Verifying updated file...");
                var updatedArtists = await _jsonService.LoadFromFileAsync<Artist>("Artist");
                Console.WriteLine($"Now have {updatedArtists.Count} artists in file.");

                // Test 6: Test with subdirectory
                Console.WriteLine("\n6. Testing with subdirectory...");
                await _jsonService.SaveToFileAsync("Artist", artists, "backup");
                var backupArtists = await _jsonService.LoadFromFileAsync<Artist>("Artist", "backup");
                Console.WriteLine($"Loaded {backupArtists.Count} artists from backup folder.");

                Console.WriteLine("\n=== All tests completed successfully! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n!!! Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Test error scenarios.
        /// </summary>
        public void TestErrorScenarios()
        {
            Console.WriteLine("\n=== Testing Error Scenarios ===");
            
            try
            {
                // Test 1: Invalid filename
                Console.WriteLine("\n1. Testing with empty filename...");
                _jsonService.SaveToFile("", new List<Artist>());
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Expected error: {ex.Message}");
            }

            try
            {
                // Test 2: Null data
                Console.WriteLine("\n2. Testing with null data...");
                _jsonService.SaveToFile("Test", (List<Artist>?)null);
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine($"Expected error: {ex.Message}");
            }

            // Test 3: Non-existent file (should return empty list)
            Console.WriteLine("\n3. Loading non-existent file...");
            var result = _jsonService.LoadFromFile<Artist>("NonExistentFile");
            Console.WriteLine($"Result count: {result.Count} (should be 0)");

            Console.WriteLine("\n=== Error scenario tests completed ===");
        }
    }
}