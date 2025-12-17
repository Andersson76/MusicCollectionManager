using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusicCollectionManager.Services.Json;

namespace MusicCollectionManager.Services.Logging
{
    public class LogService
    {
        private const string LogFileName = "application_log.json";
        private const int MaxLogEntries = 1000;
        private readonly JsonFileService _jsonService;
        private readonly List<LogEntry> _currentLogs;
        private int _nextId = 1;

        public event Action<LogEntry>? OnLogEntryAdded;

        public LogService(JsonFileService jsonService)
        {
            _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
            _currentLogs = new List<LogEntry>();
        }

        public async Task InitializeAsync()
        {
            var existingLogs = await _jsonService.LoadFromFileAsync<LogEntry>(LogFileName, "logs");
            if (existingLogs != null && existingLogs.Any())
            {
                _currentLogs.AddRange(existingLogs);
                _nextId = _currentLogs.Max(l => l.Id) + 1;
            }

            await LogAsync(LogLevel.Information, "System", "Startup", "Application", 
                "Log service initialized");
        }

        // ADD THIS METHOD
        public async Task LogInformationAsync(string source, string action, string entityType, 
                                             string message, int? entityId = null, string? additionalData = null)
        {
            await LogAsync(LogLevel.Information, source, action, entityType, message, entityId, additionalData);
        }

        public async Task LogCrudAsync(string source, string action, string entityType, 
                                      int? entityId = null, string? additionalData = null)
        {
            string message = $"{action} operation on {entityType}" + 
                           (entityId.HasValue ? $" ID {entityId}" : "");
            await LogAsync(LogLevel.Information, source, action, entityType, message, entityId, additionalData);
        }

        public async Task LogErrorAsync(string source, string action, string entityType, 
                                       string message, Exception? exception = null, int? entityId = null)
        {
            string? additionalData = exception != null ? 
                $"Exception: {exception.GetType().Name}, Message: {exception.Message}" : null;
            await LogAsync(LogLevel.Error, source, action, entityType, message, entityId, additionalData);
        }

        // ADD THESE METHODS FOR COMPLETENESS
        public async Task LogWarningAsync(string source, string action, string entityType, 
                                         string message, int? entityId = null, string? additionalData = null)
        {
            await LogAsync(LogLevel.Warning, source, action, entityType, message, entityId, additionalData);
        }

        public async Task LogCriticalAsync(string source, string action, string entityType, 
                                          string message, int? entityId = null, string? additionalData = null)
        {
            await LogAsync(LogLevel.Critical, source, action, entityType, message, entityId, additionalData);
        }

        private async Task LogAsync(LogLevel level, string source, string action, string entityType, 
                                   string message, int? entityId = null, string? additionalData = null)
        {
            var logEntry = new LogEntry(level, source, action, entityType, message, entityId, additionalData)
            {
                Id = _nextId++
            };

            _currentLogs.Add(logEntry);

            if (_currentLogs.Count > MaxLogEntries)
            {
                _currentLogs.RemoveRange(0, _currentLogs.Count - MaxLogEntries);
            }

            await SaveLogsAsync();
            OnLogEntryAdded?.Invoke(logEntry);
            Console.WriteLine(logEntry.ToString());
        }

        private async Task SaveLogsAsync()
        {
            await _jsonService.SaveToFileAsync(LogFileName, _currentLogs, "logs");
        }

        public List<LogEntry> GetAllLogs()
        {
            return _currentLogs.OrderByDescending(l => l.Timestamp).ToList();
        }

        public async Task ShutdownAsync()
        {
            await LogAsync(LogLevel.Information, "System", "Shutdown", "Application", 
                "Application is shutting down");
            await SaveLogsAsync();
        }
    }
}