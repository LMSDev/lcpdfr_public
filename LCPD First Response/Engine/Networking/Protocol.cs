using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCPD_First_Response.Engine.Networking.Data;
using ProtoBuf;

namespace LCPD_First_Response.Engine.Networking
{
    /// <summary>
    /// The LCPDFR protocol running on top of NetIncomingMessage/NetOutgoingMessage
    /// Protocol looks like this:
    /// byte MessageID
    /// ProtoBuf Data
    /// </summary>
    class Message
    {
        /// <summary>
        /// The message id that specifies the message type
        /// </summary>
        public byte MessageID { get; private set; }
        public object Data { get; private set; }

        public Protocol()
        {

            //ProtoBuf.Serializer.Serialize<ClientData>()
        }
    }

    enum EMessageID
    {
        PlayerJoined,
    }
}
