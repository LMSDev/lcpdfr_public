namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using global::LCPDFR.Networking;
    using global::LCPDFR.Networking.User;

    using GTA;

    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Scripts;

    enum ESearchAreaNetworkMessages
    {
        Create,
        Follow,
        DontFollow,
        Remove,
    }

    /// <summary>
    /// A search area blip associated to a ped
    /// </summary>
    internal class SearchArea : BaseComponent, ITickable
    {
        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// Whether this search area is synced across the network.
        /// </summary>
        private bool dontSync;

        /// <summary>
        /// The network ID of the ped assigned.
        /// </summary>
        private int networkID;

        /// <summary>
        /// The blip.
        /// </summary>
        private Blip blip;

        /// <summary>
        /// The last position of the blip.
        /// </summary>
        private Vector3 lastPosition;

        /// <summary>
        /// Size used to simulate growing of the circle.
        /// </summary>
        private float tempSize;

        /// <summary>
        /// A timer to blue/red flash the search area.
        /// </summary>
        private LCPD_First_Response.Engine.Timers.Timer colourTimer;

        /// <summary>
        /// Whether or not the position of the blip should follow the ped.
        /// </summary>
        private bool followPed;

        /// <summary>
        /// A value with the number of current active search areas
        /// </summary>
        private static int activeSearchAreas;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchArea"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        /// <param name="size">
        /// The size of the search area.
        /// </param>
        public SearchArea(CPed ped, float size) : this(ped, size, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchArea"/> class.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        /// <param name="size">
        /// The size.
        /// </param>
        /// <param name="dontSync">
        /// Whether or not the creation should be synced across the network.
        /// </param>
        public SearchArea(CPed ped, float size, bool dontSync)
        {
            this.ped = ped;
            this.dontSync = dontSync;
            this.Size = size;
            this.FollowPed = true;
            this.tempSize = 10f;
            this.blip = AreaBlocker.CreateAreaBlip(ped.Position, this.tempSize, BlipColor.DarkTurquoise);
            this.lastPosition = ped.Position;
            this.colourTimer = new Timers.Timer(250, this.ColourSwap);
            this.colourTimer.Start();

            // Make the radar zoom out like it does for wanted level
            activeSearchAreas++;
            Engine.GUI.Gui.RadarZoom = 640;

            // Broadcast creation.
            if (!dontSync)
            {
                if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.CanSendData)
                {
                    this.networkID = ped.NetworkID;
                    DynamicData dynamicData = new DynamicData(Main.NetworkManager.ActivePeer);
                    dynamicData.Write(this.networkID);
                    dynamicData.Write(size);
                    Main.NetworkManager.SendMessage("SearchArea", ESearchAreaNetworkMessages.Create, dynamicData);
                }
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "SearchArea";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the instance has been deleted.
        /// </summary>
        public bool Deleted { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the position of the blip should follow the ped.
        /// </summary>
        public bool FollowPed
        {
            get
            {
                return this.followPed;
            }

            set
            {
                bool didChange = this.followPed != value;
                this.followPed = value;

                if (!didChange)
                {
                    return;
                }


                Log.Debug("FollowPed: State is now " + value, this);

                // Tell network about search area status.
                if (!this.dontSync)
                {
                    if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.CanSendData)
                    {
                        if (this.ped != null && this.ped.Exists())
                        {
                            ESearchAreaNetworkMessages message = ESearchAreaNetworkMessages.Follow;
                            if (!value)
                            {
                                message = ESearchAreaNetworkMessages.DontFollow;
                            }

                            Main.NetworkManager.SendMessageWithNetworkID("SearchArea", message, this.ped.NetworkID);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the search area.
        /// </summary>
        public float Size { get; set; }

        /// <summary>
        /// Gets or sets the colour of the search area.
        /// </summary>
        public BlipColor Colour { get; set; }

        /// <summary>
        /// The current position of the centre of the search area
        /// </summary>
        public Vector3 GetPosition()
        {
            return this.lastPosition;
        }

        /// <summary>
        /// Called every tick.
        /// </summary>
        public void Process()
        {
            if (this.ped != null && this.ped.Exists())
            {
                if (this.blip.Exists())
                {
                    // If the blip exists, delete it.
                    this.blip.Delete();
                }

                if (this.tempSize < this.Size)
                {
                    this.tempSize += 3;
                }
                else
                {
                    this.tempSize = this.Size;
                }

                if (this.FollowPed && !this.ped.Wanted.HasBeenArrested)
                {
                    // If the search area is to follow the ped, update the lastPosition and create the blip at the current position
                    this.blip = AreaBlocker.CreateAreaBlip(this.ped.Position, this.tempSize, this.Colour);
                    this.lastPosition = this.ped.Position;
                }
                else if (!this.ped.Wanted.HasBeenArrested)
                {
                    // Otherwise, create the blip at the lastPosition
                    this.blip = AreaBlocker.CreateAreaBlip(this.lastPosition, this.tempSize, this.Colour);
                }
                

                if (this.ped.Wanted.HasBeenArrested || !this.ped.IsAliveAndWell)
                {
                    // Remove the search area if has been arrested or dead
                    this.Remove();
                }
            }
            else
            {
                // Remove the search area if the ped doesn't exist
                this.Remove();
            }
        }

        /// <summary>
        /// Removes the search area.
        /// </summary>
        public void Remove()
        {
            if (this.blip != null && this.blip.Exists())
            {
                this.blip.Delete();
            }

            activeSearchAreas--;

            // If no more search areas, zoom radar back in
            if (activeSearchAreas < 1)
            {
                Engine.GUI.Gui.RadarZoom = 0;
            }

            this.colourTimer.Abort();
            this.Delete();

            this.Deleted = true;

            if (!this.dontSync)
            {
                if (Main.NetworkManager.IsNetworkSession && Main.NetworkManager.CanSendData)
                {
                    Main.NetworkManager.SendMessageWithNetworkID("SearchArea", ESearchAreaNetworkMessages.Remove, this.networkID);
                }
            }
        }

        /// <summary>
        /// Called every half second.
        /// </summary>
        private void ColourSwap(object[] parameter)
        {
            // Swap the colours
            if (this.Colour == BlipColor.DarkTurquoise)
            {
                this.Colour = BlipColor.DarkRed;
            }
            else
            {
                this.Colour = BlipColor.DarkTurquoise;
            }
        }

        public static void HandleNetworkMessages()
        {
            if (Main.NetworkManager.IsNetworkSession)
            {
                if (Main.NetworkManager.IsHost)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            Log.Debug("Registering networking handlers", "SearchArea");

            Main.NetworkManager.Client.AddUserDataHandler(
                "SearchArea",
                ESearchAreaNetworkMessages.Create,
                delegate(NetworkServer sender, ReceivedUserMessage message)
                {
                    Log.Debug("Received create message", "SearchArea");
                    int id = message.ReadInt32();
                    float size = message.ReadFloat();
                    CPed networkPed = CPed.FromNetworkID(id);
                    if (networkPed != null && networkPed.Exists())
                    {
                        Log.Debug("Created search area", "SearchArea");
                        networkPed.SearchArea = new SearchArea(networkPed, size, true);
                    }
                    else
                    {
                        Log.Debug("Invalid ID for ped", "SearchArea");
                        if (Main.NetworkManager.Client != null)
                        {
                            Main.NetworkManager.Client.CacheIfPossible(id, message);
                        }
                    }
                });

            Main.NetworkManager.Client.AddUserDataHandler(
                "SearchArea",
                ESearchAreaNetworkMessages.Follow,
                delegate(NetworkServer sender, ReceivedUserMessage message)
                {
                    Log.Debug("Received follow message", "SearchArea");
                    int id = message.ReadInt32();
                    CPed networkPed = CPed.FromNetworkID(id);
                    if (networkPed != null && networkPed.Exists())
                    {
                        if (networkPed.SearchArea != null)
                        {
                            networkPed.SearchArea.FollowPed = true;
                        }
                        else
                        {
                            Log.Debug("SearchArea.Follow: No search area for " + id, "SearchArea");
                        }
                    }
                    else
                    {
                        if (Main.NetworkManager.Client != null)
                        {
                            Main.NetworkManager.Client.CacheIfPossible(id, message);
                        }
                    }
                });

            Main.NetworkManager.Client.AddUserDataHandler(
                "SearchArea",
                ESearchAreaNetworkMessages.DontFollow,
                delegate(NetworkServer sender, ReceivedUserMessage message)
                {
                    Log.Debug("Received don't follow message", "SearchArea");
                    int id = message.ReadInt32();
                    CPed networkPed = CPed.FromNetworkID(id);
                    if (networkPed != null && networkPed.Exists())
                    {
                        if (networkPed.SearchArea != null)
                        {
                            networkPed.SearchArea.FollowPed = false;
                        }
                        else
                        {
                            Log.Debug("SearchArea.DontFollow: No search area for " + id, "SearchArea");
                        }
                    }
                    else
                    {
                        Log.Debug("Invalid ID for ped", "SearchArea");
                        if (Main.NetworkManager.Client != null)
                        {
                            Main.NetworkManager.Client.CacheIfPossible(id, message);
                        }
                    }
                });

            Main.NetworkManager.Client.AddUserDataHandler(
                "SearchArea",
                ESearchAreaNetworkMessages.Remove,
                delegate(NetworkServer sender, ReceivedUserMessage message)
                {
                    int id = message.ReadInt32();
                    CPed networkPed = CPed.FromNetworkID(id);
                    if (networkPed != null && networkPed.Exists())
                    {
                        if (networkPed.SearchArea != null)
                        {
                            networkPed.SearchArea.Remove();
                        }
                        else
                        {
                            Log.Debug("SearchArea.Remove: No search area for " + id, "SearchArea");
                        }
                    }
                });
        }
    }
}
