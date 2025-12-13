namespace MusicCollectionManager.Interfaces
{
    /// <summary>
    /// Interface that defines the contract for entities stored in DataStore.
    /// This ensures all entities have an Id property, which is required for
    /// the DataStore's CRUD operations and ID management.
    /// </summary>
    public interface IEntity
    {
        int Id { get; set; }
    }
}