namespace LCPD_First_Response.LCPDFR.Callouts
{
    using System;
    using System.Collections.Generic;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Scripts;

    /// <summary>
    /// The callout acceptance state indicating whether the user has already accepted the callout.
    /// </summary>
    public enum ECalloutAcceptanceState
    {
        /// <summary>
        /// No state.
        /// </summary>
        None,

        /// <summary>
        /// Callout can be accepted by the user.
        /// </summary>
        Pending,

        /// <summary>
        /// Callout is running.
        /// </summary>
        Running,

        /// <summary>
        /// Callout has ended.
        /// </summary>
        Ended,
    }

    /// <summary>
    /// Base class for callouts.
    /// </summary>
    [ScriptInfo("Callout", false)]
    public abstract class Callout : GameScript
    {
        /// <summary>
        /// The blip for the area where the callout takes place.
        /// </summary>
        private GTA.Blip areaBlip;

        /// <summary>
        /// The position of the area blip (if any).
        /// </summary>
        private GTA.Vector3 areaBlipPosition;

        /// <summary>
        /// The radius of the area blip.
        /// </summary>
        private float areaBlipRadius;

        /// <summary>
        /// Whether the player state should not be updated on end.
        /// </summary>
        private bool dontUpdatePlayerState;

        /// <summary>
        /// The minimum distance.
        /// </summary>
        private float minimumDistance;

        /// <summary>
        /// The position.
        /// </summary>
        private Vector3 pos;

        /// <summary>
        /// The spawn points.
        /// </summary>
        private static SpawnPoint[] spawnPoints;

        /// <summary>
        /// The state callbacks.
        /// </summary>
        private Dictionary<Enum, Action> stateCallbacks = new Dictionary<Enum, Action>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Callout"/> class.
        /// </summary>
        protected Callout()
        {
            // Default callout message
            this.CalloutMessage = "CalloutMessage missing: " + this.ScriptInfo.Name;
            this.stateCallbacks = new Dictionary<Enum, Action>();

            if (spawnPoints == null)
            {
                LoadSpawnPoints();
            }
        }

        /// <summary>
        /// Gets the position of the area blip (if any).
        /// </summary>
        public Vector3 AreaBlipPosition
        {
            get
            {
                return this.areaBlipPosition;
            }
        }

        /// <summary>
        /// Gets the radius of the area blip (if any).
        /// </summary>
        public float AreaBlipRadius
        {
            get
            {
                return this.areaBlipRadius;
            }
        }

        /// <summary>
        /// Gets or sets the callout message.
        /// </summary>
        public string CalloutMessage { get; protected set; }

        /// <summary>
        /// Gets the acceptance state of the callout indicating whether the user has already accepted the callout.
        /// </summary>
        public ECalloutAcceptanceState AcceptanceState { get; private set; }

        /// <summary>
        /// Gets or sets the state of the callout. State can be any enum.
        /// </summary>
        public Enum State { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the callout is a prank.
        /// </summary>
        protected bool IsPrankCall { get; private set; }

        /// <summary>
        /// Returns <see cref="SpawnPoint"/> around <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="minimumDistance">The maximum distance.</param>
        /// <param name="maximumDistance">The minimum distance.</param>
        /// <returns>The spawnpoint.</returns>
        public static SpawnPoint GetSpawnPointInRange(GTA.Vector3 position, float minimumDistance, float maximumDistance)
        {
            List<SpawnPoint> tempPoints = new List<SpawnPoint>();
 
            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
                float distance = spawnPoint.Position.DistanceTo(position);
                if (distance > minimumDistance && distance < maximumDistance)
                {
                    tempPoints.Add(spawnPoint);
                }
            }

            if (tempPoints.Count > 0)
            {
                return Common.GetRandomCollectionValue<SpawnPoint>(tempPoints.ToArray());
            }

            return SpawnPoint.Zero;
        }

        /// <summary>
        /// Gets all spawn points.
        /// </summary>
        /// <returns>The spawn points.</returns>
        public static SpawnPoint[] GetAllSpawnPoints()
        {
            LoadSpawnPoints();
            return spawnPoints;
        }

        /// <summary>
        /// Called just before the callout message is being displayed. Return false if callout should be aborted.
        /// </summary>
        /// <returns>
        /// True if callout can be displayed, false if it should be aborted.
        /// </returns>
        public virtual bool OnBeforeCalloutDisplayed()
        {
            return true;
        }

        /// <summary>
        /// Called when the callout message is being displayed. Call base to set state to Pending.
        /// </summary>
        public virtual void OnCalloutDisplayed()
        {
            this.AcceptanceState = ECalloutAcceptanceState.Pending;
        }

        /// <summary>
        /// Called when the callout has been accepted. Call base to set state to Running.
        /// </summary>
        /// <returns>
        /// True if callout was setup properly, false if it failed. Calls <see cref="End"/> when failed.
        /// </returns>
        public virtual bool OnCalloutAccepted()
        {
            Engine.GUI.Gui.RadarZoom = 0;
            if (this.areaBlip != null)
            {
                this.areaBlip.Delete();
            }

            this.AcceptanceState = ECalloutAcceptanceState.Running;
            return true;
        }

        /// <summary>
        /// Called when the callout hasn't been accepted. Call base to set state to None.
        /// </summary>
        public virtual void OnCalloutNotAccepted()
        {
            Engine.GUI.Gui.RadarZoom = 0;
            if (this.areaBlip != null)
            {
                this.areaBlip.Delete();
            }

            this.AcceptanceState = ECalloutAcceptanceState.None;
            base.End();
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the calloutmanager to shutdown the callout or can be called by the 
        /// callout itself to execute the cleanup code. Call base to set state to None.
        /// </summary>
        public new virtual void End()
        {
            Engine.GUI.Gui.RadarZoom = 0;
            if (this.areaBlip != null)
            {
                this.areaBlip.Delete();
            }

            this.AcceptanceState = ECalloutAcceptanceState.Ended;
            if (!this.dontUpdatePlayerState)
            {
                LCPDFRPlayer.LocalPlayer.AvailabilityState = EPlayerAvailabilityState.Idle;
            }
            else
            {
                LCPDFRPlayer.LocalPlayer.AvailabilityState = EPlayerAvailabilityState.InCalloutFinished;

                if (!Globals.HasHelpboxDisplayedPlayerUpdateState)
                {
                    DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("CALLOUT_UPDATE_STATE")); }, this, 4000, true);
                    Globals.HasHelpboxDisplayedPlayerUpdateState = true;
                }
            }

            base.End();
        }


        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.State != null)
            {
                foreach (KeyValuePair<Enum, Action> stateCallback in this.stateCallbacks)
                {
                    if (this.State.HasFlag(stateCallback.Key))
                    {
                        stateCallback.Value.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the player is not within the minimum distance to the set position.
        /// </summary>
        /// <returns>True if outside, false if within.</returns>
        internal bool IsNotWithinMinimumDistance()
        {
            if (this.pos == default(Vector3))
            {
                return true;
            }

            return CPlayer.LocalPlayer.Ped.Position.DistanceTo(this.pos) > this.minimumDistance;
        }

        /// <summary>
        /// Adds a minimum distance check to the callout, that will compare <paramref name="minimumDistance"/> to player's distance to <paramref name="position"/> when
        /// <seealso cref="Callout.OnCalloutAccepted"/> is called. If distance is below <paramref name="minimumDistance"/>, callout is aborted.
        /// </summary>
        /// <param name="minimumDistance">The minimum distance.</param>
        /// <param name="position">The position.</param>
        protected void AddMinimumDistanceCheck(float minimumDistance, Vector3 position)
        {
            this.minimumDistance = minimumDistance;
            this.pos = position;
        }

        /// <summary>
        /// Registers <paramref name="state"/> internally so whenever <see cref="Callout.State"/> is <paramref name="state"/><paramref name="callback"/> will be called.
        /// </summary>
        /// <param name="state">The state. Can be any enum.</param>
        /// <param name="callback">The callback.</param>
        protected void RegisterStateCallback(Enum state, Action callback)
        {
            this.stateCallbacks.Add(state, callback);
        }

        /// <summary>
        /// Sets the callout as prank call, 
        /// </summary>
        protected void SetAsPrankCall()
        {
            this.IsPrankCall = true;
        }

        /// <summary>
        /// Sets the callout as finished using various options.
        /// </summary>
        /// <param name="success">Whether the callout was successfully finished.</param>
        /// <param name="playSound">Whether a sound should be played now.</param>
        /// <param name="dontUpdatePlayerState">Whether the player has to manually transmit its state to control.</param>
        protected void SetCalloutFinished(bool success, bool playSound, bool dontUpdatePlayerState)
        {
            if (success)
            {
                
            }

            if (playSound)
            {
                GTA.Native.Function.Call("TRIGGER_MISSION_COMPLETE_AUDIO", 1);
            }

            this.dontUpdatePlayerState = dontUpdatePlayerState;
        }

        /// <summary>
        /// Shows a blip on the map where the callout will take place before the callout has been accepted. When accepted, this blip will be deleted
        /// and the callout itself is responsible for a new blip.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="size">
        /// The size.
        /// </param>
        protected void ShowCalloutAreaBlipBeforeAccepting(GTA.Vector3 position, float size)
        {
            this.areaBlip = AreaBlocker.CreateAreaBlip(position, size);
            this.areaBlipPosition = position;
            this.areaBlipRadius = size;

            // Based on the distance, increase radar zoom
            float distance = LCPDFRPlayer.LocalPlayer.Ped.Position.DistanceTo2D(position);
            Engine.GUI.Gui.RadarZoom = (int)(distance * 3.3);
        }

        /// <summary>
        /// Loads all spawn points.
        /// </summary>
        private static void LoadSpawnPoints()
        {
            string[] lines = FileParser.ParseString(Properties.Resources.Coordinates);
            spawnPoints = new SpawnPoint[lines.Length];
            try
            {
                int i = 0;
                foreach (string line in lines)
                {
                    // Parse vector and populate array
                    SpawnPoint s = Legacy.FileParser.ParseVector3WithHeading(line);
                    spawnPoints[i] = s;
                    i++;
                }
            }
            catch (Exception ex)
            {
                Log.Error("LoadSpawnPoints: Error while loading spawn points", "Callout");
                throw;
            }

            Log.Debug(string.Format("LoadSpawnPoints: {0} coordinates loaded!", spawnPoints.Length), "Callout");
        }
    }
}