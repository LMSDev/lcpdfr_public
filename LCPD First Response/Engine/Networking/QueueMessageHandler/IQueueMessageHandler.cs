using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LCPD_First_Response.Engine.Networking.QueueMessageHandler
{
    interface IQueueMessageHandler
    {
        string GetName();
        void Handle(JObject data);
    }
}
