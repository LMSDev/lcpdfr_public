using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using LCPD_First_Response.Engine.Timers;

namespace LCPD_First_Response.Engine.Networking.QueueMessageHandler
{
    class WallMessage : IQueueMessageHandler
    {
        public string GetName()
        {
            return "WallMessage";
        }

        /// <summary>
        /// Adds a message to our wall.
        /// </summary>
        /// <param name="data">Raw JSON message</param>
        public void Handle(JObject data)
        {
            var message = (string)data["message"];
            DelayedCaller.Call(delegate
            {
                LCPDFR.Main.TextWall.AddText(message);
            }, 1);
        }

        /// <summary>
        /// Create a message which is ready to be sent to a session
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>Raw JSON queue event</returns>
        public JObject CreateMessage(string message)
        {
            JObject newQueueItem = new JObject();
            newQueueItem["type"] = GetName();
            newQueueItem["message"] = message;
            return newQueueItem;
        }
    }
}
