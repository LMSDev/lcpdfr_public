namespace LCPDFR.Networking.User
{
    using Lidgren.Network;

    /// <summary>
    /// Represents a message received from either a client or a server containing user data.
    /// </summary>
    public class ReceivedUserMessage
    {
        /// <summary>
        /// The internal net message.
        /// </summary>
        private readonly NetIncomingMessage incomingMessage;

        /// <summary>
        /// The position in the internal net message at start.
        /// </summary>
        private int startPosBits;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivedUserMessage"/> class.
        /// </summary>
        /// <param name="incomingMessage">The internal incoming message.</param>
        /// <param name="handlerFunction">The handler function for the message.</param>
        /// <param name="sender">The sender of the message.</param>
        internal ReceivedUserMessage(NetIncomingMessage incomingMessage, object handlerFunction, NetworkClient sender)
        {
            this.HandlerFunction = handlerFunction;
            this.Sender = sender;
            this.incomingMessage = incomingMessage;
            this.startPosBits = (int)this.incomingMessage.Position;
        }

        /// <summary>
        /// Gets the associated handler function.
        /// </summary>
        public object HandlerFunction { get; private set; }

        /// <summary>
        /// Gets the sender of the message.
        /// </summary>
        public NetworkClient Sender { get; private set; }

        /// <summary>
        /// Gets the length of the message in bits.
        /// </summary>
        public int LengthInBits
        {
            get
            {
                return this.incomingMessage.LengthBits - this.startPosBits;
            }
        }

        /// <summary>
        /// Gets the length of the message in bytes.
        /// </summary>
        public int LengthInBytes
        {
            get
            {
                return this.LengthInBits / 8;
            }
        }

        /// <summary>
        /// Gets or sets the position in the message stream in bits.
        /// </summary>
        public long PositionInBits
        {
            get
            {
                return this.incomingMessage.Position - this.startPosBits;
            }

            set
            {
                this.incomingMessage.Position = this.startPosBits + value;
            }
        }

        /// <summary>
        /// Gets or sets the position in the message stream in bytes.
        /// </summary>
        public long PositionInBytes
        {
            get
            {
                return this.PositionInBits / 8;
            }

            set
            {
                this.PositionInBits = value * 8;
            }
        }

        /// <summary>
        /// Gets the internal message.
        /// </summary>
        internal NetIncomingMessage InternalMessage
        {
            get
            {
                return this.incomingMessage;
            }
        }

        /// <summary>
        /// Reads a byte from the message.
        /// </summary>
        /// <returns>The byte</returns>
        public byte ReadByte()
        {
            return this.incomingMessage.ReadByte();
        }

        /// <summary>
        /// Reads multiple bytes from the message.
        /// </summary>
        /// <param name="length">Number of bytes to read.</param>
        /// <returns>The bytes.</returns>
        public byte[] ReadBytes(int length)
        {
            return this.incomingMessage.ReadBytes(length);
        }

        /// <summary>
        /// Reads a 16 bit signed integer from the message.
        /// </summary>
        /// <returns>The integer.</returns>
        public short ReadInt16()
        {
            return this.incomingMessage.ReadInt16();
        }

        /// <summary>
        /// Reads a 32 bit signed integer from the message.
        /// </summary>
        /// <returns>The integer.</returns>
        public int ReadInt32()
        {
            return this.incomingMessage.ReadInt32();
        }

        /// <summary>
        /// Reads a 64 bit signed integer from the message.
        /// </summary>
        /// <returns>The integer.</returns>
        public long ReadInt64()
        {
            return this.incomingMessage.ReadInt64();
        }

        /// <summary>
        /// Reads a floating point value from the message.
        /// </summary>
        /// <returns>The float.</returns>
        public float ReadFloat()
        {
            return this.incomingMessage.ReadFloat();
        }

        /// <summary>
        /// Reads a double precision value from the message.
        /// </summary>
        /// <returns>The double.</returns>
        public double ReadDouble()
        {
            return this.incomingMessage.ReadDouble();
        }

        /// <summary>
        /// Reads a string from the message.
        /// </summary>
        /// <returns>The string.</returns>
        public string ReadString()
        {
            return this.incomingMessage.ReadString();
        }
    }
}