using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MusicCollectionManager.Interfaces;

namespace MusicCollectionManager.Services
{
    /// <summary>
    /// A generic data store class that handles CRUD operations, searching, and internal ID logic.
    /// This class uses generics to work with any type of entity while maintaining type safety.
    /// </summary>
    /// <typeparam name="T">The type of entity this DataStore will manage. 
    /// This allows the same DataStore implementation to work with different entity types 
    /// (e.g., Album, Artist, Track) without code duplication.</typeparam>
    public class DataStore<T> where T : class, IEntity
    {
        // Internal list to store entities - using List<T> for efficient CRUD operations
        private readonly List<T> _items = new();
        
        // Counter for automatic ID generation - ensures unique IDs across all entities
        private int _nextId = 1;

        /// <summary>
        /// Adds a new entity to the DataStore with automatic ID generation.
        /// </summary>
        /// <param name="item">The entity to add. Must not be null.</param>
        /// <returns>The added entity with its assigned ID.</returns>
        /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
        public T Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Assign automatic ID and increment counter for next entity
            item.Id = _nextId++;
            _items.Add(item);
            
            return item;
        }

        /// <summary>
        /// Updates an existing entity in the DataStore.
        /// </summary>
        /// <param name="item">The entity with updated properties.</param>
        /// <returns>True if update was successful, false if entity was not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
        public bool Update(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Find existing item by ID
            var existingItem = GetById(item.Id);
            if (existingItem == null)
                return false;

            // Remove old item and add updated item
            _items.Remove(existingItem);
            _items.Add(item);
            
            return true;
        }

        /// <summary>
        /// Deletes an entity from the DataStore by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity to delete.</param>
        /// <returns>True if deletion was successful, false if entity was not found.</returns>
        public bool Delete(int id)
        {
            var item = GetById(id);
            if (item == null)
                return false;

            return _items.Remove(item);
        }

        /// <summary>
        /// Retrieves all entities from the DataStore.
        /// </summary>
        /// <returns>A read-only collection of all entities.</returns>
        public IReadOnlyList<T> GetAll()
        {
            // Return a copy to prevent external modification of internal list
            return _items.ToList().AsReadOnly();
        }

        /// <summary>
        /// Retrieves an entity by its ID.
        /// <param name="id">The ID of the entity to retrieve.</param>
        /// <returns>The entity if found, null otherwise.</returns>
        public T? GetById(int id)
        {
            // Using FirstOrDefault for efficient lookup
            return _items.FirstOrDefault(item => item.Id == id);
        }

        /// <summary>
        /// Searches for entities that match a specified predicate using LINQ.
        /// This method demonstrates the power of generics combined with LINQ expressions
        /// to create type-safe, flexible search functionality.
        /// <param name="predicate">A LINQ expression that defines the search criteria.
        /// Example: Find(item => item.Title.Contains("Rock"))</param>
        /// <returns>A collection of entities matching the predicate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            // Using LINQ Where with the provided predicate expression
            // The generic type T ensures compile-time type checking of the predicate
            return _items.AsQueryable().Where(predicate).ToList();
        }

        /// <summary>
        /// Searches for entities using a simple string-based search on a specified property.
        /// This is a convenience method for common search scenarios.
        /// </summary>
        /// <param name="propertySelector">Function to select the property to search on.</param>
        /// <param name="searchTerm">The term to search for.</param>
        /// <returns>A collection of entities matching the search term.</returns>
        public IEnumerable<T> Search(Func<T, string> propertySelector, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAll();

            return Find(item => propertySelector(item)
                .Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the total count of entities in the DataStore.
    
        public int Count => _items.Count;

        /// <summary>
        /// Clears all entities from the DataStore and resets the ID counter.
        /// Use with caution as this operation cannot be undone.
     
        public void Clear()
        {
            _items.Clear();
            _nextId = 1;
        }
    }


    
}