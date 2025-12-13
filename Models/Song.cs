using MusicCollectionManager.Interfaces;

namespace MusicCollectionManager.Models
{
    internal class Song : IEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
    }
}
