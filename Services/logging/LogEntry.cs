using System.Text.Json.Serialization;
using MusicCollectionManager.Interfaces;

namespace MusicCollectionManager.Services.Logging
{
    public class LogEntry : IEntity
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AdditionalData { get; set; }

        public LogEntry()
        {
            Timestamp = DateTime.Now;
        }

        public LogEntry(LogLevel level, string source, string action, string entityType, string message, int? entityId = null, string? additionalData = null)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Source = source;
            Action = action;
            EntityType = entityType;
            Message = message;
            EntityId = entityId;
            AdditionalData = additionalData;
        }

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Source}.{Action}: {Message}";
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LogLevel
    {
        Information = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}