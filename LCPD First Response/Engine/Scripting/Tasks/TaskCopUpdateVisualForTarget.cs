namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Task that doesn't do any real action, but just performs a visual check for the target and updates its OfficersVisual property.
    /// </summary>
    internal class TaskCopUpdateVisualForTarget : PedTask
    {
        /// <summary>
        /// The distance within a suspect can be identified if visual is lost.
        /// </summary>
        private const float DistanceToIdentifyLostSuspect = 75f;

        /// <summary>
        /// The distance within a suspect can be identified by a helicopter if visual is lost.
        /// </summary>
        private const float DistanceForHelicopterToIdentifyLostSuspect = 150.0f;

        /// <summary>
        /// Whether the cop can see the suspect.
        /// </summary>
        private bool canSeeSuspect;

        /// <summary>
        /// The target.
        /// </summary>
        private CPed target;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCopUpdateVisualForTarget"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        public TaskCopUpdateVisualForTarget(CPed target) : base(ETaskID.CopUpdateVisualForTarget)
        {
            this.target = target;
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskCopUpdateVisualForTarget";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            // If suspect can be seen currently, remove one cop from visual
            if (this.canSeeSuspect)
            {
                this.target.Wanted.OfficersVisual--;
            }

            this.SetTaskAsDone();
        }

        /// <summary>
        /// Processes the task logic.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Process(CPed ped)
        {
            if (this.target.Exists() && this.target.IsAliveAndWell && !this.target.Wanted.HasBeenArrested)
            {
                // Check if cop can still see suspect. If the suspect is in vehicle, we use a less restrictive check.
                bool oldstate = this.canSeeSuspect;
                if (this.target.IsInVehicle)
                {
                    this.canSeeSuspect = ped.HasSpottedChar(this.target);
                }
                else
                {
                    this.canSeeSuspect = ped.HasSpottedChar(this.target);
                }

                // If suspect is lost, cop has to be close   
                if (this.canSeeSuspect)
                {
                    if (this.target.Wanted.VisualLost)
                    {
                        if (ped.IsInHelicopter())
                        {
                            if (this.target.Position.DistanceTo(ped.Position) > DistanceForHelicopterToIdentifyLostSuspect)
                            {
                                this.canSeeSuspect = false;
                            }
                        }
                        else
                        {
                            if (this.target.Position.DistanceTo(ped.Position) > DistanceToIdentifyLostSuspect)
                            {
                                this.canSeeSuspect = false;
                            }
                        }

                    }
                }

                ped.GetPedData<PedDataCop>().CanSeeSuspect = this.canSeeSuspect;

                // If state equals, do nothing at all
                if (oldstate != this.canSeeSuspect)
                {
                    if (this.canSeeSuspect)
                    {
                        this.target.Wanted.OfficersVisual++;
                        if (!this.target.Wanted.IsBeingArrested)
                        {
                            // Only if the ped isn't already being arrested
                            if (!ped.IsAmbientSpeechPlaying && ped.PedData.ReportGainedVisual)
                            {
                                if (Common.GetRandomBool(0, 2, 1))
                                {
                                    if (this.target.IsInVehicle)
                                    {
                                        if (this.target.CurrentVehicle.Model.IsBoat)
                                        {
                                            ped.SayAmbientSpeech("SUSPECT_IS_IN_BOAT");
                                        }
                                        else if (this.target.CurrentVehicle.Model.IsBike)
                                        {
                                            ped.SayAmbientSpeech("SUSPECT_IS_ON_BIKE");
                                        }
                                        else
                                        {
                                            ped.SayAmbientSpeech("SUSPECT_IS_IN_CAR");
                                        }
                                    }
                                    else
                                    {
                                        ped.SayAmbientSpeech("SUSPECT_IS_ON_FOOT");
                                    }
                                }
                                else
                                {
                                    ped.SayAmbientSpeech("SPOT_SUSPECT");
                                }

                                ped.PedData.ReportGainedVisual = false;
                                DelayedCaller.Call(delegate { ped.PedData.ReportGainedVisual = true; }, 10000);

                                if (this.target.Wanted.OfficersVisual == 1)
                                {
                                    new EventCriminalSpotted(ped, this.target);
                                }
                            }
                        }
                        
                    }
                    else
                    {
                        this.target.Wanted.OfficersVisual--;
                    }
                }
            }
            else
            {
                this.MakeAbortable(ped);
            }
        }
    }
}
