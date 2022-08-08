namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using GTA;

    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;

    using Timer = LCPD_First_Response.Engine.Timers.Timer;

    /// <summary>
    /// Responsible for displaying close police departments as well as letting the player in. 
    /// </summary>
    [ScriptInfo("PoliceDepartmentManager", true)]
    internal class PoliceDepartmentManager : GameScript
    {
        /// <summary>
        /// Maximum number of pd blips that are drawn.
        /// </summary>
        private const int MaxNumberOfBlipsDrawn = 3;

        /// <summary>
        /// Whether all pd blips are shown.
        /// </summary>
        private bool showAllPDs;

        /// <summary>
        /// The timer to update the blips.
        /// </summary>
        private Timer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoliceDepartmentManager"/> class.
        /// </summary>
        public PoliceDepartmentManager()
        {
            //Load pd data using legacy code
            Legacy.DataFile dataFile = new Legacy.DataFile(Properties.Resources.PoliceDepartments);
            Legacy.DataSet dataSet = dataFile.DataSets[0];

            // Initialize list with capacity
            this.PoliceDepartments = new List<PoliceDepartment>(dataSet.Tags.Length);

            foreach (Legacy.Tag tag in dataSet.Tags) {
                if (tag.Name != "PD") {
                    continue;
                }

                Vector3 blipPos = Legacy.FileParser.ParseVector3(tag.GetAttributeByName("BLIPPOS").Value);
                Vector3 carPos = Legacy.FileParser.ParseVector3(tag.GetAttributeByName("CARPOS").Value);
                float carHeading = tag.GetAttributesValueByName<float>("CARHEADING");

                PoliceDepartment policeDepartment = new PoliceDepartment(blipPos, carPos, carHeading);
                policeDepartment.PlayerEnteredLeft += new PoliceDepartment.PlayerEnteredLeftPDEventHandler(this.policeDepartment_PlayerEnteredLeft);
                policeDepartment.PlayerCloseToPD += new PoliceDepartment.PlayerCloseToPDEventHandler(this.policeDepartment_PlayerCloseToPD);
                this.PoliceDepartments.Add(policeDepartment);
            }

            // Setup timer to update pd blips
            this.UpdatePDBlips = true;
            this.timer = new Timer(5000, this.UpdateBlips);
            this.timer.Start();
        }

        /// <summary>
        /// Fired when the player has entered or left the police department.
        /// </summary>
        public event PoliceDepartment.PlayerEnteredLeftPDEventHandler PlayerEnteredLeftPD;

        /// <summary>
        /// Fired when the player is close to a pd.
        /// </summary>
        public event PoliceDepartment.PlayerCloseToPDEventHandler PlayerCloseToPD;

        /// <summary>
        /// Gets the closest police department set by the last blip update. This might be no longer the closest one.
        /// </summary>
        public PoliceDepartment ClosestPoliceDepartment { get; private set; }

        /// <summary>
        /// Gets the current police department the player is in.
        /// </summary>
        public PoliceDepartment CurrentPoliceDepartment { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the player is currently in a police department. Note that is true even if the model selection is taking place outside!
        /// </summary>
        public bool IsPlayerInPoliceDepartment { get; private set; }

        /// <summary>
        /// Gets the last police department the player was in. Updated when leaving a pd.
        /// </summary>
        public PoliceDepartment LastPoliceDepartment { get; private set; }

        /// <summary>
        /// Gets the police departments.
        /// </summary>
        public List<PoliceDepartment> PoliceDepartments { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether all police department blips are shown.
        /// </summary>
        public bool ShowAllPDs
        {
            get
            {
                return this.showAllPDs;
            }

            set
            {
                this.showAllPDs = value;

                foreach (PoliceDepartment policeDepartment in this.PoliceDepartments)
                {
                    policeDepartment.Visible = this.showAllPDs;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the blips of the police departments are updated.
        /// </summary>
        public bool UpdatePDBlips { get; set; }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.CurrentPoliceDepartment != null)
            {
                this.CurrentPoliceDepartment.Process();
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            // Remove all pds
            foreach (PoliceDepartment policeDepartment in this.PoliceDepartments)
            {
                policeDepartment.End();
            }

            this.PoliceDepartments = null;

            // Remove timer
            this.timer.Stop();
        }

        /// <summary>
        /// Updates the showed pd blips.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        public void UpdateBlips(params object[] parameter)
        {
            // Don't update when in a pd
            if (this.IsPlayerInPoliceDepartment)
            {
                return;
            }

            if (!this.UpdatePDBlips)
            {
                return;
            }

            // Calculate closest police departments
            Dictionary<PoliceDepartment, float> allPoliceDepartments = new Dictionary<PoliceDepartment, float>(MaxNumberOfBlipsDrawn);
            foreach (PoliceDepartment policeDepartment in this.PoliceDepartments)
            {
                float distance = CPlayer.LocalPlayer.Ped.Position.DistanceTo(policeDepartment.Position);

                allPoliceDepartments.Add(policeDepartment, distance);
            }

            // Sort by distance
            List<KeyValuePair<PoliceDepartment, float>> policeDepartmentsList = allPoliceDepartments.ToList();
            policeDepartmentsList.Sort((firstPair, nextPair) => firstPair.Value.CompareTo(nextPair.Value));

            // Only draw the closest ones
            for (int i = 0; i < policeDepartmentsList.Count; i++)
            {
                if (i == 0)
                {
                    this.ClosestPoliceDepartment = policeDepartmentsList[i].Key;
                }

                if (i < MaxNumberOfBlipsDrawn)
                {
                    policeDepartmentsList[i].Key.Visible = true;
                }
                else
                {
                    // Don't draw the pds that are further away
                    if (policeDepartmentsList[i].Key.Visible)
                    {
                        policeDepartmentsList[i].Key.Visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the player has either entered or left a police department.
        /// </summary>
        /// <param name="policeDepartment">The police department.</param>
        /// <param name="entered">True if entered, false if not.</param>
        private void policeDepartment_PlayerEnteredLeft(PoliceDepartment policeDepartment, bool entered)
        {
            if (entered)
            {
                this.CurrentPoliceDepartment = policeDepartment;
                this.IsPlayerInPoliceDepartment = true;
            }
            else
            {
                this.CurrentPoliceDepartment = null;
                this.IsPlayerInPoliceDepartment = false;
                this.LastPoliceDepartment = policeDepartment;
            }

            if (this.PlayerEnteredLeftPD != null)
            {
                this.PlayerEnteredLeftPD(policeDepartment, entered);
            }
        }

        /// <summary>
        /// Called when the player is close to a pd.
        /// </summary>
        /// <param name="policeDepartment">The police department.</param>
        /// <returns>True if player can enter the pd, false if not.</returns>
        private bool policeDepartment_PlayerCloseToPD(PoliceDepartment policeDepartment)
        {
            if (this.PlayerCloseToPD != null)
            {
                return this.PlayerCloseToPD(policeDepartment);
            }

            return true;
        }
    }
}
