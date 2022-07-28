namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    using System;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;

    class BehaviorSupportPullover : Behavior
    {
        private readonly Partner partner;

        private readonly Pullover pullover;

        private bool isGoingToCar;

        public BehaviorSupportPullover(Partner partner, Pullover pullover)
        {
            this.partner = partner;
            this.pullover = pullover;

            this.partner.PartnerPed.EnsurePedHasWeapon();

            // Always go to the pavement so partner doesn't block the traffic
            GTA.Vector3 offset = new GTA.Vector3(2, -2, 0);
            if (pullover.Vehicle.GetSideOfStreetVehicleIsAt() == EStreetSide.Left)
            {
                offset = new GTA.Vector3(-2, -4, 0);
            }

            this.partner.PartnerPed.Task.GoToCoordAiming(pullover.Vehicle.Driver.GetOffsetPosition(offset), EPedMoveState.Run, pullover.Vehicle.Driver.Position);
            pullover.OnEnd += new BaseScript.OnEndEventHandler(this.Pullover_OnEnd);
            pullover.PedResisted += new Action(this.Pullover_PedResisted);
            this.partner.PartnerPed.SetFlashlight(true, true, false);

            // Set state a little later so there's enough time for the aiming task to be applied
            DelayedCaller.Call(delegate { this.isGoingToCar = true; }, this, 2000);
        }

        public override void OnAbort()
        {
            this.pullover.OnEnd -= new BaseScript.OnEndEventHandler(this.Pullover_OnEnd);
            this.pullover.PedResisted -= new Action(this.Pullover_PedResisted);
        }

        public override EBehaviorState Run()
        {
            if (this.isGoingToCar)
            {
                if (this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleHitWall))
                {
                    Log.Debug("Process: Hit wall, holding position", this);
                    if (this.pullover.Vehicle.HasDriver)
                    {
                        // Partner hit a wall, so we are going to stop and aim
                        this.partner.PartnerPed.Task.ClearAll();
                        this.partner.PartnerPed.Task.GoToCharAiming(pullover.Vehicle.Driver, 100f, 100f);
                        this.isGoingToCar = false;
                        return this.BehaviorState;
                    }
                }

                if (!this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGoToPointAiming))
                {
                    if (this.pullover.Vehicle.HasDriver)
                    {
                        // Extend distance when going to passenger window
                        float distance = 3f;
                        if (pullover.Vehicle.GetSideOfStreetVehicleIsAt() == EStreetSide.Right)
                        {
                            distance = 5f;
                        }

                        this.partner.PartnerPed.Task.GoToCharAiming(pullover.Vehicle.Driver, distance, 9f);
                    }

                    this.isGoingToCar = false;
                }

            }

            return this.BehaviorState;
        }

        /// <summary>
        /// Called when the pullover has ended.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void Pullover_OnEnd(object sender)
        {
            if (this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
            {
                this.partner.PartnerPed.Task.ClearAll();
            }

            this.Abort();
        }

        /// <summary>
        /// Called when the ped resisted during the pullover.
        /// </summary>
        private void Pullover_PedResisted()
        {
            if (this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGoToPointAiming) ||
                this.partner.PartnerPed.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekEntityAiming))
            {
                this.partner.PartnerPed.Task.ClearAll();
            }

            this.Abort();
        }
    }
}