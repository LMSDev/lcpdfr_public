namespace LCPD_First_Response.LCPDFR.Scripts.Scenarios
{
    using System;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Scripting;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Scenarios;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Input;

    internal class ScenarioPlayerAssault : Scenario, IAmbientScenario, ICanOwnEntities, IPedController
    {
        private bool didntDropWeapon;
        private bool hasReachedPlayer;
        private float startHeading;

        private bool isFighting;
        private CPed suspect;

        public override string ComponentName
        {
            get
            {
                return "ScenarioPlayerAssault";
            }
        }

        public override void Initialize()
        {
            Vector3 position = CPlayer.LocalPlayer.Ped.Position.Around(Common.GetRandomValue(15, 45));
            position = World.GetNextPositionOnPavement(position);
            this.suspect = new CPed("M_Y_COWBOY_01", position, EPedGroup.Criminal);
            if (this.suspect.Exists())
            {
                this.suspect.RequestOwnership(this);
                this.suspect.Intelligence.RequestForAction(EPedActionPriority.RequiredByScript, this);
                this.suspect.BlockPermanentEvents = true;
                this.suspect.Weapons.AssaultRifle_AK47.Ammo = 4000;
                this.suspect.SetWeapon(Weapon.Rifle_AK47);
                this.suspect.Task.GoToCharAiming(CPlayer.LocalPlayer.Ped, 4f, 8f);
            }
            else
            {
                this.MakeAbortable();
            }
        }

        public override void Process()
        {
            if (this.suspect.Position.DistanceTo(CPlayer.LocalPlayer.Ped.Position) < 6 && !this.hasReachedPlayer)
            {
                CPlayer.LocalPlayer.CanControlCharacter = false;
                CPlayer.LocalPlayer.Ped.CanSwitchWeapons = false;
                CameraHelper.FocusGameCamOnPed(this.suspect, true, 1500, 3500);
                this.hasReachedPlayer = true;
                DelayedCaller.Call(delegate { CPlayer.LocalPlayer.Ped.Task.PlayAnimation(new AnimationSet("busted"), "idle_2_hands_up", 4.0f, AnimationFlags.Unknown12 | AnimationFlags.Unknown06 | AnimationFlags.Unknown11 | AnimationFlags.Unknown09); }, this, 250);
                DelayedCaller.Call(delegate { CPlayer.LocalPlayer.CanControlCharacter = true; }, this, 4500);
                DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox("You are aimed at by a criminal. Cooperating will significantly increase your chance to survive. You can always try to fight by simply moving."); }, this, 500);
            }

            if (this.hasReachedPlayer)
            {
                if (CPlayer.LocalPlayer.CanControlCharacter && !this.isFighting)
                {
                    if (this.startHeading == 0)
                    {
                        this.startHeading = CPlayer.LocalPlayer.Ped.Heading;
                        if (CPlayer.LocalPlayer.Ped.Weapons.Current != Weapon.Unarmed)
                        {
                            this.suspect.Intelligence.SayText("Drop your weapon, officer!", 4000);
                            DelayedCaller.Call(delegate { TextHelper.PrintFormattedHelpBox("Press ~KEY_ARREST~ to drop your weapon."); }, this, 1000);
                            DelayedCaller.Call(
                                delegate
                                {
                                    if (CPlayer.LocalPlayer.Ped.Weapons.Current != Weapon.Unarmed)
                                    {
                                        this.didntDropWeapon = true;
                                    }
                                }, 
                                this, 
                                5000);
                        }
                    }

                    if (CPlayer.LocalPlayer.Ped.Weapons.Current != Weapon.Unarmed)
                    {
                        if (KeyHandler.IsKeyDown(ELCPDFRKeys.Arrest))
                        {
                            CPlayer.LocalPlayer.Ped.Weapons.Current.Ammo = 0;
                            CPlayer.LocalPlayer.Ped.DropCurrentWeapon();
                            DelayedCaller.Call(delegate { CPlayer.LocalPlayer.Ped.SetWeapon(Weapon.Unarmed); }, this, 100);
                        }
                    }

                    if (!CPlayer.LocalPlayer.Ped.IsStandingStill || !Common.IsNumberInRange(CPlayer.LocalPlayer.Ped.Heading, this.startHeading, 5f, 5f, 360)
                        || this.didntDropWeapon)
                    {
                        this.isFighting = true;

                        // Only allow regaining control if player actually moved.
                        if (!CPlayer.LocalPlayer.Ped.IsStandingStill || !Common.IsNumberInRange(CPlayer.LocalPlayer.Ped.Heading, this.startHeading, 5f, 5f, 360))
                        {
                            CPlayer.LocalPlayer.Ped.CanSwitchWeapons = true;
                            CPlayer.LocalPlayer.Ped.Task.ClearAll();
                        }

                        DelayedCaller.Call(
                            delegate
                            {
                                this.suspect.Intelligence.SayText("You asked for it!", 3500);
                                this.suspect.Task.AlwaysKeepTask = true;
                                this.suspect.Task.ShootAt(CPlayer.LocalPlayer.Ped, ShootMode.Continuous, 2000);
                                DelayedCaller.Call(delegate { if (this.suspect.Exists()) { this.suspect.Task.FightAgainst(CPlayer.LocalPlayer.Ped, int.MaxValue); } }, this, 2100);
                            },
                            this,
                            300);
                    }
                }

                if (this.isFighting && CPlayer.LocalPlayer.Ped.HasBeenDamagedBy(this.suspect))
                {
                    if (CPlayer.LocalPlayer.Ped.IsDead || !this.suspect.IsAliveAndWell)
                    {
                        this.MakeAbortable();
                    }
                }
            }
        }

        public bool CanScenarioStart(Vector3 position)
        {
            return false;
        }

        public bool CanBeDisposedNow()
        {
            return false;
        }

        public void PedHasLeft(CPed ped)
        {
            
        }
    }
}