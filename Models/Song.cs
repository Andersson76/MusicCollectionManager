using MusicCollectionManager.Interfaces;

namespace MusicCollectionManager.Models
{
    /// <summary>
    /// Representerar en låt i systemet.
    /// </summary>
    public class Song : IEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public int AlbumId { get; set; } // Association till Album
        
        /// <summary>
        /// Returnerar en strängrepresentation av låten.
        /// </summary>
        public override string ToString()
        {
            var minutes = DurationSeconds / 60;
            var seconds = DurationSeconds % 60;
            return $"{Title} ({minutes}:{seconds:00})";
        }
    }
}