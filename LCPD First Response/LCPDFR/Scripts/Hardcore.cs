namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System.Collections.Generic;
    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Events;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;
    using GTA;
    using System;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    /// <summary>
    /// LCPDFR's awesome hardcore mode
    /// </summary>
    [ScriptInfo("Hardcore", true)]
    internal class Hardcore : GameScript
    {
        
        // Currently Unused
        /* 
        private List<Weapon> Pistols = new List<Weapon>();
        private List<Weapon> Shotguns = new List<Weapon>();
        private List<Weapon> Rifles = new List<Weapon>();
        private List<Weapon> SMGs = new List<Weapon>();
        */

        /// <summary>
        /// The last reported value for the player's health
        /// </summary>
        private int playerHealth = 0;

        /// <summary>
        /// The last reported value for the player's armour
        /// </summary>
        private int playerArmour = 0;

        /// <summary>
        /// The change in the player's health
        /// </summary>
        private int healthChange = 0;

        /// <summary>
        /// The change in the player's armour
        /// </summary>
        private int armourChange = 0;

        /// <summary>
        /// Whether or not the player is injured
        /// </summary>
        public static bool playerInjured;

        /// <summary>
        /// Whether or not hardcore is enabled
        /// </summary>
        public static bool hardcoreEnabled;

        /// <summary>
        /// Whether or not a medic has been assigned to treat the player
        /// </summary>
        private bool medicAssigned;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Hardcore"/> class.
        /// </summary>
        public Hardcore()
        {
            // Currently Unused
            /* 
            Pistols.Add(Weapon.Handgun_Glock);
            Pistols.Add(Weapon.Handgun_DesertEagle);
            Pistols.Add(Weapon.TBOGT_Pistol44);
            Pistols.Add(Weapon.TLAD_Automatic9mm);

            Shotguns.Add(Weapon.TLAD_AssaultShotgun);
            Shotguns.Add(Weapon.TLAD_SawedOffShotgun);
            Shotguns.Add(Weapon.TBOGT_NormalShotgun);
            Shotguns.Add(Weapon.Shotgun_Baretta);
            Shotguns.Add(Weapon.Shotgun_Basic);

            Rifles.Add(Weapon.TBOGT_AdvancedMG);
            Rifles.Add(Weapon.Rifle_M4);
            Rifles.Add(Weapon.Rifle_AK47);

            SMGs.Add(Weapon.SMG_MP5);
            SMGs.Add(Weapon.SMG_Uzi);
            SMGs.Add(Weapon.TBOGT_GoldenSMG);
            SMGs.Add(Weapon.TBOGT_AssaultSMG);
            */

            if (Settings.HardcoreEnabled) hardcoreEnabled = true;

            if (hardcoreEnabled)
            {
                playerArmour = CPlayer.LocalPlayer.Ped.Armor;
                playerHealth = CPlayer.LocalPlayer.Ped.Health;
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!hardcoreEnabled)
            {
                // If hardcore mode isn't enabled, don't process any further.
                return;
            }

            if (playerArmour != CPlayer.LocalPlayer.Ped.Armor)
            {
                // Armour value has changed, update and track it.
                armourChange = (playerArmour - CPlayer.LocalPlayer.Ped.Armor);
                playerArmour = CPlayer.LocalPlayer.Ped.Armor;
            }

            if (playerHealth != CPlayer.LocalPlayer.Ped.Health)
            {
                // Health value has changed, update and track it.
                healthChange = (playerHealth - CPlayer.LocalPlayer.Ped.Health);
                playerHealth = CPlayer.LocalPlayer.Ped.Health;
            }

            foreach (Weapon w in Enum.GetValues(typeof(Weapon)))
            {
                if (w != Weapon.Misc_AnyWeapon && w != Weapon.None)
                {
                    if (CPlayer.LocalPlayer.Ped.HasBeenDamagedBy(w))
                    {
                        // If the player wasn't runover or anything, apply ragdoll.
                        if (w != Weapon.Misc_Fall && w != Weapon.Misc_RunOverByCar && w != Weapon.Misc_RammedByCar && w != Weapon.Unarmed) RagdollPlayer(Convert.ToInt32(healthChange + armourChange) * 50, true, 0);

                        if (healthChange > 0)
                        {
                            // If the player's health changed, make it do double the damage.
                            DamagePlayer(healthChange);
                                 
                            if (playerHealth < 40)
                            {
                                if (!playerInjured)
                                {
                                    // If the player's health is less than 40 and the player isn't already injured, make them injured
                                    // When the player is injured, they can't use weapons and use an injured animation set.

                                    // Blood
                                    Natives.SetCharBleeding(CPlayer.LocalPlayer.Ped, true);

                                    // Animation set
                                    Natives.RequestAnims("move_injured_generic");
                                    CPlayer.LocalPlayer.Ped.SetAnimGroup("move_injured_generic");

                                    // Speech
                                    if (!CPlayer.LocalPlayer.Ped.IsAmbientSpeechPlaying) CPlayer.LocalPlayer.Ped.SayAmbientSpeech("BEEN_SHOT");

                                    // Injured flag
                                    playerInjured = true;
                                    medicAssigned = false;

                                    // Weapon blocking
                                    CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed);
                                    CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = true; 
                                }
                            }
                        }

                        if (armourChange > 0)
                        {
                            // If the player's armour was changed, do double the damage.
                            DamagePlayer(armourChange);
                        }

                        // Important: clear the last weapon damage so we can check for new o
                        Natives.ClearCharLastWeaponDamage(CPlayer.LocalPlayer.Ped);

                        healthChange = 0;                 
                        armourChange = 0;
                    }
                }
            }


            // This is some basic code for getting medics to treat the player, although it'd be better to use ambulance backup instead.
            if (playerInjured)
            {
                if (!medicAssigned)
                {
                    // If no medic has been assigned to treat the player, look for one
                    foreach (CPed ped in Pools.PedPool.GetAll())
                    {
                        if (ped.Exists())
                        {
                            if (ped != CPlayer.LocalPlayer.Ped)
                            {
                                if (ped.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 20.0f)
                                {
                                    // If we've found a nearby medic, get them to treat the player
                                    if (ped.Model == "M_Y_PMEDIC")
                                    {
                                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.TreatPed))
                                        {
                                            // If not already treating, make them treat the player
                                            ped.Task.ClearAll();
                                            // ped.AttachBlip();
                                            TaskTreatPed taskCuffPed = new TaskTreatPed(CPlayer.LocalPlayer.Ped);
                                            taskCuffPed.AssignTo(ped, ETaskPriority.MainTask);
                                            medicAssigned = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (CPlayer.LocalPlayer.Ped.Health >= 40)
                {
                    // If at any point the player's health goes above 40, disable the effects of hardcore.
                    
                    // Animation set
                    Natives.RequestAnims(CPlayer.LocalPlayer.AnimGroup);
                    CPlayer.LocalPlayer.Ped.SetAnimGroup(CPlayer.LocalPlayer.AnimGroup);

                    // Flags
                    playerInjured = false;
                    medicAssigned = false;

                    // Bleeding and weapon block
                    CPlayer.LocalPlayer.Ped.BlockWeaponSwitching = false;
                    Natives.SetCharBleeding(CPlayer.LocalPlayer.Ped, false);
                }
            }

        }

        /// <summary>
        /// Applies bullet impact ragdoll to the player
        /// </summary>
        /// <param name="duration">How long the ragdoll is applied for</param>
        /// <param name="reachForWound">Whether or not the player reaches for their wounds</param>
        public void RagdollPlayer(int duration, bool reachForWound, int timeBeforeReachForWound = 0)
        {
            var ragdoll = CPlayer.LocalPlayer.Ped.Euphoria.BeingShot;
            ragdoll.ReachForWound = reachForWound;
            ragdoll.TimeBeforeReachForWound = timeBeforeReachForWound;
            ragdoll.Start(duration);
        }

        /// <summary>
        /// Subtracts a specified amount from the player's armour, then their health if necessary
        /// </summary>
        /// <param name="amount">The damage amount to be applied</param>
        private void DamagePlayer(int amount)
        {
            if (CPlayer.LocalPlayer.Ped.IsAlive)
            {
                Natives.DamageChar(CPlayer.LocalPlayer.Ped, amount);
            }
        }
    }
}
