using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using LCPD_First_Response.Engine;
using LCPD_First_Response.Engine.Timers;

namespace LCPD_First_Response.LCPDFR.Scripts.PoliceComputer
{
    internal class ChatClient
    {
        /// <summary>
        /// The web endpoint for the webserver.
        /// </summary>
        public const string WebEndpoint = "lcpdfr.endpoint.d081.lcpdfr.com";
        private int roomId;
        private bool isConnected = false;
        public delegate void OnChatConnectionStateChangedHandler();
        public delegate void OnChatReceivedMesageEventHandler(CChatMessage message);
        public event OnChatConnectionStateChangedHandler OnChatConnectionStateChanged;
        public event OnChatReceivedMesageEventHandler OnReceivedChatMesage;
        private object sanityLock;

        public bool IsConnected
        {
            get { return isConnected; }
        }

        public void Connect(int roomId)
        {

        }

        public void GetRoomDetails(int roomId, Action<CChatRoom> callbackAction)
        {
            // Delegate to fetch all arguments and call internal function
            Action threadDelegate = delegate
            {
                string JSON = GetWebResponse("http://" + WebEndpoint + "/cops/newcs/chat.php?do=getDetails&roomId=" + roomId);
                var s = new System.Web.Script.Serialization.JavaScriptSerializer();
                CChatRoom obj = s.Deserialize<CChatRoom>(JSON);
                DelayedCaller.Call(delegate { callbackAction(obj); }, this, 1);
            };

            // Spawn thread
            Thread thread = new Thread(new ThreadStart(threadDelegate));
            thread.IsBackground = true;
            thread.Start();

        }

        private string GetWebResponse(string url)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            request.Proxy = null;
            request.Headers.Add("X-LCPDFR-Request", "True");
            request.Headers.Add("X-LCPDFR-HWAuth", Authentication.GetHardwareID());
            request.Headers.Add("X-LCPDFR-APIKey", Settings.NetworkAPIKey);
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            request.Headers.Add("X-LCPDFR-Version",
                String.Format("{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision));

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            Stream responseStream = response.GetResponseStream();

            StreamReader streamReader = new StreamReader(responseStream);
            string responseData = streamReader.ReadToEnd();
            return responseData;
        }

        internal class CChatRoom
        {
            public int ID;
            public string Name;
            public EChatRoomType RoomType;
            public bool CurrentUserHasAccess;
            public List<CChatUser> Users;
        }

        internal class CChatUser
        {
            public int ID;
            public string DisplayName;
            public string OrginatingGFWLName;
        }

        internal class CChatMessage
        {
            public int ID;
            public EChatMessageType TypeID;
            public CChatUser OrginatingUser;
            public string Message;
            public double Date;
            public DateTime GetProperDateTime()
            {
                var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                dtDateTime = dtDateTime.AddSeconds(Date).ToLocalTime();
                return dtDateTime;
            }
        }

        internal enum EChatRoomType
        {
            Public,
            Group,
            Internal
        }

        internal enum EChatMessageType
        {
            Chat,
            Join,
            Part,
            Close
        }
    }
}
