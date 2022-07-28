namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.Engine.Scripting.Native;

    /// <summary>
    /// The taser feature.
    /// </summary>
    [ScriptInfo("Taser", true)]
    internal class Taser : GameScript
    {
        /// <summary>
        /// All models that should show no taser.
        /// </summary>
        private string[] noHolsterModels = { "M_Y_SWAT", "M_Y_NHELIPILOT", "M_M_FATCOP_01", "M_Y_CLUBFIT", "IG_FRANCIS_MC" };

        /// <summary>
        /// The holstered offset.
        /// </summary>
        private Vector3 holsteredOffset = new Vector3(0.055f, 0.125f, 0.155f);

        /// <summary>
        /// The holstered angle.
        /// </summary>
        private Vector3 holsteredAngle = new Vector3(2.268f, 3.426f, 0f);

        /// <summary>
        /// The ammo of the handgun.
        /// </summary>
        private int handgunAmmo;

        /// <summary>
        /// The handgun weapon.
        /// </summary>
        private Weapon handgunWeapon;

        /// <summary>
        /// Whether the taser's model is shown.
        /// </summary>
        private bool showModel;

        /// <summary>
        /// Whether the taser is holstered.
        /// </summary>
        private bool taserHolstered;

        /// <summary>
        /// The taser object.
        /// </summary>
        private GTA.Object taserObject;

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleFireGun))
            {
                if (CPlayer.LocalPlayer.Ped.Weapons.Current == Weapon.Misc_Unused0 && !this.taserHolstered)
                {
                    if (!CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleReloadGun) && !CPlayer.LocalPlayer.Ped.IsInMeleeCombat)
                    {
                        //if (CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexPlayerGun))
                        //{
                            AudioHelper.PlayActionSound("TASER_DEPLOY");

                            CPed ped = CPlayer.LocalPlayer.Ped.Intelligence.GetTargetedPed();

                            Stats.UpdateStat(Stats.EStatType.TaserFired, 1);

                            // Needs delayed as the damage doesn't take effect immediately.
                            DelayedCaller.Call(delegate
                            {
                                if (ped != null && ped.Exists())
                                {
                                    if (ped.IsRagdoll)
                                    {
                                        ped.ApplyForceRelative(new Vector3(0, 0, -0.5f), new Vector3(0, 0, 0));
                                        ped.DropCurrentWeapon();
                                        ped.HandleAudioAnimEvent("PAIN_HIGH");

                                        // TODO: Tase event/flag
                                        ped.PedData.ComplianceChance += 50;
                                        ped.PedData.HasBeenTased = true;
                                    } 
                                }
                            }, 400);
                        //}
                    }
                }
            }

            // Disable taser in vehicle
            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                if (!this.taserHolstered && this.taserObject != null && this.taserObject.Exists())
                {
                    this.SwapTaserStateCallback(null);
                }
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.HolsterTaser))
            {
                // If taser is not holstered
                if (!this.taserHolstered)
                {
                    // If first usage, save weapon and ammo
                    if (this.handgunWeapon == Weapon.Unarmed)
                    {
                        this.handgunAmmo = CPlayer.LocalPlayer.Ped.Weapons.inSlot(WeaponSlot.Handgun).Ammo;
                        this.handgunWeapon = CPlayer.LocalPlayer.Ped.Weapons.inSlot(WeaponSlot.Handgun);
                    }

                    // Player mustn't be aiming at the moment
                    if (!CPlayer.LocalPlayer.Ped.IsAiming)
                    {
                        if (this.taserObject == null || !this.taserObject.Exists())
                        {
                            this.taserObject = World.CreateObject("lcpdfr_taser", CPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0f, 0f, 5f)));
                        }

                        if (this.taserObject != null && this.taserObject.Exists())
                        {
                            // Play holster animation
                            this.taserObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Spine, Vector3.Zero, Vector3.Zero);
                            string animation = "holster";
                            if (CPlayer.LocalPlayer.Ped.IsDucking)
                            {
                                animation = "holster_crouch";
                            }

                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody(animation, "gun@deagle", 4.0f, false, 0, 0, 0, -1);
                            Stats.UpdateStat(Stats.EStatType.TaserEquipped, 1);
                            DelayedCaller.Call(this.SwapTaserStateCallback, 400);
                        }
                    }

                    // Verify model settings
                    this.CheckShowModels();
                }
                else
                {
                    // Taser already holstered
                    // If no weapon present
                    if (CPlayer.LocalPlayer.Ped.Weapons.Current == Weapon.Unarmed)
                    {
                        if (this.taserObject == null || !this.taserObject.Exists())
                        {
                            this.taserObject = World.CreateObject("lcpdfr_taser", CPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0f, 0f, 5f)));
                        }

                        if (this.taserObject != null && this.taserObject.Exists())
                        {
                            // Attach
                            this.taserObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Pelvis, this.holsteredOffset, this.holsteredAngle);
                            string animation = "unholster";

                            if (CPlayer.LocalPlayer.Ped.IsDucking)
                            {
                                animation = "unholster_crouch";
                            }

                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody(animation, "gun@deagle", 4.0f, false, 0, 0, 0, -1);
                            DelayedCaller.Call(this.SwapTaserStateCallback, 400);

                            // TODO: Ammo shit
                            this.handgunAmmo = CPlayer.LocalPlayer.Ped.Weapons.inSlot(WeaponSlot.Handgun).Ammo;
                            this.handgunWeapon = CPlayer.LocalPlayer.Ped.Weapons.inSlot(WeaponSlot.Handgun);
                        }
                    }
                    else
                    {
                        // Player has a weapon, but is not aiming
                        if (!CPlayer.LocalPlayer.Ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskSimpleAimGun))
                        {
                            string animation = "holster";
                            if (CPlayer.LocalPlayer.Ped.IsDucking)
                            {
                                animation = "holster_crouch";
                            }

                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody(animation, "gun@deagle", 4.0f, false, 0, 0, 0, -1);
                            DelayedCaller.Call(this.SwapTaserStateCallback, 400);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called after the the state has changed.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void SwapTaserStateCallback(object[] parameter)
        {
            if (this.taserHolstered)
            {
                //this.taserObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Spine, Vector3.Zero, Vector3.Zero); // with sniper's model we don't need to attach to the hand.
                this.handgunWeapon = CPlayer.LocalPlayer.Ped.Weapons.inSlot(WeaponSlot.Handgun);
                this.handgunAmmo = CPlayer.LocalPlayer.Ped.Weapons.inSlot(WeaponSlot.Handgun).Ammo;
                GTA.Native.Function.Call("GIVE_WEAPON_TO_CHAR", (GTA.Ped)CPlayer.LocalPlayer.Ped, 8, 20, 1);
                this.taserHolstered = false;

                // Red dot
                CPlayer.LocalPlayer.Ped.SetTaserLight(true, true, true);

                // Hide taser for the time being
                this.taserObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Spine, Vector3.Zero, Vector3.Zero);

                DelayedCaller.Call(this.TaserTimer, 500);
            }
            else
            {
                this.HolsterTaser();
                CPlayer.LocalPlayer.Ped.Weapons.inSlot(WeaponSlot.Handgun).Remove();
                GTA.Native.Function.Call("GIVE_WEAPON_TO_CHAR", (GTA.Ped)CPlayer.LocalPlayer.Ped, Convert.ToInt32(this.handgunWeapon), this.handgunAmmo, 1);
                GTA.Native.Function.Call("SET_CURRENT_CHAR_WEAPON", (GTA.Ped)CPlayer.LocalPlayer.Ped, 0);
            }
        }

        /// <summary>
        /// The taser timer callback.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void TaserTimer(object[] parameter)
        {
            if (!this.taserHolstered)
            {
                if (CPlayer.LocalPlayer.Ped.Weapons.Current == Weapon.Misc_Unused0)
                {
                    // good, they have the actual taser weapon
                }
                else
                {
                    // bad, they have the taser object but another weapon.
                    // remove the taser
                    CPlayer.LocalPlayer.Ped.Weapons.inSlot(WeaponSlot.Handgun).Remove();
                    GTA.Native.Function.Call("GIVE_WEAPON_TO_CHAR", (GTA.Ped)CPlayer.LocalPlayer.Ped, Convert.ToInt32(this.handgunWeapon), this.handgunAmmo, 1);
                    GTA.Native.Function.Call("SET_CURRENT_CHAR_WEAPON", (GTA.Ped)CPlayer.LocalPlayer.Ped, 0);
                    this.HolsterTaser();
                }
            }
        }

        /// <summary>
        /// Holsters the taser.
        /// </summary>
        private void HolsterTaser()
        {
            CPlayer.LocalPlayer.Ped.SetTaserLight(false, true, true);

            if (this.taserObject != null && this.taserObject.Exists())
            {
                if (this.showModel)
                {
                    this.taserObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Pelvis, this.holsteredOffset, this.holsteredAngle);
                }
                else
                {
                    this.taserObject.AttachToPed(CPlayer.LocalPlayer.Ped, Bone.Spine, Vector3.Zero, Vector3.Zero);
                }
            }

            this.taserHolstered = true;
        }

        /// <summary>
        /// This method simply checks whether or not we should show the taser model on the player's character when holstered
        /// as some models don't have a suitable space for it to be shown at (.e.g SWAT because they have big bulky pouches
        /// where the animation pulls the gun from so it looks fine without showing the taser model holstered).
        /// This also checks if the model needs to use different offsets, as well as the user defined ShowHolsteredTaser setting.
        /// </summary>
        private void CheckShowModels()
        {
            // Enable by default
            this.showModel = true;

            // Check user setting
            if (Settings.ShowHolsteredTaser == false)
            {
                this.showModel = false;
            }

            // Check model setting
            foreach (string m in this.noHolsterModels)
            {
                if (CPlayer.LocalPlayer.Ped.Model == m)
                {
                    this.showModel = false;
                }
            }

            // Adjust offsets for nb's TIT police
            if (CPlayer.LocalPlayer.Ped.Model == "F_Y_HOOKER_03" || CPlayer.LocalPlayer.Ped.Model == "F_Y_STRIPPERC01" || CPlayer.LocalPlayer.Ped.Model == "F_Y_STRIPPERC02")
            {
                this.holsteredOffset = new Vector3(0.09f, 0.05f, 0.16f);
            }
            else
            {
                this.holsteredOffset = new Vector3(0.055f, 0.125f, 0.155f);
            }
        }
    }
}