namespace LCPD_First_Response.Engine
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Pool class, that works like a List
    /// </summary>
    /// <typeparam name="T">The type of the items in this pool</typeparam>
    internal class Pool<T> : IEnumerable
    {
        /// <summary>
        /// The entities in this pool
        /// </summary>
        private List<T> entities;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool{T}"/> class.
        /// </summary>
        public Pool()
        {
            this.entities = new List<T>();
        }

        /// <summary>
        /// Gets the number of entities in this pool
        /// </summary>
        public int Count
        {
            get
            {
                return this.entities.Count;
            }
        }

        /// <summary>
        /// Gets the pool element at <paramref name="index"/>
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>Type instance</returns>
        public T this[int index]
        {
            get
            {
                return this.entities[index];
            }
        }

        /// <summary>
        /// Adds the given entity to the pool
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        public void Add(T entity)
        {
            this.entities.Add(entity);
        }

        /// <summary>
        /// Returns all entities in this pool
        /// </summary>
        /// <returns>Entities array</returns>
        public T[] GetAll()
        {
            return this.entities.ToArray();
        }

        /// <summary>
        /// Removes the given entity from the pool
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        public void Remove(T entity)
        {
            this.entities.Remove(entity);
        }

        /// <summary>
        /// Returns an enumerator to iterate through this pool
        /// </summary>
        /// <returns>The enumerator</returns>
        public IEnumerator GetEnumerator()
        {
            return this.entities.GetEnumerator();
        }
    }
}
