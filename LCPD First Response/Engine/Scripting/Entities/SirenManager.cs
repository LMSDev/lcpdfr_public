namespace LCPD_First_Response.Engine.Scripting.Entities
{
    using System;
    using System.Collections.Generic;

    using AdvancedHookManaged;

    using LCPD_First_Response.Engine.IO;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.LCPDFR;

    /// <summary>
    /// The lighting modes available.
    /// </summary>
    internal enum ELightingMode
    {
        /// <summary>
        /// The default lighting mode of GTA.
        /// </summary>
        Default,

        /// <summary>
        /// From the outer left and right to center.
        /// </summary>
        LeftRightToCenter,

        /// <summary>
        /// Left and right outer lights only.
        /// </summary>
        LeftAndRightOnly,

        /// <summary>
        /// Left and right changing.
        /// </summary>
        LeftAndRightChanging,

        /// <summary>
        /// Left, right and center light only.
        /// </summary>
        LeftAndRightAndCenter,

        /// <summary>
        /// White then red
        /// </summary>
        WhiteThenRed
    }


    /// <summary>
    /// Manages the sirens of a emergency services vehicle. Can toggle single siren and supports multiple patterns.
    /// </summary>
    internal class SirenManager : BaseComponent, ITickable
    {
        /// <summary>
        /// Whether siren can be enabled even for models that do not support it normally.
        /// </summary>
        private bool allowSirenForNonSirenModel;

        /// <summary>
        /// Whether the vehicle model normally supports sirens.
        /// </summary>
        private bool doesModelSupportSirenNormally;

        /// <summary>
        /// The lighting mode.
        /// </summary>
        private ELightingMode lightingMode;

        /// <summary>
        /// The current siren ID.
        /// </summary>
        private int sirenID;

        /// <summary>
        /// The ID for the siren sound.
        /// </summary>
        private int sirenSoundID;

        /// <summary>
        /// The name for the siren sound.
        /// </summary>
        private string sirenSoundName;

        /// <summary>
        /// The main siren timer.
        /// </summary>
        private NonAutomaticTimer timer;

        /// <summary>
        /// The white then red siren timer.
        /// </summary>
        private NonAutomaticTimer whiteRedTimer;

        /// <summary>
        /// The flashing head/taillights timer
        /// </summary>
        private NonAutomaticTimer strobeTimer;

        /// <summary>
        /// The vehicle.
        /// </summary>
        private CVehicle vehicle;

        private bool resetStrobes;

        /// <summary>
        /// Initializes a new instance of the <see cref="SirenManager"/> class.
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle.
        /// </param>
        public SirenManager(CVehicle vehicle)
        {
            this.lightingMode = ELightingMode.Default;
            this.vehicle = vehicle;
            this.timer = new NonAutomaticTimer(2000);
            this.timer.Trigger();

            this.whiteRedTimer = new NonAutomaticTimer(500);
            this.whiteRedTimer.Trigger();

            this.strobeTimer = new NonAutomaticTimer(200);
            this.strobeTimer.Trigger();
            this.doesModelSupportSirenNormally = vehicle.AVehicle.HasSiren;
            this.sirenSoundName = "AMBIENT_HORNS_REGULAR_SIRENS_COP_SIREN_LOOP";
            this.sirenSoundID = -1;
        }

        /// <summary>
        /// Sets a value indicating whether siren can be enabled even for models that do not support it normally.
        /// </summary>
        public bool AllowSirenForNonSirenModel
        {
            set
            {
                this.vehicle.AVehicle.HasSiren = value;
                this.allowSirenForNonSirenModel = value;
            }
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "SirenManager";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the siren is muted.
        /// </summary>
        public bool IsSirenMuted
        {
            get
            {
                return this.vehicle.AVehicle.SirenMuted;
            }
        }

        /// <summary>
        /// Gets or sets the lighting mode.
        /// </summary>
        public ELightingMode LightingMode
        {
            get
            {
                return this.lightingMode;
            }

            set
            {
                this.DeactivateAll();
                this.lightingMode = value;
                this.sirenID = 0;
                this.timer.Trigger();

                // If mode is default, simply activate all sirens
                if (this.lightingMode == ELightingMode.Default)
                {
                    this.ActivateAll();
                }
            }
        }

        /// <summary>
        /// Gets all sirens.
        /// </summary>
        public ASiren[] Sirens
        {
            get
            {
                return this.GetAllSirens();
            }
        }

        /// <summary>
        /// Gets or sets the sound associated with the siren.
        /// </summary>
        public string SirenSound
        {
            get
            {
                return this.sirenSoundName;
            }

            set
            {
                this.sirenSoundName = value;

                if (this.sirenSoundID != -1)
                {
                    SoundEngine.StopSound(this.sirenSoundID);
                    this.sirenSoundID = -1;
                }
            }
        }

        /// <summary>
        /// Activates all siren lights.
        /// </summary>
        public void ActivateAll()
        {
            foreach (ASiren siren in this.Sirens)
            {
                siren.On = true;
            }
        }

        /// <summary>
        /// Deactivates all siren lights.
        /// </summary>
        public void DeactivateAll()
        {
            foreach (ASiren siren in this.Sirens)
            {
                siren.On = false;
            }
        }

        public void MuteSiren()
        {
            if (vehicle.Exists())
            {
                vehicle.AVehicle.SirenMuted = true;
            }
        }

        public void UnmuteSiren()
        {
            if (vehicle.Exists())
            {
                vehicle.AVehicle.SirenMuted = false;
            }
        }

        public void Process()
        {
            if (!this.vehicle.Exists())
            {
                Delete();
                return;
            }

            if (this.allowSirenForNonSirenModel && !this.doesModelSupportSirenNormally)
            {
                if (this.lightingMode == ELightingMode.Default)
                {
                    if (this.vehicle.SirenActive)
                    {
                        if (this.sirenSoundID == -1)
                        {
                            this.sirenSoundID = SoundEngine.PlaySoundFromVehicle(this.vehicle, this.sirenSoundName, true);
                        }
                    }
                    else
                    {
                        if (this.sirenSoundID != -1)
                        {
                            SoundEngine.StopSound(this.sirenSoundID);
                            this.sirenSoundID = -1;
                        }
                    }
                }
            }

            if (this.timer.CanExecute())
            {
                if (this.vehicle.SirenActive)
                {
                    //GTA.Game.DisplayText("SIRENSIRENSIREN");
                    

                    switch (this.lightingMode)
                    {
                        case ELightingMode.LeftRightToCenter:
                            switch (this.sirenID)
                            {
                                case 0:
                                    this.DeactivateAll();
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren0).On = true;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren6).On = true;
                                    this.sirenID++;
                                    break;
                                case 1:
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren1).On = true;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren5).On = true;
                                    this.sirenID++;
                                    break;
                                case 2:
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren2).On = true;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren4).On = true;
                                    this.sirenID++;
                                    break;
                                case 3:
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren3).On = true;
                                    this.sirenID++;
                                    break;
                                case 4:
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren3).On = false;
                                    this.sirenID++;
                                    break;
                                case 5:
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren2).On = false;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren4).On = false;
                                    this.sirenID = 0;
                                    break;
                            }

                            break;

                        case ELightingMode.LeftAndRightChanging:
                            if (this.sirenID == 0)
                            {
                                this.DeactivateAll();
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren0).On = true;
                                this.sirenID++;
                            }
                            else if (this.sirenID == 1)
                            {
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren6).On = true;
                                this.sirenID++;
                            }
                            else if (this.sirenID == 2)
                            {
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren0).On = false;
                                this.sirenID = 0;
                            }

                            break;

                        case ELightingMode.LeftAndRightAndCenter:
                            if (this.sirenID == 0)
                            {
                                this.DeactivateAll();
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren0).On = true;
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren6).On = true;
                                this.sirenID++;
                            }
                            else if (this.sirenID == 1)
                            {
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren3).On = true;
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren0).On = false;
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren6).On = false;
                                this.sirenID = 0;
                            }

                            break;

                        case ELightingMode.LeftAndRightOnly:
                            if (this.sirenID == 0)
                            {
                                this.DeactivateAll();
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren0).On = true;
                                this.vehicle.AVehicle.Siren(VehicleSiren.Siren6).On = true;
                                this.sirenID++;
                            }

                            break;
                    }
                }
            }

            if (this.strobeTimer.CanExecute())
            {
                if (Settings.SirenStrobeLights && this.vehicle.SirenActive)
                {
                    if (resetStrobes) resetStrobes = false;

                    if (this.vehicle.AVehicle.Light(VehicleLight.RightFront).On)
                    {
                        this.vehicle.AVehicle.Light(VehicleLight.RightFront).On = false;
                        this.vehicle.AVehicle.Light(VehicleLight.LeftFront).On = true;
                        this.vehicle.AVehicle.Light(VehicleLight.LeftRear).On = false;
                        this.vehicle.AVehicle.Light(VehicleLight.RightRear).On = true;
                    }
                    else if (this.vehicle.AVehicle.Light(VehicleLight.LeftFront).On)
                    {
                        this.vehicle.AVehicle.Light(VehicleLight.LeftFront).On = false;
                        this.vehicle.AVehicle.Light(VehicleLight.RightFront).On = true;
                        this.vehicle.AVehicle.Light(VehicleLight.LeftRear).On = true;
                        this.vehicle.AVehicle.Light(VehicleLight.RightRear).On = false;
                    }
                }
                else
                {
                    if (!resetStrobes)
                    {
                        this.vehicle.AVehicle.Light(VehicleLight.LeftFront).On = true;
                        this.vehicle.AVehicle.Light(VehicleLight.RightFront).On = true;
                        this.vehicle.AVehicle.Light(VehicleLight.LeftRear).On = true;
                        this.vehicle.AVehicle.Light(VehicleLight.RightRear).On = true;
                        resetStrobes = true;
                    }

                }
            }

            if (this.whiteRedTimer.CanExecute())
            {
                if (this.vehicle.SirenActive)
                {
                    switch (this.lightingMode)
                    {
                        case ELightingMode.WhiteThenRed:
                            switch (this.sirenID)
                            {
                                case 0:
                                    this.DeactivateAll();
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren1).On = true;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren3).On = true;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren5).On = true;
                                    this.sirenID++;
                                    break;
                                case 1:
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren1).On = false;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren3).On = false;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren5).On = false;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren0).On = true;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren2).On = true;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren4).On = true;
                                    this.vehicle.AVehicle.Siren(VehicleSiren.Siren6).On = true;
                                    this.sirenID = 0;
                                    break;
                            }

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all sirens.
        /// </summary>
        /// <returns>All sirens.</returns>
        private ASiren[] GetAllSirens()
        {
            List<ASiren> sirens = new List<ASiren>();
            foreach (VehicleSiren value in Enum.GetValues(typeof(VehicleSiren)))
            {
                sirens.Add(this.vehicle.AVehicle.Siren(value));
            }
            return sirens.ToArray();
        }
    }
}
