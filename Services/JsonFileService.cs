using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MusicCollectionManager.Services
{
    /// <summary>
    /// Service for loading and saving collections of objects to JSON files.
    /// Handles automatic directory creation and comprehensive error handling.
    /// </summary>
    public class JsonFileService
    {
        // Default directory for storing data files
        private const string DefaultDataDirectory = "data";
        
        // JSON serialization options for consistent formatting
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of JsonFileService with default JSON options.
        /// </summary>
        public JsonFileService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true, // Makes JSON human-readable
                PropertyNameCaseInsensitive = true, // Case-insensitive deserialization
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Skip null properties
                Converters = { new JsonStringEnumConverter() } // Handle enums as strings
            };
        }

        /// <summary>
        /// Loads a collection of objects from a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize.</typeparam>
        /// <param name="fileName">Name of the JSON file (with or without extension).</param>
        /// <param name="subDirectory">Optional subdirectory within the data folder.</param>
        /// <returns>A list of deserialized objects, or empty list if file doesn't exist.</returns>
        /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
        public async Task<List<T>> LoadFromFileAsync<T>(string fileName, string? subDirectory = null)  // FIXED: Made subDirectory nullable
        {
            ValidateFileName(fileName);
            
            string filePath = GetFilePath(fileName, subDirectory);
            
            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}. Returning empty list.");
                    return new List<T>();
                }

                // Read and deserialize JSON
                string jsonContent = await File.ReadAllTextAsync(filePath);
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    Console.WriteLine($"File is empty: {filePath}. Returning empty list.");
                    return new List<T>();
                }

                var result = JsonSerializer.Deserialize<List<T>>(jsonContent, _jsonOptions);
                
                Console.WriteLine($"Successfully loaded {result?.Count ?? 0} items from {filePath}");
                return result ?? new List<T>();
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON deserialization error in {filePath}: {jsonEx.Message}");
                throw new InvalidOperationException($"Invalid JSON format in {filePath}. Please check the file content.", jsonEx);
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"IO error reading {filePath}: {ioEx.Message}");
                throw new IOException($"Could not read file {filePath}. It may be in use by another process.", ioEx);
            }
            catch (UnauthorizedAccessException authEx)
            {
                Console.WriteLine($"Access denied to {filePath}: {authEx.Message}");
                throw new UnauthorizedAccessException($"Access denied to file {filePath}. Check permissions.", authEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error loading {filePath}: {ex.Message}");
                throw new Exception($"Failed to load data from {filePath}.", ex);
            }
        }

        /// <summary>
        /// Synchronous version of LoadFromFileAsync for compatibility.
        /// </summary>
        public List<T> LoadFromFile<T>(string fileName, string? subDirectory = null)  // FIXED: Made subDirectory nullable
        {
            return LoadFromFileAsync<T>(fileName, subDirectory).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Saves a collection of objects to a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of objects to serialize.</typeparam>
        /// <param name="fileName">Name of the JSON file (with or without extension).</param>
        /// <param name="data">The collection of objects to save.</param>
        /// <param name="subDirectory">Optional subdirectory within the data folder.</param>
        /// <returns>True if save was successful, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown when fileName is null or empty, or data is null.</exception>
        public async Task<bool> SaveToFileAsync<T>(string fileName, IEnumerable<T>? data, string? subDirectory = null)  // FIXED: Made subDirectory nullable
        {
            ValidateFileName(fileName);
            
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data to save cannot be null.");

            string filePath = GetFilePath(fileName, subDirectory);
            
            try
            {
                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);  // FIXED: Made directory nullable
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Created directory: {directory}");
                }

                // Serialize data to JSON
                string jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
                
                // Write to file with atomic operation (write to temp file then rename)
                string tempFilePath = filePath + ".tmp";
                await File.WriteAllTextAsync(tempFilePath, jsonContent);
                
                // Replace original file with temp file
                if (File.Exists(filePath))
                    File.Delete(filePath);
                
                File.Move(tempFilePath, filePath);
                
                Console.WriteLine($"Successfully saved {GetItemCount(data)} items to {filePath}");
                return true;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON serialization error for {filePath}: {jsonEx.Message}");
                throw new InvalidOperationException($"Failed to serialize data to JSON for {filePath}.", jsonEx);
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"IO error writing to {filePath}: {ioEx.Message}");
                throw new IOException($"Could not write to file {filePath}. It may be in use or disk may be full.", ioEx);
            }
            catch (UnauthorizedAccessException authEx)
            {
                Console.WriteLine($"Access denied to {filePath}: {authEx.Message}");
                throw new UnauthorizedAccessException($"Access denied to write to {filePath}. Check permissions.", authEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error saving to {filePath}: {ex.Message}");
                throw new Exception($"Failed to save data to {filePath}.", ex);
            }
        }

        /// <summary>
        /// Synchronous version of SaveToFileAsync for compatibility.
        /// </summary>
        public bool SaveToFile<T>(string fileName, IEnumerable<T>? data, string? subDirectory = null)  // FIXED: Made subDirectory nullable and added ? to IEnumerable<T>
        {
            return SaveToFileAsync<T>(fileName, data, subDirectory).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the full file path for a given filename.
        /// </summary>
        private string GetFilePath(string fileName, string? subDirectory)  // FIXED: Made subDirectory nullable
        {
            // Ensure .json extension
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";

            // Build directory path
            string directory = DefaultDataDirectory;
            if (!string.IsNullOrEmpty(subDirectory))
                directory = Path.Combine(directory, subDirectory);

            // Combine to full path
            return Path.Combine(directory, fileName);
        }

        /// <summary>
        /// Validates that a filename is not null or empty.
        /// </summary>
        private void ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Filename cannot be null or empty.", nameof(fileName));
        }

        /// <summary>
        /// Gets the count of items in an IEnumerable (without enumerating all if possible).
        /// </summary>
        private int GetItemCount<T>(IEnumerable<T> data)  // This is fine - data won't be null here because we check above
        {
            if (data is ICollection<T> collection)
                return collection.Count;
            
            if (data is IReadOnlyCollection<T> readOnlyCollection)
                return readOnlyCollection.Count;
            
            // Fallback: count by enumeration
            int count = 0;
            foreach (var _ in data) count++;
            return count;
        }

        /// <summary>
        /// Checks if a JSON file exists.
        /// </summary>
        public bool FileExists(string fileName, string? subDirectory = null)  // FIXED: Made subDirectory nullable
        {
            string filePath = GetFilePath(fileName, subDirectory);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Deletes a JSON file if it exists.
        /// </summary>
        public bool DeleteFile(string fileName, string? subDirectory = null)  // FIXED: Made subDirectory nullable
        {
            string filePath = GetFilePath(fileName, subDirectory);
            
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"Deleted file: {filePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                return false;
            }
        }
    }
}