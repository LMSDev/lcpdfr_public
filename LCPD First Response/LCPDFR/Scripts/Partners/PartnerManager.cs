namespace LCPD_First_Response.LCPDFR.Scripts.Partners
{
    using System.Collections.Generic;
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;

    /// <summary>
    /// The partner groups.
    /// </summary>
    enum EPartnerGroup
    {
        /// <summary>
        /// All groups.
        /// </summary>
        All = 1,

        /// <summary>
        /// Main partner (and player) group.
        /// </summary>
        PrimaryGroup = 10,

        /// <summary>
        /// The two assisting partners.
        /// </summary>
        SecondaryGroup = 20,
    }

    [ScriptInfo("PartnerManager", true)]
    class PartnerManager : GameScript
    {
        private const int MaximumNumberOfPartners = 3;
        private static EPartnerGroup SelectedGroup;

        public PartnerManager()
        {
            this.Partners = new List<Partner>(MaximumNumberOfPartners);
        }

        public List<Partner> Partners { get; private set; }

        public bool CanPartnerBeAdded
        {
            get
            {
                return this.Partners.Count < MaximumNumberOfPartners;
            }
        }

        public bool HasActivePartner
        {
            get
            {
                return this.Partners.Count > 0 && this.Partners.Any(partner => partner.IsAlive);
            }
        }

        public static bool IsValidPed(CPed ped)
        {
            return ped != null && ped.Exists() && ped.IsAliveAndWell && ped.PedGroup == EPedGroup.Cop
                   && ped.GetPedData<PedDataCop>().IsFreeForAction(ECopState.Blocker, null);
        }

        public bool AddPartner(CPed ped)
        {
            Log.Caller();

            if (IsValidPed(ped))
            {
                this.SetPlayerGroupSettings();

                Partner partner = new Partner(ped, this.GetFreePartnerGroup());
                partner.PartnerFreed += this.partner_PartnerFreed;
                this.Partners.Add(partner);

                return true;
            }

            return false;
        }

        public void ExecuteTask(EPartnerGroup partnerGroup, Partner.PartnerTask action)
        {
            foreach (Partner partner in this.GetPartnersByGroup(partnerGroup))
            {
                partner.ExecuteTask(action);
            }
        }

        /// <summary>
        /// Returns a <see cref="EPartnerGroup"/> value indicating the highest possible free partner group.
        /// </summary>
        /// <returns>The partner group.</returns>
        public EPartnerGroup GetFreePartnerGroup()
        {
            Log.Caller();
            bool hasPrimary = this.Partners.Any(partner => partner.PartnerGroup == EPartnerGroup.PrimaryGroup);
            return hasPrimary ? EPartnerGroup.SecondaryGroup : EPartnerGroup.PrimaryGroup;
        }

        /// <summary>
        /// Returns a <see cref="EPartnerGroup"/> value indicating the currently selected group (from QAM).
        /// </summary>
        /// <returns>The partner group.</returns>
        public EPartnerGroup GetSelectedPartnerGroup()
        {
            return SelectedGroup;
        }

        public void SetPartnerGroup(EPartnerGroup group)
        {
            SelectedGroup = group;
        }

        public Partner[] GetPartnersByGroup(EPartnerGroup partnerGroup)
        {
            if (partnerGroup == EPartnerGroup.All) return this.Partners.ToArray();
            return this.Partners.Where(partner => partner.PartnerGroup == partnerGroup).ToArray();
        }

        public bool IsPartner(CPed ped)
        {
            Log.Caller();

            return this.Partners.Any(partner => partner.PartnerPed == ped);
        }

        public void MoveToPosition(EPartnerGroup partnerGroup, Vector3 position)
        {
            foreach (Partner partner in this.GetPartnersByGroup(partnerGroup))
            {
                partner.MoveToPosition(position);
            }
        }

        private void SetPlayerGroupSettings()
        {
            //CPlayer.LocalPlayer.Group.FollowStatus = 1;
            //CPlayer.LocalPlayer.Group.FormationSpacing = 3.5f;
            //CPlayer.LocalPlayer.Group.Formation = 5;
            CPlayer.LocalPlayer.Group.SeparationRange = 9999f;
        }

        private void partner_PartnerFreed(Partner partner)
        {
            Log.Caller();

            partner.PartnerFreed -= this.partner_PartnerFreed;
            this.Partners.Remove(partner);

            // Now that we have lost a partner, reset all groups.
            foreach (Partner p in Partners)
            {
                p.ResetGroup();
            }
        }
    }
}