namespace LCPDFR.Networking
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;

    using LCPDFR.Networking.User;

    using Lidgren.Network;

    /// <summary>
    /// The base class for all network peers.
    /// </summary>
    public abstract class BaseNetPeer : ILoggable
    {
        /// <summary>
        /// The unique app identifier (required by Lidgren).
        /// </summary>
        private readonly string appIdentifier;

        /// <summary>
        /// The message handler for incoming messages.
        /// </summary>
        private readonly MessageHandler messageHandler;

        /// <summary>
        /// The AES key.
        /// </summary>
        private byte[] aesKey;

        /// <summary>
        /// Functions registered for user payload.
        /// </summary>
        private Dictionary<string, UserFunctions<object>> userHandlerFunctions;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNetPeer"/> class.
        /// </summary>
        /// <param name="appIdentifier">
        /// The app identifier.
        /// </param>
        protected BaseNetPeer(string appIdentifier)
        {
            this.appIdentifier = appIdentifier;
            this.messageHandler = new MessageHandler();

            this.userHandlerFunctions = new Dictionary<string, UserFunctions<object>>();

            // Default user data handler.
            this.MessageHandler.AddHandler(EMessageID.UserPayload, this.UserPayloadHandlerFunction);
            this.aesKey = new byte[16] { 0xCC, 0x02, 0xFE, 0xFF, 0x7A, 0x76, 0x55, 0x67, 0xBD, 0x10, 0xDD, 0xE1, 0x2F, 0x11, 0x45, 0xA7 };
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public string ComponentName
        {
            get
            {
                return "BaseNetPeer";
            }
        }

        /// <summary>
        /// Gets the message handler.
        /// </summary>
        internal MessageHandler MessageHandler
        {
            get
            {
                return this.messageHandler;
            }
        }

        /// <summary>
        /// Gets the app identifier.
        /// </summary>
        protected string AppIdentifier
        {
            get
            {
                return this.appIdentifier;
            }
        }

        /// <summary>
        /// Closes the connection and sends <paramref name="goodbyeMessage"/>.
        /// Waits one second before returning to allow proper termination.
        /// </summary>
        /// <param name="goodbyeMessage">The message that is sent to all connected clients.</param>
        public abstract void Close(string goodbyeMessage);

        /// <summary>
        /// Sends a message to all connected clients and/or the server.
        /// </summary>
        /// <param name="userMessage">The message.</param>
        public abstract void Send(UserMessage userMessage);

        /// <summary>
        /// Creates and sends a new message.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">The message ID.</param>
        public void Send(string identifier, Enum messageID)
        {
            // Create instance and ask superclass to send it.
            UserMessage userMessage = new UserMessage(identifier, messageID);
            this.Send(userMessage);
        }

        /// <summary>
        /// Creates and sends a new message.
        /// </summary>
        /// <param name="identifier">
        /// The message identifier. This can be used to easily separate communication and allow different enums (with the same values).
        /// For instance, an identifier could be "Vehicle" and another could be "Ped" with their respective enums.
        /// Limited to 255 chars.
        /// </param>
        /// <param name="messageID">The message ID.</param>
        /// <param name="data">The data.</param>
        public void Send(string identifier, Enum messageID, IData data)
        {
            // Create instance and ask superclass to send it.
            UserMessage userMessage = new UserMessage(identifier, messageID, data);
            this.Send(userMessage);
        }

        /// <summary>
        /// Gets the internal <see cref="NetPeer"/> instance.
        /// </summary>
        /// <returns>The instance.</returns>
        internal abstract NetPeer GetInternalNetPeer();

        /// <summary>
        /// Called when an incoming data packet is containing user payload. Place invocation logic in overwritten function.
        /// </summary>
        /// <param name="function">The function to invoke.</param>
        /// <param name="message">The original message.</param>
        /// <param name="sender">The sender. Recommended to convert to <see cref="NetworkClient"/> before invoking user function.</param>
        internal abstract void InvokeUserPayloadHandlerFunction(object function, NetIncomingMessage message, NetConnection sender);

        /// <summary>
        /// Creates a new empty <see cref="NetOutgoingMessage"/> which only contains the message ID.
        /// </summary>
        /// <param name="messageID">The message ID.</param>
        /// <returns>The message.</returns>
        internal NetOutgoingMessage BuildMessage(EMessageID messageID)
        {
            NetOutgoingMessage outgoingMessage = this.GetInternalNetPeer().CreateMessage();
            outgoingMessage.Write((byte)messageID);
            return outgoingMessage;
        }

        internal NetOutgoingMessage EncryptMessage(NetOutgoingMessage message)
        {
            // Encrypts the message by XORing the static key with a random key and using that as the AES key.
            int realSize = message.LengthBits;

            // Generate random key.
            Random random = new Random(Environment.TickCount);
            byte[] randomKey = new byte[16];
            byte[] finalKey = new byte[16];
            random.NextBytes(randomKey);

            // XOR keys with each other.
            for (int i = 0; i < this.aesKey.Length; i++)
            {
                finalKey[i] = (byte)(this.aesKey[i] ^ randomKey[i]);
            }

            NetAESEncryption aesEncryption = new NetAESEncryption(this.GetInternalNetPeer(), finalKey, 0, finalKey.Length);
            if (message.Encrypt(aesEncryption))
            {
                // Set to end of buffer before appending key to avoid overwriting data.
                //message.LengthBits = message.PeekDataBuffer().Length * 8;
                message.Write(randomKey);
                aesEncryption = new NetAESEncryption(this.GetInternalNetPeer(), this.aesKey, 0, this.aesKey.Length);
                if (message.Encrypt(aesEncryption))
                {
                    return message;
                }
            }

            return null;
        }

        internal NetIncomingMessage DecryptMessage(NetIncomingMessage message)
        {
            NetAESEncryption aes = new NetAESEncryption(this.GetInternalNetPeer(), this.aesKey, 0, this.aesKey.Length);
            message.Decrypt(aes);

            byte[] finalKey = new byte[16];
            long oldPosition = message.Position;

            // Get random key.
            int length = message.LengthBytes;
            message.Position = (length - 16) * 8;
            byte[] randomKey = message.ReadBytes(16);

            // XOR keys with each other.
            for (int i = 0; i < this.aesKey.Length; i++)
            {
                finalKey[i] = (byte)(randomKey[i] ^ this.aesKey[i]);
            }

            message.Position = oldPosition;

            SymmetricAlgorithm  m_algorithm = new AesCryptoServiceProvider();

            int len = m_algorithm.Key.Length;
            var key = new byte[len];
            for (int i = 0; i < len; i++)
                key[i] = finalKey[0 + (i % finalKey.Length)];
            m_algorithm.Key = key;
            len = m_algorithm.IV.Length;
            key = new byte[len];
            for (int i = 0; i < len; i++)
                key[len - 1 - i] = finalKey[0 + (i % finalKey.Length)];
            m_algorithm.IV = key;


            int unEncLenBits = (int)message.ReadUInt32();
            var ms = new MemoryStream(message.Data, 4, message.LengthBytes - 4 - 16);
            var cs = new CryptoStream(ms, m_algorithm.CreateDecryptor(), CryptoStreamMode.Read);
            var result = new byte[unEncLenBits];
            cs.Read(result, 0, unEncLenBits / 8);
            cs.Close();

            // Set data in message.
            var dataField = message.GetType().GetField("m_data", BindingFlags.Instance | BindingFlags.NonPublic);
            if (dataField == null)
            {
                return null;
            }

            dataField.SetValue(message, result);

            // Set correct length (it's still not correct, but at least now the size of the buffer).
            var lengthField = message.GetType().GetField("m_bitLength", BindingFlags.Instance | BindingFlags.NonPublic);
            if (lengthField == null)
            {
                return null;
            }

            lengthField.SetValue(message, unEncLenBits);

            // Restore old position.
            message.Position = oldPosition;

            return message;
        }

        protected void AddUserDataHandler(string identifier, Enum messageID, object handlerFunction)
        {
            this.AddUserDataHandler(identifier, (int)(object)messageID, handlerFunction);
        }

        protected void AddUserDataHandler(string identifier, int messageID, object handlerFunction)
        {
            if (this.userHandlerFunctions.ContainsKey(identifier))
            {
                this.userHandlerFunctions[identifier].AddHandler(messageID, handlerFunction);
            }
            else
            {
                var userFunctions = new UserFunctions<object>();
                this.userHandlerFunctions.Add(identifier, userFunctions);

                this.AddUserDataHandler(identifier, messageID, handlerFunction);
            }
        }

        private void UserPayloadHandlerFunction(NetIncomingMessage message, NetConnection sender)
        {
            // Read length of string.
            byte identiferLength = message.ReadByte();
            byte[] identifierBytes = message.ReadBytes(identiferLength);
            string identifier = Encoding.UTF8.GetString(identifierBytes);
            int messageID = message.ReadInt32();

            Logging.Debug("UserPayloadHandlerFunction: Identifier is: " + identifier + ". MessageID: " + messageID, this);

            // Look for handler.
            if (this.userHandlerFunctions.ContainsKey(identifier))
            {
                var userFunctions = this.userHandlerFunctions[identifier];
                if (userFunctions.HasHandlerFunction(messageID))
                {
                    // Ask super class to invoke.
                    var function = this.userHandlerFunctions[identifier].GetHandlerFunction(messageID);
                    this.InvokeUserPayloadHandlerFunction(function, message, sender);
                }
                else
                {
                    Logging.Error("UserPayloadHandlerFunction: Unknown message ID: " + messageID, this);
                }
            }
        }
    }
}