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
    class PlayerNameData : IData
    {
        /// <summary>
        /// The GFWL name the player uses
        /// </summary>
        [ProtoMember(1)]
        public string NetworkName { get; set; }

        public PlayerNameData()
        {
            
        }

        public PlayerNameData(string networkName)
        {
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
