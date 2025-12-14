using MusicCollectionManager.Interfaces;

namespace MusicCollectionManager.Models
{
    internal class Album : IEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
    }
}
