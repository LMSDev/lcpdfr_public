namespace LCPDFR.Networking
{
    using System;
    using System.Collections.Generic;

    using Lidgren.Network;

    internal enum EMessageID : byte
    {
        Welcome = 1,
        Version = 2,
        UserPayload = 10,
    }

    /// <summary>
    /// The message handler class, which handles incoming messages by processing them and calling their assigned handler functions.
    /// Does not process messages automatically, but only when <see cref="MessageHandler.ProcessQueue"/> is called (to allow synchronous processing).
    /// </summary>
    internal class MessageHandler : ILoggable
    {
        private Dictionary<EMessageID, MessageHandlerFunction> handlerFunctions;
        private List<KeyValuePair<NetIncomingMessage, NetConnection>> queue;
        private List<NetIncomingMessage> recycleQueue;
        private object recycleQueueLock;

        /// <summary>
        /// The object to lock the queue.
        /// </summary>
        private object queueLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandler"/> class.
        /// </summary>
        public MessageHandler()
        {
            this.handlerFunctions = new Dictionary<EMessageID, MessageHandlerFunction>();
            this.queue = new List<KeyValuePair<NetIncomingMessage, NetConnection>>();
            this.recycleQueue = new List<NetIncomingMessage>();
            this.recycleQueueLock = new object();
            this.queueLock = new object();
        }

        internal delegate void MessageHandlerFunction(NetIncomingMessage message, NetConnection sender);

        /// <summary>
        /// Gets the component name.
        /// </summary>
        public string ComponentName
        {
            get { return "MessageHandler"; }
        }

        public void ProcessQueue()
        {
            //Log.Debug("ProcessQueue", this);
            List<KeyValuePair<NetIncomingMessage, NetConnection>> tempQueue = new List<KeyValuePair<NetIncomingMessage, NetConnection>>();

            lock (this.queueLock)
            {
                foreach (KeyValuePair<NetIncomingMessage, NetConnection> netConnection in this.queue)
                {
                    try
                    {
                        // First byte is message id
                        byte messageID = netConnection.Key.ReadByte();
                        Logging.Debug("Got message ID: " + ((EMessageID)messageID), this);

                        // Search the handle function assigned to the id and 
                        MessageHandlerFunction handlerFunction = this.GetHandler((EMessageID)messageID);
                        if (handlerFunction != null)
                        {
                            try
                            {
                                handlerFunction(netConnection.Key, netConnection.Value);
                            }
                            catch (Exception ex)
                            {
                                Logging.Error("ProcessQueue: Error while handling message ID " + messageID + " (" + ((EMessageID)messageID) + ")" + ". Discarding packet. " + ex.Message + ex.StackTrace, this);
                            }
                        }
                        else
                        {
                            Logging.Warning("HandleFromBytes: No handler found for ID: " + messageID + " (" + ((EMessageID)messageID) + ")", this);
                        }

                        tempQueue.Add(netConnection);
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("ProcessQueue: Error while determining handler of packet. Message ID broken? " + ex.Message + ex.StackTrace, this);   
                    }
                }

                // Delete old messages
                foreach (KeyValuePair<NetIncomingMessage, NetConnection> keyValuePair in tempQueue)
                {
                    this.queue.Remove(keyValuePair);
                }
            }

            // And make them able to be recycled
            //lock (recycleQueueLock)
            //{
            //    this.recycleQueue.AddRange(tempQueue);
            //}
        }

        internal void AddHandler(EMessageID messageID, MessageHandlerFunction handlerFunction)
        {
            this.handlerFunctions.Add(messageID, handlerFunction);
        }

        internal MessageHandlerFunction GetHandler(EMessageID messageID)
        {
            if (this.handlerFunctions.ContainsKey(messageID))
            {
                return this.handlerFunctions[messageID];
            }

            return null;
        }

        internal void RemoveHandler(EMessageID messageID)
        {
            this.handlerFunctions.Remove(messageID);
        }

        /// <summary>
        /// Returns all bytes of the message, expect the first one (message id)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal byte[] GetMessageByteData(NetIncomingMessage message)
        {
            return message.ReadBytes(message.LengthBytes - 1);
        }

        internal bool Handle(NetIncomingMessage message, NetConnection sender)
        {
            // Add to queue
            lock (this.queueLock)
            {
                this.queue.Add(new KeyValuePair<NetIncomingMessage, NetConnection>(message, sender));
            }

            // Logging.Debug("Handle: Added message", this);
            return true;
        }

        internal NetIncomingMessage[] GetMessagesToRecycle()
        {
            lock (this.recycleQueueLock)
            {
                NetIncomingMessage[] ret = this.recycleQueue.ToArray();
                this.recycleQueue.Clear();
                return ret;
            }
        }
    }
}