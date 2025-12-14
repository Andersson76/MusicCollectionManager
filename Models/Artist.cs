using MusicCollectionManager.Interfaces;

namespace MusicCollectionManager.Models
{
    internal class Artist : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
