namespace LCPDFR.Networking.User
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Represents a user message that can be sent over the net.
    /// </summary>
    public class UserMessage
    {
        /// <summary>
        /// The message bytes.
        /// </summary>
        private List<byte> bytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessage"/> class. The message is empty and only contains the identifier and the message ID.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">
        /// The message id.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If identifier exceeds 255 characters, an exception is thrown.
        /// </exception>
        public UserMessage(string identifier, int messageID)
        {
            if (identifier.Length > 255)
            {
                throw new ArgumentException("Identifier must consist of 255 or less characters.");
            }

            this.bytes = new List<byte>();
            this.BuildMessageHeader(identifier, messageID);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessage"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">
        /// The message id.
        /// </param>
        /// <param name="data">
        /// The data that should be included in the message.
        /// </param>
        public UserMessage(string identifier, int messageID, IData data) 
            : this(identifier, messageID)
        {
            this.bytes.AddRange(data.ToBytes());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessage"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">
        /// The message id.
        /// </param>
        /// <param name="data">
        /// The data in raw bytes that should be included in the message.
        /// </param>
        public UserMessage(string identifier, int messageID, byte[] data) 
            : this(identifier, messageID)
        {
            this.bytes.AddRange(data);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessage"/> class. The message is empty and only contains the identifier and the message ID.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">
        /// The message id.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If identifier exceeds 255 characters, an exception is thrown.
        /// </exception>
        public UserMessage(string identifier, Enum messageID)
        {
            if (identifier.Length > 255)
            {
                throw new ArgumentException("Identifier must consist of 255 or less characters.");
            }

            this.bytes = new List<byte>();
            this.BuildMessageHeader(identifier, (int)(object)messageID);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessage"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">
        /// The message id.
        /// </param>
        /// <param name="data">
        /// The data that should be included in the message.
        /// </param>
        public UserMessage(string identifier, Enum messageID, IData data) 
            : this(identifier, messageID)
        {
            this.bytes.AddRange(data.ToBytes());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessage"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">
        /// The message id.
        /// </param>
        /// <param name="data">
        /// The data in raw bytes that should be included in the message.
        /// </param>
        public UserMessage(string identifier, Enum messageID, byte[] data)
            : this(identifier, messageID)
        {
            this.bytes.AddRange(data);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessage"/> class.
        /// </summary>
        /// <param name="message">
        /// The received message.
        /// </param>
        public UserMessage(ReceivedUserMessage message)
        {
            this.bytes = new List<byte>();
            this.bytes.AddRange(message.InternalMessage.PeekDataBuffer());
        }

        /// <summary>
        /// Gets the internal bytes buffer.
        /// </summary>
        /// <returns>The byte buffer.</returns>
        internal byte[] GetBytes()
        {
            return this.bytes.ToArray();
        }

        /// <summary>
        /// Creates the message header.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="messageID">The message ID.</param>
        private void BuildMessageHeader(string identifier, int messageID)
        {
            // Identifier length.
            this.bytes.Add((byte)identifier.Length);
            this.bytes.AddRange(Encoding.UTF8.GetBytes(identifier));
            this.bytes.AddRange(BitConverter.GetBytes(messageID));
        }
    }
}