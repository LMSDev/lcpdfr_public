namespace LCPD_First_Response.Engine.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using global::LCPDFR.Networking;
    using global::LCPDFR.Networking.Server;
    using global::LCPDFR.Networking.User;

    /// <summary>
    /// Helper class to queue up received messages to handle them again later.
    /// Useful when server sends data about entities not yet created on client, so data can be applied later once they exist on client.
    /// </summary>
    class MessageCache
    {
        /// <summary>
        /// The default time out for queued messages.
        /// </summary>
        public const int DefaultTimeOut = 5000;

        /// <summary>
        /// The queue containing all messages.
        /// </summary>
        private List<MessageCacheItem> queue;

        /// <summary>
        /// The recent IDs.
        /// </summary>
        private Dictionary<int, DateTime> recentIds; 

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCache"/> class.
        /// </summary>
        public MessageCache()
        {
            this.queue = new List<MessageCacheItem>();
            this.recentIds = new Dictionary<int, DateTime>();
        }

        /// <summary>
        /// Adds a message to the queue.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void AddMessage(ReceivedUserMessage message)
        {
            this.AddMessage(message, DefaultTimeOut);
        }

        /// <summary>
        /// Adds a message to the queue.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="timeOut">
        /// The time out.
        /// </param>
        public void AddMessage(ReceivedUserMessage message, int timeOut)
        {
            // If added already, increment references.
            MessageCacheItem[] items = this.queue.Where(item => item.Message == message).ToArray();
            if (items.Length > 0)
            {
                Log.Debug("AddMessage: Message in queue already, incrementing references count", this);
                items.First().References++;
            }
            else
            {
                // Reset message.
                message.PositionInBytes = 0;

                this.queue.Add(new MessageCacheItem(message, timeOut));
                Log.Debug("AddMessage: In queue now", this);
            }
        }

        /// <summary>
        /// Adds a recently created ID.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        public void AddRecentlyCreatedID(int id)
        {
            this.recentIds.Add(id, DateTime.Now.AddMilliseconds(DefaultTimeOut));
        }

        /// <summary>
        /// Returns a value whether <paramref name="id"/> has been created on the network recently (so it might be created soon on our end).
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>A boolean indicating whether the ID has been created recently.</returns>
        public bool HasIDBeenCreatedRecently(int id)
        {
            return this.recentIds.Keys.Contains(id);
        }

        /// <summary>
        /// Returns a value whether <paramref name="message"/> is queued up already.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A boolean indicating whether the message is queued up already.</returns>
        public bool IsInQueue(ReceivedUserMessage message)
        {
            return this.queue.Any(item => item.Message == message);
        }

        /// <summary>
        /// Processes the queue and invokes messages again.
        /// </summary>
        public void Process()
        {
            // Get all timed out IDs.
            DateTime now = DateTime.Now;
            List<int> idsToRemove = new List<int>();
            foreach (KeyValuePair<int, DateTime> keyValuePair in this.recentIds)
            {
                // Add to removal list.
                if (now.CompareTo(keyValuePair.Value) > 0)
                {
                    Log.Debug(string.Format("Compared {0} against {1} - first is greater", now.ToLongTimeString(), keyValuePair.Value.ToLongTimeString()), this);
                    idsToRemove.Add(keyValuePair.Key);   
                }
            }

            // Remove them.
            foreach (int i in idsToRemove)
            {
                this.recentIds.Remove(i);
            }

            // Process messages.
            List<MessageCacheItem> itemsToBeRemoved = new List<MessageCacheItem>();
            foreach (MessageCacheItem cacheItem in this.queue)
            {
                // If timed out, discard.
                if (now.CompareTo(cacheItem.TimeOut) > 0)
                {
                    Log.Debug(string.Format("Compared {0} against {1} - first is greater", now.ToLongTimeString(), cacheItem.TimeOut.ToLongTimeString()), this);
                    itemsToBeRemoved.Add(cacheItem);
                    continue;
                }

                Log.Debug("About to invoke", this);

                // Invoke message.
                if (cacheItem.Message.Sender is NetworkServer)
                {
                    ServerUserMessageHandlerFunction function = (ServerUserMessageHandlerFunction)cacheItem.Message.HandlerFunction;
                    function.Invoke((NetworkServer)cacheItem.Message.Sender, cacheItem.Message);
                    Log.Debug("Invoked clientside", this);
                }
                else
                {
                    ClientUserMessageHandlerFunction function = (ClientUserMessageHandlerFunction)cacheItem.Message.HandlerFunction;
                    function.Invoke(cacheItem.Message.Sender, cacheItem.Message);
                    Log.Debug("Invoked serverside", this);
                }

                // Decrement count and check if we still got references (so it was just added again).
                cacheItem.References--;
                if (cacheItem.References <= 0)
                {
                    Log.Debug("Item has zero refs", this);
                    itemsToBeRemoved.Add(cacheItem);
                }
            }

            // Remove invoked messages (which haven't been added again) and timed out ones.
            foreach (MessageCacheItem messageCacheItem in itemsToBeRemoved)
            {
                this.queue.Remove(messageCacheItem);
            }
        }

        /// <summary>
        /// A message cache item.
        /// </summary>
        private class MessageCacheItem
        {
            /// <summary>
            /// The message.
            /// </summary>
            private ReceivedUserMessage message;

            /// <summary>
            /// The time out.
            /// </summary>
            private DateTime timeOut;

            /// <summary>
            /// Initializes a new instance of the <see cref="MessageCacheItem"/> class.
            /// </summary>
            /// <param name="message">
            /// The message.
            /// </param>
            /// <param name="timeOut">
            /// The time out in milliseconds.
            /// </param>
            public MessageCacheItem(ReceivedUserMessage message, int timeOut)
            {
                this.message = message;
                this.timeOut = DateTime.Now.AddMilliseconds(timeOut);
                this.References = 1;
            }

            /// <summary>
            /// Gets the message.
            /// </summary>
            public ReceivedUserMessage Message
            {
                get
                {
                    return this.message;
                }
            }

            /// <summary>
            /// Gets or sets the references to the item, i.e. how often is has been added - how often it has been invoked.
            /// </summary>
            public int References { get; set; }

            /// <summary>
            /// Gets the time out.
            /// </summary>
            public DateTime TimeOut
            {
                get
                {
                    return this.timeOut;
                }
            }
        }
    }
}