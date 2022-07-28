namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    class BehaviorCoverTarget : Behavior
    {
        private readonly Vector3 position;
        private readonly CPed target;
        private readonly Partner partner;
        private bool covering;

        public BehaviorCoverTarget(Partner partner, Vector3 position)
        {
            Log.Caller();

            this.partner = partner;
            this.position = position;

            // Execute logic.
            this.partner.PartnerPed.Task.ClearAll();
            this.partner.PartnerPed.EnsurePedHasWeapon();
            //this.partner.PartnerPed.SayAmbientSpeech("JACKING_GENERIC_BACK");
            //this.partner.PartnerPed.Task.GoToCharAiming(this.arrest.Suspect, 5f, 10f);
        }

        public BehaviorCoverTarget(Partner partner, CPed ped)
        {
            Log.Caller();

            this.partner = partner;
            this.target = ped;

            // Execute logic.
            //this.partner.PartnerPed.SayAmbientSpeech("JACKING_GENERIC_BACK");
            this.partner.PartnerPed.Task.ClearAll();

            this.partner.Intelligence.SetIsInPlayerGroup(false);
            // Log.Debug("Removed partner from player group", "BehaviorCoverTarget");

            //this.partner.PartnerPed.EnsurePedHasWeapon();
            //this.partner.PartnerPed.Task.GoTo(this.target);
            this.partner.PartnerPed.SetNextDesiredMoveState(Engine.Scripting.Native.EPedMoveState.Walk);
            this.partner.PartnerPed.Task.GoToCharAiming(this.target, 2f, 2.5f);
        }

        public override void OnAbort()
        {
            // arrest.OnEnd -= this.arrest_OnEnd;
            // arrest.PedResisted -= this.Abort;
            Game.Console.Print("On Abort");
            if (this.partner.IsAlive)
            {
                if (this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    this.partner.PartnerPed.Task.ClearAll();
                }

                // Once completed, holster gun and set anims back to normal
                this.partner.PartnerPed.AnimGroup = this.partner.PartnerPed.Model.ModelInfo.AnimGroup;
                this.partner.PartnerPed.Task.PlayAnimSecondaryUpperBody("gunstance_2_copm_idle", "cop", 1.0f, false);
                this.partner.PartnerPed.SetWeapon(Weapon.Unarmed);
            }
        }

        public override EBehaviorState Run()
        {
            /*
            if (this.arrest.Suspect.Wanted.IsCuffed)
            {
                this.Abort();
            }
            */
            //if (partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.goto))
            if (this.target == null || !this.target.Exists())
            {
                this.Abort();
                return EBehaviorState.Failed;
            }

            if (this.partner.PartnerPed.Position.DistanceTo(this.target.Position) < 5)
            {
                if (this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
                {
                    // Once near the target, set the partner's anims to the "cop_search" ones and have them do a gun animation
                    this.partner.PartnerPed.AnimGroup = "move_cop_search";
                    
                    this.partner.PartnerPed.Task.ClearAll();

                    covering = true;

                    this.partner.PartnerPed.SetWeapon(Weapon.Handgun_Glock);
                    this.partner.PartnerPed.Animation.Play(new AnimationSet("cop"), "copm_arrest_ground", 1.0f, AnimationFlags.Unknown06 | AnimationFlags.Unknown09 | AnimationFlags.Unknown11);
                }
            }
            if (covering)
            {
                // While covering, check they're not too far from the target.  If the target has moved away, cancel it
                if (this.partner.PartnerPed.Position.DistanceTo2D(this.target.Position) > 10)
                {
                    this.Abort();
                    return EBehaviorState.Success;
                }
            }
            

            return this.BehaviorState;
        }
        private void arrest_OnEnd(object sender)
        {
            this.Abort();
        }
    }
}