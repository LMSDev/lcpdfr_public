namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    class BehaviorHoldPedAtGunpoint : Behavior, ICanOwnEntities, IPedController
    {
        private ContentManager contentManager;
        private readonly Partner partner;
        private readonly CPed ped;

        private readonly bool arrest;

        public BehaviorHoldPedAtGunpoint(Partner partner, CPed ped, bool arrest)
        {
            this.partner = partner;
            this.ped = ped;
            this.arrest = arrest;
            this.contentManager = new ContentManager();

            this.Start();
        }

        public override void OnAbort()
        {
            if (this.partner.PartnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
            {
                this.partner.PartnerPed.Intelligence.TaskManager.Abort(this.partner.PartnerPed.Intelligence.TaskManager.FindTaskWithID(ETaskID.BustPed));
                this.partner.PartnerPed.Task.ClearAll();
            }

            // Very, very weird bug here: If we just free the ped below, the next attempt to access it (most likely from the grab script as it accesses all peds) will pass
            // existence check but in CPed:1334 accessing the position will make it crash. The ped where it fails is the ped we would free here. What the hell is going on?
            //Log.Debug("Partner handle: " + this.partner.PartnerPed.Handle + " Sus handle: " + this.ped.Handle, "BehaviorHoldPedAtGunpoint");

            if (this.ped.Exists() && !this.ped.Wanted.IsCuffed && this.ped.Intelligence.IsStillAssignedToController(this))
            {
                this.ped.Intelligence.ResetAction(this);
                this.ped.Task.ClearAll();
            }
        }

        public override EBehaviorState Run()
        {
            if (!this.ped.Exists())
            {
                this.Abort();
                return EBehaviorState.Failed;
            }

            this.contentManager.Process();

            if (!this.partner.PartnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.BustPed))
            {
                this.Abort();
                return EBehaviorState.Success;
            }

            return this.BehaviorState;
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public void PedHasLeft(CPed ped)
        {
            if (ped.Exists())
            {
                ped.ReleaseOwnership(this);
                ped.Intelligence.TaskManager.ClearTasks();
                ped.Task.ClearAll();

                this.contentManager.RemovePed(ped);
            }

            this.OnAbort();
        }

        private void Start()
        {
            if (this.arrest)
            {
                if (!this.ped.PedData.CanBeArrestedByPlayer || this.ped.Wanted.IsBeingArrested) return;

                // Owning the ped for an arrest is mandatory.
                EPedActionPriority priority = EPedActionPriority.RequiredByScriptCritical;
                if (!this.ped.Intelligence.IsFreeForAction(priority))
                {
                    this.Abort();
                    return;
                }

                this.ped.Intelligence.RequestForAction(priority, this);
                this.ped.RequestOwnership(this);
                this.contentManager.AddPed(ped, 200f, EContentManagerOptions.KillBeforeFree);
                ped.PedData.DontAllowEmptyVehiclesAsTransporter = true;
            }
            else
            {
                // Only own ped if no owner is set.
                if (!this.ped.HasOwner && !this.ped.IsRequiredForMission)
                {
                    if (this.ped.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this))
                    {
                        this.ped.RequestOwnership(this);
                        this.contentManager.AddPed(ped, 200f, EContentManagerOptions.KillBeforeFree);
                    }
                }

                // When not arresting, make suspect put hands up if either stopped, surrendered or not involved in any kind of pursuit.
                if (this.ped.Wanted.Surrendered || this.ped.Wanted.IsStopped || this.ped.PedData.Available)
                {
                    if (!this.ped.Wanted.IsBeingArrested && !this.ped.Wanted.IsBeingFrisked
                        && !this.ped.Wanted.IsBeingArrestedByPlayer && !this.ped.Wanted.IsCuffed)
                    {
                        if (!this.ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleHandsUp))
                        {
                            this.ped.Task.ClearAll();
                            this.ped.Task.HandsUp(-2);
                        }
                    }

                }
            }

            //AudioHelper.PlayActionSound("ARREST_PLAYER");
            this.partner.PartnerPed.SayAmbientSpeech(this.partner.PartnerPed.VoiceData.ArrestSpeech);

            // Prevent walkie talkie task from fucking up things
            if (this.partner.PartnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkieTalkie))
            {
                this.partner.PartnerPed.Intelligence.TaskManager.Abort(this.partner.PartnerPed.Intelligence.TaskManager.FindTaskWithID(ETaskID.WalkieTalkie));
            }

            TaskBustPed taskBustPed = new TaskBustPed(ped) { BlockCuffing = !this.arrest };
            taskBustPed.AssignTo(this.partner.PartnerPed, ETaskPriority.MainTask);
            Stats.UpdateStat(Stats.EStatType.PartnerOrderedArrest, 1);
        }
    }
}