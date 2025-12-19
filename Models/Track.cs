using System.Text.Json.Serialization;
using MusicCollectionManager.Interfaces;

namespace MusicCollectionManager.Models
{
    /// <summary>
    /// Representerar en låt (Track) i systemet.
    /// Komposition/aggregation:
    /// - Ett Album "äger" (innehåller) flera Tracks.
    /// - Track tillhör ett Album via AlbumId (FK liknande relation i vår modell).
    /// </summary>
    public class Track : IEntity
    {
        // Inkapsling: privata fält + publika properties
        [JsonInclude] private int _id;
        [JsonInclude] private string _title = string.Empty;
        [JsonInclude] private int _albumId;
        [JsonInclude] private int _duration;     // i sekunder (enklast att summera)
        [JsonInclude] private int _trackNumber;

        public int Id
        {
            get => _id;
            set => _id = value;
        }

        public string Title
        {
            get => _title;
            set => _title = value?.Trim() ?? string.Empty;
        }

        public int AlbumId
        {
            get => _albumId;
            set => _albumId = value;
        }

        /// <summary>
        /// Speltid i sekunder.
        /// </summary>
        public int Duration
        {
            get => _duration;
            set => _duration = value < 0 ? 0 : value;
        }

        public int TrackNumber
        {
            get => _trackNumber;
            set => _trackNumber = value < 1 ? 1 : value;
        }

        public Track() { }

        public Track(int id, string title, int albumId, int duration, int trackNumber)
        {
            Id = id;
            Title = title;
            AlbumId = albumId;
            Duration = duration;
            TrackNumber = trackNumber;
        }

        public bool IsValid()
        {
            return Id > 0
                && !string.IsNullOrWhiteSpace(Title)
                && AlbumId > 0
                && Duration >= 0
                && TrackNumber > 0;
        }

        public override string ToString()
        {
            return $"{TrackNumber}. {Title} ({Duration}s)";
        }
    }
}