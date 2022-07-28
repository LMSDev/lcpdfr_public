namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    class BehaviorSupportArrest : Behavior
    {
        private readonly Arrest arrest;

        private readonly Partner partner;

        public BehaviorSupportArrest(Partner partner, Arrest arrest)
        {
            Log.Caller();

            this.partner = partner;
            this.arrest = arrest;

            // Hook up events.
            arrest.OnEnd += arrest_OnEnd;
            arrest.PedResisted += this.Abort;

            // Execute logic.
            this.partner.PartnerPed.Task.ClearAll();
            this.partner.PartnerPed.EnsurePedHasWeapon();
            this.partner.PartnerPed.SetWeapon(this.partner.PartnerPed.PedData.DefaultWeapon);
            this.partner.PartnerPed.Task.GoToCharAiming(this.arrest.Suspect, 5f, 10f);
            this.partner.PartnerPed.SetFlashlight(true, true, false);
        }

        public Arrest Arrest
        {
            get
            {
                return this.arrest;
            }
        }

        public override void OnAbort()
        {
            arrest.OnEnd -= this.arrest_OnEnd;
            arrest.PedResisted -= this.Abort;

            if (this.partner.IsAlive)
            {
                if (this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    this.partner.PartnerPed.Task.ClearAll();
                }

                if (this.partner.PartnerPed.Intelligence.TaskManager.IsTaskActive(ETaskID.Flashlight))
                {
                    PedTask task = this.partner.PartnerPed.Intelligence.TaskManager.FindTaskWithID(ETaskID.Flashlight);
                    if (task != null)
                    {
                        this.partner.PartnerPed.Intelligence.TaskManager.Abort(task);
                    }
                }
            }
        }

        public override EBehaviorState Run()
        {
            if (this.arrest.Suspect.Wanted.IsCuffed)
            {
                this.Abort();
            }

            return this.BehaviorState;
        }
        private void arrest_OnEnd(object sender)
        {
            this.Abort();
        }
    }
}