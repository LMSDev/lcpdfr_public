namespace LCPD_First_Response.LCPDFR
{
    using System;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.LCPDFR.Scripts;

    /// <summary>
    /// The LCPDFR player class, extending the basic player class with some advanced features.
    /// </summary>
    internal class LCPDFRPlayer : CPlayer<LCPDFRPlayer>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LCPDFRPlayer"/> class.
        /// </summary>
        public LCPDFRPlayer()
        {
            this.AvailabilityState = EPlayerAvailabilityState.Idle;
        }

        /// <summary>
        /// Gets or sets the availability state of the player.
        /// </summary>
        public EPlayerAvailabilityState AvailabilityState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the player can use ANPR.
        /// </summary>
        public bool CanUseANPR
        {
            get
            {
                if (Main.ScriptManager.GetRunningScriptInstances("LicenseNumberScanner").Length > 0)
                {
                    LicenseNumberScanner licenseNumberScanner = Main.ScriptManager.GetRunningScriptInstances("LicenseNumberScanner")[0] as LicenseNumberScanner;
                    return licenseNumberScanner.Enabled;
                }

                return false;
            }

            set
            {
                if (Main.ScriptManager.GetRunningScriptInstances("LicenseNumberScanner").Length > 0)
                {
                    LicenseNumberScanner licenseNumberScanner = Main.ScriptManager.GetRunningScriptInstances("LicenseNumberScanner")[0] as LicenseNumberScanner;
                    licenseNumberScanner.Enabled = value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is arresting someone.
        /// </summary>
        public bool IsArresting
        {
            get
            {
                if (Main.ScriptManager.GetRunningScriptInstances("Arrest").Length > 0)
                {
                    foreach (BaseScript scriptInstance in Main.ScriptManager.GetRunningScriptInstances("Arrest"))
                    {
                        if ((scriptInstance as Arrest).RequiresPlayerAttention)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is busy, e.g. arresting, frisking or performing a pullover.
        /// </summary>
        public bool IsBusy
        {
            get
            {
                if (this.IsArresting)
                {
                    return true;
                }

                if (this.IsChasing)
                {
                    return true;
                }

                if (Main.CalloutManager.IsCalloutRunning)
                {
                    return true;
                }

                if (this.IsPullingOver)
                {
                    return true;
                }

                if (this.IsUsingPoliceComputer)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the player has set its state to busy for callouts.
        /// </summary>
        public bool IsBusyForCallouts { get; set; }

        /// <summary>
        /// Gets a value indicating whether the player is currently in a chase.
        /// </summary>
        public bool IsChasing
        {
            get
            {
                return this.Ped.PedData.CurrentChase != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether player is grabbing anyone.
        /// </summary>
        public bool IsGrabbing
        {
            get
            {
                return Grab.grabbing;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is in a police department.
        /// </summary>
        public bool IsInPoliceDepartment
        {
            get
            {
                return Main.PoliceDepartmentManager.IsPlayerInPoliceDepartment;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is currently in the tutorial.
        /// </summary>
        public bool IsInTutorial
        {
            get
            {
                return Main.ScriptManager.GetRunningScriptInstances("Tutorial").Length > 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is performing a pullover.
        /// </summary>
        public bool IsPullingOver
        {
            get
            {
                return Main.PulloverManager.IsPulloverRunning;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the player is reporting anything
        /// </summary>
        public bool IsReporting { get; set; }

        /// <summary>
        /// Gets a value indicating whether the player is actively using the police computer.
        /// </summary>
        public bool IsUsingPoliceComputer
        {
            get
            {
                return Main.PoliceComputer.IsActive;
            }
        }

        public bool IsViewingArrestOptions
        {
            get
            {
                if (Main.ScriptManager.GetRunningScriptInstances("AimingManager").Length > 0)
                {
                    foreach (BaseScript scriptInstance in Main.ScriptManager.GetRunningScriptInstances("AimingManager"))
                    {
                        if ((scriptInstance as AimingManager).menuShowMoreOptions)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }


        }

        /// <summary>
        /// Toggles the player hat.
        /// </summary>
        public void ToggleHat()
        {
            int propIndex = this.Ped.Skin.GetPropIndex((GTA.PedProp)EPedProp.Hat);
            if (propIndex == this.Ped.Model.ModelInfo.GetNumberOfHats())
            {
                propIndex = 0;
            }
            else
            {
                propIndex++;
            }

            this.Ped.SetProp(EPedProp.Hat, propIndex);
        }

        /// <summary>
        /// The car used in the tutorial which the player can place the suspect in.
        /// </summary>
        public CVehicle TutorialCar { get; set; }
    }


    /// <summary>
    /// The availability of the player, which determines whether callouts can be created.
    /// </summary>
    internal enum EPlayerAvailabilityState
    {
        /// <summary>
        /// Player is idle.
        /// </summary>
        Idle,

        /// <summary>
        /// Player is in a callout.
        /// </summary>
        InCallout,

        /// <summary>
        /// Player is in a callout, but has not yet updated the state to be available again.
        /// </summary>
        InCalloutFinished,
    }
}