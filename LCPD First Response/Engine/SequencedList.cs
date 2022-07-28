namespace LCPD_First_Response.Engine
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A list containing items of type <typeparamref name="T"/> that uses an internal index to return the Next/Previous item in the list.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    internal class SequencedList<T> : List<T>
    {
        /// <summary>
        /// The index.
        /// </summary>
        private int listIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequencedList{T}"/> class.
        /// </summary>
        public SequencedList()
        {
            this.listIndex = -1;
        }

        /// <summary>
        /// Returns whether there is another item available.
        /// </summary>
        /// <returns>True if there if yes, false otherwise.</returns>
        public bool IsNextItemAvailable()
        {
            if (this.listIndex == this.Count - 1)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the next item in the list.
        /// </summary>
        /// <returns>The next item.</returns>
        public T Next()
        {
            T item;

            if (this.IsNextItemAvailable())
            {
                this.listIndex++;
                item = this[this.listIndex]; 
            }
            else
            {
                this.Reset();
                this.listIndex++;
                item = this[this.listIndex];
            }

            return item;
        }

        /// <summary>
        /// Returns the previous item in the list.
        /// </summary>
        /// <returns>The previous item.</returns>
        public T Previous()
        {
            // If index is zero or below, set to max
            if (this.listIndex <= 0)
            {
                this.listIndex = this.Count - 1;
            }
            else
            {
                // If higher than zero, subtract by one
                this.listIndex--;
            }

            T item = this[this.listIndex];
            return item;
        }

        /// <summary>
        /// Resets the index.
        /// </summary>
        public void Reset()
        {
            this.listIndex = -1;
        }

        /// <summary>
        /// Resets and shuffles the entire list.
        /// </summary>
        public void Shuffle()
        {
            this.Reset();

            Random rng = new Random(Environment.TickCount);
            int n = this.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = this[k];
                this[k] = this[n];
                this[n] = value;
            }  
        }
    }
}