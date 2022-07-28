namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.Engine.Scripting.Native;

    /// <summary>
    /// The faster walking feature where the player can hold a key down (ALT) to walk faster.
    /// </summary>
    [ScriptInfo("FastWalk", true)]
    internal class FastWalk : GameScript
    {
        /// <summary>
        /// Models which can't use the walk fast anim
        /// </summary>
        private string[] blockModels = { "M_M_FATCOP_01" };

        /// <summary>
        /// If the fast walk is currently active
        /// </summary>
        private bool active;

        /// <summary>
        /// If the fast walk anim has been applied
        /// </summary>
        private bool animApplied;

        /// <summary>
        /// If the weapon run anim has been applied
        /// </summary>
        private bool runAnimApplied;

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();
            if (!Hardcore.playerInjured)
            {
                // If the player is injured, don't continue
                if (KeyHandler.IsKeyStillDown(ELCPDFRKeys.WalkFaster))
                {
                    active = true;
                    if (!animApplied)
                    {
                        Natives.RequestAnims("move_cop_search");
                        CPlayer.LocalPlayer.Ped.SetAnimGroup("move_cop_search");
                        Natives.BlockCharAmbientAnims(CPlayer.LocalPlayer.Ped, true);
                        Natives.BlockCharGestureAnims(CPlayer.LocalPlayer.Ped, true);
                        animApplied = true;

                        Stats.UpdateStat(Stats.EStatType.FastWalkUsed, 1);
                    }
                }
                else
                {
                    if (active)
                    {
                        Natives.RequestAnims(CPlayer.LocalPlayer.AnimGroup);
                        Natives.SetAnimGroupForChar(CPlayer.LocalPlayer.Ped, CPlayer.LocalPlayer.AnimGroup);
                        Natives.BlockCharAmbientAnims(CPlayer.LocalPlayer.Ped, false);
                        Natives.BlockCharGestureAnims(CPlayer.LocalPlayer.Ped, false);
                        active = false;
                        animApplied = false;
                    }
                }

                if (CPlayer.LocalPlayer.Ped.Speed > 2)
                {
                    if (!runAnimApplied)
                    {
                        if (!CPlayer.LocalPlayer.Ped.IsInVehicle && !CPlayer.LocalPlayer.Ped.IsSwimming && !CPlayer.LocalPlayer.Ped.IsAiming)
                        {
                            if (!GTA.Native.Function.Call<bool>("IS_CHAR_STOPPED", Game.LocalPlayer.Character))
                            {
                                if (CPlayer.LocalPlayer.Ped.Weapons.Current == Weapon.Handgun_DesertEagle || CPlayer.LocalPlayer.Ped.Weapons.Current == Weapon.Handgun_Glock || CPlayer.LocalPlayer.Ped.Weapons.Current == Weapon.Misc_Unused0)
                                {
                                    if (!CPlayer.LocalPlayer.Ped.IsDucking)
                                    {
                                        if (!CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("gun@cops"), "pistol_partial_a"))
                                        {
                                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("pistol_partial_a", "gun@cops", 4.0f, true);
                                            runAnimApplied = true;
                                        }
                                    }
                                }
                                else if (CPlayer.LocalPlayer.Ped.Weapons.Current == Weapon.Rifle_M4 || CPlayer.LocalPlayer.Ped.Weapons.Current == Weapon.Shotgun_Baretta)
                                {
                                    if (CPlayer.LocalPlayer.Ped.IsDucking)
                                    {
                                        if (!CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("gun@cops"), "swat_rifle_crouch"))
                                        {
                                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("swat_rifle_crouch", "gun@cops", 4.0f, true);
                                            runAnimApplied = true;
                                        }
                                    }
                                    else
                                    {
                                        if (!CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("gun@cops"), "swat_rifle"))
                                        {
                                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("swat_rifle", "gun@cops", 4.0f, true);
                                            runAnimApplied = true;

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (runAnimApplied)
                    {
                        if (CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("gun@cops"), "pistol_partial_a"))
                        {
                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("pistol_partial_a", "gun@cops", 4.0f, false);
                        }

                        if (CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("gun@cops"), "swat_rifle_crouch"))
                        {
                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("swat_rifle_crouch", "gun@cops", 4.0f, false);
                        }

                        if (CPlayer.LocalPlayer.Ped.Animation.isPlaying(new AnimationSet("gun@cops"), "swat_rifle"))
                        {
                            CPlayer.LocalPlayer.Ped.Task.PlayAnimSecondaryUpperBody("swat_rifle", "gun@cops", 4.0f, false);
                        }

                        runAnimApplied = false;
                    }
                }
            }
        }
    }
}