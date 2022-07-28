namespace LCPDFR.Networking
{
    /// <summary>
    /// Exposes a function to retrieve the content of the class in bytes. Used for messages.
    /// </summary>
    public interface IData
    {
        /// <summary>
        /// Gets the byte representation of the message.
        /// </summary>
        /// <returns>The bytes.</returns>
        byte[] ToBytes();
    }
}