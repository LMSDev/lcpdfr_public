using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ProtoBuf;

namespace LCPD_First_Response.Engine.Networking.Data
{
    using LCPDFR.Networking;

    [ProtoContract]
    class PlayerData : IData
    {
        [ProtoMember(1)]
        public IPAddress IPAddress { get; set; }
        /// <summary>
        /// The GFWL name the player uses
        /// </summary>
        [ProtoMember(2)]
        public string NetworkName { get; set; }

        public PlayerData()
        {
            
        }

        public PlayerData(IPAddress ipAddress, string networkName)
        {
            this.IPAddress = ipAddress;
            this.NetworkName = networkName;
        }

        public byte[] ToBytes()
        {
            var memoryStream = new MemoryStream();
            Serializer.Serialize(memoryStream, this);
            return memoryStream.ToArray();
        }
    }
}