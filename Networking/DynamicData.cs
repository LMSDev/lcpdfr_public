namespace LCPDFR.Networking
{
    using Lidgren.Network;

    /// <summary>
    /// Helper class to easily write any kind of data over the net. Allows you to simply write any data to a stream rather than creating a predefined data object.
    /// </summary>
    public class DynamicData : IData
    {
        /// <summary>
        /// The internal outgoing message.
        /// </summary>
        private NetOutgoingMessage outgoingMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicData"/> class.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        public DynamicData(BaseNetPeer sender)
        {
            this.outgoingMessage = sender.GetInternalNetPeer().CreateMessage();
        }

        /// <summary>
        /// Gets the byte representation of the message.
        /// </summary>
        /// <returns>The bytes.</returns>
        public byte[] ToBytes()
        {
            return this.outgoingMessage.PeekDataBuffer();
        }

        public void Write(bool value)
        {
            this.outgoingMessage.Write(value);
        }

        public void Write(byte value)
        {
            this.outgoingMessage.Write(value);
        }

        public void Write(int value)
        {
            this.outgoingMessage.Write(value);
        }

        public void Write(string value)
        {
            this.outgoingMessage.Write(value);
        }

        public void Write(uint value)
        {
            this.outgoingMessage.Write(value);
        }

        public void Write(long value)
        {
            this.outgoingMessage.Write(value);
        }

        public void Write(float value)
        {
            this.outgoingMessage.Write(value);
        }

        public void Write(double value)
        {
            this.outgoingMessage.Write(value);
        }

        public void Write(byte[] value)
        {
            this.outgoingMessage.Write(value);
        }
    }
}