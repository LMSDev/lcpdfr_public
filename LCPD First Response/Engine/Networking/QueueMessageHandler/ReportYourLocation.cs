using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LCPD_First_Response.Engine.Scripting.Entities;

namespace LCPD_First_Response.Engine.Networking.QueueMessageHandler
{
    /// <summary>
    /// This will be used for the clan system -- it will enable dispatchers to get all unit locations of their clan's LCPDFR players.
    /// There is no CreateMessage() - as this is a type which will only be created by the server itself or a web client. Response-only.
    /// </summary>
    class ReportYourLocation : IQueueMessageHandler
    {
        public string GetName()
        {
            return "ReportYourLocation";
        }

        public void Handle(Newtonsoft.Json.Linq.JObject data)
        {
            var location = CPlayer.LocalPlayer.Ped.Position;
            Main.LCPDFRServer.SetSessionVariable("LocationX", location.X.ToString());
            Main.LCPDFRServer.SetSessionVariable("LocationY", location.Y.ToString());
            Main.LCPDFRServer.SetSessionVariable("LocationZ", location.Z.ToString());
            string area = Engine.Scripting.AreaHelper.GetAreaNameMeaningful(location);
            Main.LCPDFRServer.SetSessionVariable("Area", area);
            if (data["fromSession"] != null)
            {
                var obj = new Newtonsoft.Json.Linq.JObject();
                obj["type"] = "ReportedLocation";
                obj["fromSession"] = Main.LCPDFRServer.SessionID;
                Main.LCPDFRServer.SendQueueItemToSession((string)data["fromSession"], obj);
            }
        }
    }
}
