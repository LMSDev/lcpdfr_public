namespace LCPDFR.Networking
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Class that allows to communicate with the router using UPNP to discover the external IP and easy portforwarding.
    /// Taken from http://www.codeproject.com/Articles/27992/NAT-Traversal-with-UPnP-in-C with a few modifications
    /// (the method they use to get the internal IP is wrong)
    /// </summary>
    internal class UPnP
    {
        /// <summary>
        /// The timeout for discover.
        /// </summary>
        private TimeSpan timeout = new TimeSpan(0, 0, 0, 3);

        /// <summary>
        /// The different urls.
        /// </summary>
        private string descUrl, serviceUrl, eventUrl;

        /// <summary>
        /// The IP address of the local gateway, that is the discovered UPnP device.
        /// </summary>
        private IPAddress localGateway;

        /// <summary>
        /// The local IP address.
        /// </summary>
        private IPAddress localIpAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="UPnP"/> class.
        /// </summary>
        public UPnP()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UPnP"/> class.
        /// </summary>
        /// <param name="localIpAddress">
        /// The local IP address.
        /// </param>
        /// <param name="defaultInternetGateway">
        /// The default internet gateway IP address.
        /// </param>
        public UPnP(string localIpAddress, string defaultInternetGateway)
        {
            if (!string.IsNullOrEmpty(localIpAddress))
            {
                this.localIpAddress = IPAddress.Parse(localIpAddress);
            }

            if (!string.IsNullOrEmpty(defaultInternetGateway))
            {
                this.localGateway = IPAddress.Parse(defaultInternetGateway);
            }
        }

        /// <summary>
        /// Gets a value indicating whether an UPnP firewall was found.
        /// </summary>
        public bool FoundFirewall { get; private set; }

        /// <summary>
        /// Gets the local gateway IP address.
        /// </summary>
        public IPAddress LocalGateway
        {
            get
            {
                return this.localGateway;
            }
        }

        /// <summary>
        /// Gets the local IP address.
        /// </summary>
        public IPAddress LocalIpAddress
        {
            get
            {
                return this.localIpAddress;
            }
        }

        /// <summary>
        /// Gets the service url.
        /// </summary>
        public string ServiceUrl
        {
            get
            {
                return this.serviceUrl;
            }
        }

        /// <summary>
        /// Gets or sets the timeout for discover.
        /// </summary>
        public TimeSpan TimeOut
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

        /// <summary>
        /// Tries to discover a UPnP device. Returns true on success.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool Discover()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            string req = "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: 239.255.255.250:1900\r\n" +
            "ST:upnp:rootdevice\r\n" +
            "MAN:\"ssdp:discover\"\r\n" +
            "MX:2\r\n\r\n";
            byte[] data = Encoding.ASCII.GetBytes(req);
            IPEndPoint MulticastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
            byte[] buffer = new byte[0x1000];

            DateTime start = DateTime.Now;

            // While timeout not reached
            while (DateTime.Now - start < this.timeout)
            {
                // Send some data
                s.SendTo(data, MulticastEndPoint);
                s.SendTo(data, MulticastEndPoint);
                s.SendTo(data, MulticastEndPoint);

                if (s.Poll(500000, SelectMode.SelectRead))
                {
                    int length = s.Receive(buffer);
                    string resp = Encoding.ASCII.GetString(buffer, 0, length).ToLower();
                    //Console.WriteLine("---------------------------\r\n" + resp);
                    if (resp.Contains("upnp:rootdevice"))
                    {
                        resp = resp.Substring(resp.ToLower().IndexOf("location:") + 9);
                        resp = resp.Substring(0, resp.IndexOf("\r")).Trim();
                        //Console.WriteLine(resp);
                        if (!string.IsNullOrEmpty(this.serviceUrl = this.GetServiceUrl(resp)))
                        {
                            //Console.WriteLine(this.serviceUrl);
                            this.descUrl = resp;

                            // Try to get the IP address from the service url if no local gateway set yet.
                            if (this.localGateway == null)
                            {
                                if (this.descUrl.Contains("http://"))
                                {
                                    this.descUrl = this.descUrl.Replace("http://", string.Empty);
                                }

                                if (this.descUrl.Contains(":"))
                                {
                                    string ip = this.descUrl.Substring(0, this.descUrl.IndexOf(":"));
                                    //Console.WriteLine(ip);

                                    // Try to parse IP.
                                    IPAddress.TryParse(ip, out this.localGateway);
                                }
                            }

                            this.FoundFirewall = true;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to set the service url to the local internet gateway if it supports UPnP. Use this prior to <see cref="Discover"/> to avoid searching the network for the right router.
        /// Returns true if router supports UPnP and service url was set, false if not.
        /// </summary>
        /// <returns>True on success, false otherwise.</returns>
        public bool SetServiceUrlUsingLocalInternetGateway()
        {
            // Find gateway
            IPAddress address = this.localGateway;

            // Try whether the gateway supports UPnP
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            string req = "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: 239.255.255.250:1900\r\n" +
            "ST:upnp:rootdevice\r\n" +
            "MAN:\"ssdp:discover\"\r\n" +
            "MX:3\r\n\r\n";
            byte[] data = Encoding.ASCII.GetBytes(req);
            byte[] buffer = new byte[0x1000];
            DateTime start = DateTime.Now;

            // While timeout not reached
            while (DateTime.Now - start < this.timeout)
            {
                // Send data to IP
                IPEndPoint ipe = new IPEndPoint(address, 1900);
                s.SendTo(data, ipe);

                if (s.Poll(500000, SelectMode.SelectRead))
                {
                    int length = s.Receive(buffer);
                    string resp = Encoding.ASCII.GetString(buffer, 0, length).ToLower();
                    //Console.WriteLine(resp);
                    if (resp.Contains("upnp:rootdevice"))
                    {
                        resp = resp.Substring(resp.ToLower().IndexOf("location:") + 9);
                        resp = resp.Substring(0, resp.IndexOf("\r")).Trim();
                        if (!string.IsNullOrEmpty(serviceUrl = GetServiceUrl(resp)))
                        {
                            this.descUrl = resp;
                            this.FoundFirewall = true;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the local IP address.
        /// </summary>
        /// <returns>The local IP address.</returns>
        public string GetLocalIPAddress()
        {
            IPHostEntry host;
            string localIP = string.Empty;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    this.localIpAddress = ip;
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        /// <summary>
        /// Returns the local IP address and ensures the gateway of the local IP is set to the discovered UPnP device.
        /// This is to prevent bad entries such as Hamachi or Tunngle to be returned (as they are first in order).
        /// </summary>
        /// <returns>The local IP address.</returns>
        public string GetLocalIPAddressForDiscoveredGateway()
        {
            var cards = NetworkInterface.GetAllNetworkInterfaces().ToList();

            IPHostEntry host;
            string localIP = string.Empty;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    // Ensure IP has local gateway set to our local gateway.
                    if (cards.Any())
                    {
                        foreach (var card in cards)
                        {
                            var props = card.GetIPProperties();
                            if (props == null)
                                continue;

                            var gateways = props.GatewayAddresses;
                            if (!gateways.Any())
                                continue;

                            // Check if it has our gateway set.
                            if (gateways.Any(gateway => gateway.Address.Equals(this.localGateway)))
                            {
                                // Get all assigned IPs for this network interface and return the internetwork one.
                                foreach (UnicastIPAddressInformation unicast in props.UnicastAddresses)
                                {
                                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                                    {
                                        this.localIpAddress = unicast.Address;
                                        return unicast.Address.ToString();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return localIP;
        }

        /// <summary>
        /// Forwards <paramref name="port"/>.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="description">The description.</param>
        public void ForwardPort(int port, ProtocolType protocol, string description)
        {
            if (string.IsNullOrEmpty(serviceUrl))
                throw new Exception("No UPnP service available or Discover() has not been called");
            XmlDocument xdoc = SOAPRequest(serviceUrl, "<u:AddPortMapping xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
                "<NewRemoteHost></NewRemoteHost><NewExternalPort>" + port.ToString() + "</NewExternalPort><NewProtocol>" + protocol.ToString().ToUpper() + "</NewProtocol>" +
                "<NewInternalPort>" + port.ToString() + "</NewInternalPort><NewInternalClient>" + this.localIpAddress +
                "</NewInternalClient><NewEnabled>1</NewEnabled><NewPortMappingDescription>" + description +
            "</NewPortMappingDescription><NewLeaseDuration>0</NewLeaseDuration></u:AddPortMapping>", "AddPortMapping");
        }

        /// <summary>
        /// Deletes a forwarding rule on <paramref name="port"/>.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol.</param>
        public void DeleteForwardingRule(int port, ProtocolType protocol)
        {
            if (string.IsNullOrEmpty(serviceUrl))
                throw new Exception("No UPnP service available or Discover() has not been called");
            XmlDocument xdoc = SOAPRequest(serviceUrl,
            "<u:DeletePortMapping xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
            "<NewRemoteHost>" +
            "</NewRemoteHost>" +
            "<NewExternalPort>" + port + "</NewExternalPort>" +
            "<NewProtocol>" + protocol.ToString().ToUpper() + "</NewProtocol>" +
            "</u:DeletePortMapping>", "DeletePortMapping");
        }

        /// <summary>
        /// Gets the external IP.
        /// </summary>
        /// <returns>The IP address.</returns>
        public IPAddress GetExternalIP()
        {
            if (string.IsNullOrEmpty(serviceUrl))
                throw new Exception("No UPnP service available or Discover() has not been called");
            XmlDocument xdoc = SOAPRequest(serviceUrl, "<u:GetExternalIPAddress xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
            "</u:GetExternalIPAddress>", "GetExternalIPAddress");
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xdoc.NameTable);
            nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
            string IP = xdoc.SelectSingleNode("//NewExternalIPAddress/text()", nsMgr).Value;
            return IPAddress.Parse(IP);
        }

        /// <summary>
        /// Gets the service url for <paramref name="resp"/>.
        /// </summary>
        /// <param name="resp">The web url.</param>
        /// <returns>The service url.</returns>
        private string GetServiceUrl(string resp)
        {
#if !DEBUG
        try
        {
#endif
            XmlDocument desc = new XmlDocument();
            desc.Load(WebRequest.Create(resp).GetResponse().GetResponseStream());
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(desc.NameTable);
            nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
            XmlNode typen = desc.SelectSingleNode("//tns:device/tns:deviceType/text()", nsMgr);
            if (!typen.Value.Contains("InternetGatewayDevice"))
                return null;
            XmlNode node = desc.SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:WANIPConnection:1\"]/tns:controlURL/text()", nsMgr);
            if (node == null)
                return null;
            XmlNode eventnode = desc.SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:WANIPConnection:1\"]/tns:eventSubURL/text()", nsMgr);
            eventUrl = CombineUrls(resp, eventnode.Value);
            return CombineUrls(resp, node.Value);
#if !DEBUG
        }
        catch { return null; }
#endif
        }

        private string CombineUrls(string resp, string p)
        {
            int n = resp.IndexOf("://");
            n = resp.IndexOf('/', n + 3);
            return resp.Substring(0, n) + p;
        }

        private XmlDocument SOAPRequest(string url, string soap, string function)
        {
            string req = "<?xml version=\"1.0\"?>" +
            "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
            "<s:Body>" +
            soap +
            "</s:Body>" +
            "</s:Envelope>";
            WebRequest r = HttpWebRequest.Create(url);
            r.Method = "POST";
            byte[] b = Encoding.UTF8.GetBytes(req);
            r.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:WANIPConnection:1#" + function + "\"");
            r.ContentType = "text/xml; charset=\"utf-8\"";
            r.ContentLength = b.Length;
            r.GetRequestStream().Write(b, 0, b.Length);
            XmlDocument resp = new XmlDocument();
            WebResponse wres = r.GetResponse();
            Stream ress = wres.GetResponseStream();
            resp.Load(ress);
            return resp;
        }
    }
}