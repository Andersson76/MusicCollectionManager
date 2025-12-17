using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MusicCollectionManager.Interfaces;

namespace MusicCollectionManager.Services
{
    /// <summary>
    /// En generisk klass som hanterar CRUD-operationer, sökning och intern ID-logik.
    /// Begränsad till typer som implementerar IEntity.
    /// </summary>
    /// <typeparam name="T">Typen av entitet som ska hanteras. Måste implementera IEntity.</typeparam>
    public class DataStore<T> where T : class, IEntity
    {
        private readonly List<T> _items = new();
        private int _nextId = 1;

        public int Count => _items.Count;

        public T Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            item.Id = _nextId++;
            _items.Add(item);
            
            return item;
        }

        public bool Update(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var existingItem = GetById(item.Id);
            if (existingItem == null)
                return false;

            _items.Remove(existingItem);
            _items.Add(item);
            
            return true;
        }

        public bool Delete(int id)
        {
            var item = GetById(id);
            if (item == null)
                return false;

            return _items.Remove(item);
        }

        public IReadOnlyList<T> GetAll()
        {
            return _items.ToList().AsReadOnly();
        }

        public T? GetById(int id)
        {
            return _items.FirstOrDefault(item => item.Id == id);
        }

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return _items.AsQueryable().Where(predicate).ToList();
        }

        public IEnumerable<T> Search(Func<T, string> propertySelector, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAll();

            return Find(item => propertySelector(item)
                .Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        public void Clear()
        {
            _items.Clear();
            _nextId = 1;
        }
    }
}