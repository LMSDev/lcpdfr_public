namespace LCPDFR.Networking.User
{
    using System.Collections.Generic;

    /// <summary>
    /// Class to keep track of user callbacks. Used by both, client and server.
    /// </summary>
    /// <typeparam name="T">The type of the callback.</typeparam>
    internal class UserFunctions<T>
    {
        /// <summary>
        /// The registered functions.
        /// </summary>
        private Dictionary<int, T> handlerFunctions;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserFunctions{T}"/> class.
        /// </summary>
        public UserFunctions()
        {
            this.handlerFunctions = new Dictionary<int, T>();
        }

        public void AddHandler(int messageID, T userMessageHandler)
        {
            this.handlerFunctions.Add(messageID, userMessageHandler);
        }

        public T GetHandlerFunction(int messageID)
        {
            return this.handlerFunctions[messageID];
        }

        public bool HasHandlerFunction(int messageID)
        {
            return this.handlerFunctions.ContainsKey(messageID);
        }
    }
}