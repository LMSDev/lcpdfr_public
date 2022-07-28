namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    using System;
    using System.Diagnostics;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;

    class Partner : BaseComponent, ICanOwnEntities, IPedController
    {
        private bool isFreeing;

        /// <summary>
        /// The partner ped.
        /// </summary>
        private CPed partner;

        /// <summary>
        /// The partner group.
        /// </summary>
        private EPartnerGroup partnerGroup;

        /// <summary>
        /// The partner intelligence.
        /// </summary>
        private PartnerIntelligence intelligence;

        public event Action<Partner> PartnerFreed;

        public delegate void PartnerTask(Partner partner);

        public Partner(CPed ped, EPartnerGroup partnerGroup)
        {
            Log.Caller();
            //Log.Info("Group: " + partnerGroup.ToString(), this);
            this.partner = ped;
            this.partnerGroup = partnerGroup;
            this.intelligence = new PartnerIntelligence(this);
            this.Setup();
            this.partner.Intelligence.RegisterExtendedIntelligence(this.intelligence, 5);
        }

        /// <summary>
        /// Gets a value indicating whether the partner exists at all.
        /// </summary>
        public bool Exists
        {
            get
            {
                return this.partner != null && this.partner.Exists();
            }
        }

        public PartnerIntelligence Intelligence
        {
            get
            {
                return this.intelligence;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the partner is alive.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                return this.Exists && this.partner.IsAliveAndWell;
            }
        }

        public EPartnerGroup PartnerGroup
        {
            get
            {
                return this.partnerGroup;
            }
        }

        /// <summary>
        /// Gets the partner ped.
        /// </summary>
        public CPed PartnerPed
        {
            get
            {
                return this.partner;
            }
        }

        public void Arrest(CPed ped)
        {
            if (!this.IsAlive) return;
            this.intelligence.ArrestPed(ped);
        }

        public void ExecuteTask(PartnerTask task)
        {
            task.Invoke(this);
        }

        public void FollowInVehicle(CVehicle vehicle)
        {
            if (!this.IsAlive) return;
            this.intelligence.FollowInVehicle(vehicle);
        }

        public void HoldAtGunpoint(CPed ped)
        {
            if (!this.IsAlive) return;
            this.intelligence.HoldPedAtGunpoint(ped);
        }

        public void CoverPedTarget(CPed ped)
        {
            if (!this.IsAlive) return;
            this.intelligence.CoverTarget(ped);
        }

        public void HoldPosition()
        {
            if (!this.IsAlive) return;
            this.intelligence.HoldPosition();
        }

        public void MoveToPosition(Vector3 position)
        {
            if (!this.IsAlive) return;
            this.intelligence.MoveToPosition(position);
        }

        public void Regroup()
        {
            if (!this.IsAlive) return;
            this.intelligence.ForceIdle();
        }

        public void ResetGroup()
        {
            Log.Caller();
            this.partnerGroup = LCPDFR.Main.PartnerManager.GetFreePartnerGroup();
            Log.Debug("Active group is now: " + this.partnerGroup, this);
        }

        public void Reset()
        {
            this.partner.ReleaseOwnership(this);
            this.partner.LeaveGroup();
            this.partner.DeleteBlip();
            this.partner.NoLongerNeeded();
            CPlayer.LocalPlayer.Group.RemoveMember(this.partner);

            this.partner.RequestOwnership(this);
            this.partner.Task.ClearAll();
            this.partner.Intelligence.TaskManager.ClearTasks();
            this.partner.Task.GoTo(CPlayer.LocalPlayer.Ped);
        }

        private void Setup()
        {
            Log.Caller();
            this.partner.PedData.DefaultWeapon = Weapon.Handgun_Glock;
            this.partner.GetPedData<PedDataCop>().RequestPedAction(ECopState.Blocker, this);
            this.partner.RequestOwnership(this);

            this.partner.BlockPermanentEvents = false;
            this.partner.WillDoDrivebys = false;
            this.partner.WillUseCarsInCombat = true;
            this.partner.WillFlyThroughWindscreen = true;
            this.partner.SenseRange = 75.0f;
            this.partner.SetPathfinding(true, true, true);
            this.partner.SetWaterFlags(false, false, true);
            this.partner.Accuracy = 100;
            this.partner.Armor = 200;
            this.partner.MaxHealth = 400;
            this.partner.Health = 400;
            this.partner.SetShootRate(100);

            // TODO: Manually implement group functions.
            //CPlayer.LocalPlayer.Group.AddMember(this.partner);

            // Kill all running tasks.
            this.partner.Task.ClearAll();
            this.partner.Intelligence.TaskManager.ClearTasks();
            this.partner.Task.GoTo(CPlayer.LocalPlayer.Ped);
            this.partner.AttachBlip(sync: false);
            this.partner.Blip.Scale = 0.5f;
            this.partner.Blip.Friendly = true;
            this.partner.Blip.Display = BlipDisplay.MapOnly;
            this.partner.Blip.Name = CultureHelper.GetText("PARTNER_PARTNER");

            this.intelligence.PartnerDied += this.intelligence_PartnerDied;
        }

        private void Free()
        {
            Log.Caller();
            this.isFreeing = true;
            this.intelligence.PartnerDied -= this.intelligence_PartnerDied;
            this.partner.ReleaseOwnership(this);
            this.partner.GetPedData<PedDataCop>().ResetPedAction(this);
            this.partner.LeaveGroup();
            this.partner.DeleteBlip();
            this.partner.NoLongerNeeded();
            CPlayer.LocalPlayer.Group.RemoveMember(this.partner);

            if (this.PartnerFreed != null)
            {
                this.PartnerFreed(this);
            }

            this.isFreeing = false;
        }

        private void intelligence_PartnerDied()
        {
            if (!this.isFreeing)
            {
                this.Free();
            }
        }

        /// <summary>
        /// Called when a ped has 'left' from the current controller, e.g. due to a more important event. Use this to clean up things.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public void PedHasLeft(CPed ped)
        {
            Log.Caller();

            // For some reason we lost control.
            if (this.partner == ped)
            {
                Log.Warning("PedHasLeft: Lost control of partner ped", this);

                if (!this.isFreeing)
                {
                    this.Free();
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
                return "Partner";
            }
        }
    }
}