namespace LCPDFR.Networking
{
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    internal class ServerMessageReceivedAttribute : Attribute
    {
        private readonly string identifier;

        private readonly int messageID;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerMessageReceivedAttribute"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier.
        /// </param>
        /// <param name="messageID">
        /// The message id.
        /// </param>
        public ServerMessageReceivedAttribute(string identifier, object messageID)
        {
            this.identifier = identifier;
            this.messageID = (int)messageID;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerMessageReceivedAttribute"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The identifier.
        /// </param>
        /// <param name="messageID">
        /// The message id.
        /// </param>
        public ServerMessageReceivedAttribute(string identifier, int messageID)
        {
            this.identifier = identifier;
            this.messageID = messageID;
        }

        public int MessageID
        {
            get
            {
                return this.messageID;
            }
        }

        public string Identifier
        {
            get
            {
                return this.identifier;
            }
        }
    }
}