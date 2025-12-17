using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MusicCollectionManager.Services.Json
{
    public class JsonFileService
    {
        private const string DefaultDataDirectory = "data";
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonFileService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() },
                IncludeFields = true // Important for your private fields
            };
        }

        public async Task<List<T>> LoadFromFileAsync<T>(string fileName, string? subDirectory = null)
        {
            ValidateFileName(fileName);
            
            string filePath = GetFilePath(fileName, subDirectory);
            
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}. Returning empty list.");
                    return new List<T>();
                }

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
                throw new InvalidOperationException($"Invalid JSON format in {filePath}.", jsonEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading {filePath}: {ex.Message}");
                throw new Exception($"Failed to load data from {filePath}.", ex);
            }
        }

        public async Task<bool> SaveToFileAsync<T>(string fileName, IEnumerable<T>? data, string? subDirectory = null)
        {
            ValidateFileName(fileName);
            
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string filePath = GetFilePath(fileName, subDirectory);
            
            try
            {
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Created directory: {directory}");
                }

                string jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
                
                string tempFilePath = filePath + ".tmp";
                await File.WriteAllTextAsync(tempFilePath, jsonContent);
                
                if (File.Exists(filePath))
                    File.Delete(filePath);
                
                File.Move(tempFilePath, filePath);
                
                Console.WriteLine($"Successfully saved {GetItemCount(data)} items to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to {filePath}: {ex.Message}");
                throw new Exception($"Failed to save data to {filePath}.", ex);
            }
        }

        public List<T> LoadFromFile<T>(string fileName, string? subDirectory = null)
        {
            return LoadFromFileAsync<T>(fileName, subDirectory).GetAwaiter().GetResult();
        }

        public bool SaveToFile<T>(string fileName, IEnumerable<T>? data, string? subDirectory = null)
        {
            return SaveToFileAsync<T>(fileName, data, subDirectory).GetAwaiter().GetResult();
        }

        private string GetFilePath(string fileName, string? subDirectory)
        {
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";

            string directory = DefaultDataDirectory;
            if (!string.IsNullOrEmpty(subDirectory))
                directory = Path.Combine(directory, subDirectory);

            return Path.Combine(directory, fileName);
        }

        private void ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Filename cannot be null or empty.", nameof(fileName));
        }

        private int GetItemCount<T>(IEnumerable<T> data)
        {
            if (data is ICollection<T> collection)
                return collection.Count;
            
            if (data is IReadOnlyCollection<T> readOnlyCollection)
                return readOnlyCollection.Count;
            
            int count = 0;
            foreach (var _ in data) count++;
            return count;
        }
    }
}