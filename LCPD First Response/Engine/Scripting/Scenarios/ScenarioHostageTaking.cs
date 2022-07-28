namespace LCPD_First_Response.Engine.Scripting.Scenarios
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    internal class ScenarioHostageTaking : Scenario, ICanOwnEntities, IPedController
    {
        // hostage_struggle: Geisel fasst sich an Kehle und wird festgehalten
        // hostage: geisel wird gehalten
        // hostage_taker_peek: griff vom geiselnehmer
        // hostage_peek: geisel wird gehalten
        // hostage_let_go: rolle zur seite (man wird fallengelassen)
        // hostage_taker_duck: geiselnemher duckt sich hinter geisel und zielt mit waffe
        // hostage_duck: geisel duckt sich
        // execute_rom: geisel stirbt
        // hostage_taker: griff vom geiselnehmer

        private AnimationSet animSet;
        private bool hasFired;
        private CPed hostage;
        private CPed taker;

        public ScenarioHostageTaking(CPed taker, CPed hostage)
        {
            this.hostage = hostage;
            this.taker = taker;
        }

        public override void Initialize()
        {
            this.animSet = new AnimationSet("missroman12");

            // Setup position and heading
            this.taker.FreezePosition = true;
            Vector3 position = new Vector3(0, 0.5f, -0.5f);
            this.hostage.Position = this.taker.GetOffsetPosition(position);
            this.hostage.Heading = this.taker.Heading;

            // Ensure hostage taker has weapon
            this.taker.PedData.DefaultWeapon = Weapon.Handgun_Glock;
            this.taker.EnsurePedHasWeapon();

            // Hostage taker animations
            // Unknown12 = Animation can be aborted (e.g. to aim), Unknown09 = balance upper body (so animation as well as euphoria). No lower body animation
            // Unknown11 = Allow movement while animation is running
            // Unknown02 = Freeze position
            // Unknown05 = Loop (restart)
            // Unknown06 = animation doesn't end

            //AnimationFlags { None = 0, RootMotion = 1, Unknown02 = 2, LockPelvisOnZAxis = 4, StayInEndFrameRotation = 8, Loop = 16, StayInEndFramePose = 32, EndAtCurrentWorldPosition = 64, UpperBodyOnly_Unknown = 128, UpperBodyOnly = 256, IdleAndBlockControls = 512, AllowPlayerRotation = 1024, Unknown12 = 2048 };.

            // Build up two sequences
            TaskSequence takerSequence = new TaskSequence();
            takerSequence.AddTask.PlayAnimation(this.animSet, "hostage_taker", 9.0f, AnimationFlags.Unknown06);
            takerSequence.AddTask.PlayAnimation(this.animSet, "Hostage_taker_struggle", 9.0f, AnimationFlags.Unknown06);
            takerSequence.AddTask.PlayAnimation(this.animSet, "Hostage_taker_fire", 9.0f, AnimationFlags.Unknown06);
            takerSequence.AddTask.PlayAnimation(this.animSet, "hostage_taker_peek", 9.0f, AnimationFlags.Unknown06);
            takerSequence.AddTask.PlayAnimation(this.animSet, "hostage_taker", 9.0f, AnimationFlags.Unknown06);
            takerSequence.AddTask.PlayAnimation(this.animSet, "Hostage_taker_shot", 8.0f, AnimationFlags.Unknown06);
            takerSequence.Perform(this.taker);

            TaskSequence hostageSequence = new TaskSequence();
            hostageSequence.AddTask.PlayAnimation(this.animSet, "hostage", 9.0f, AnimationFlags.Unknown06);
            hostageSequence.AddTask.PlayAnimation(this.animSet, "hostage_struggle", 9.0f, AnimationFlags.Unknown06);
            hostageSequence.AddTask.PlayAnimation(this.animSet, "hostage_peek", 9.0f,  AnimationFlags.Unknown06);
            hostageSequence.AddTask.PlayAnimation(this.animSet, "hostage_peek", 9.0f, AnimationFlags.Unknown06);
            hostageSequence.AddTask.PlayAnimation(this.animSet, "hostage", 9.0f, AnimationFlags.Unknown06);
            hostageSequence.AddTask.PlayAnimation(this.animSet, "Hostage_let_go", 8.0f);
            hostageSequence.Perform(this.hostage);

            DelayedCaller.Call(this.ClearSequence, 29000, takerSequence);
            DelayedCaller.Call(this.ClearSequence, 29000, hostageSequence);
            DelayedCaller.Call(this.End, 29500);

            //taskTimed = new TaskTimed(6500, true, Kill, null);
            //taskTimed.AssignTo();
        }

        public override void Process()
        {
            if (this.taker.Animation.isPlaying(this.animSet, "Hostage_taker_fire"))
            {
                Game.TimeScale = 1f;
                // Get anim time
                float time = this.taker.Animation.GetCurrentAnimationTime(this.animSet, "Hostage_taker_fire");
                if (time >= 0.40 && time < 0.42)
                {
                    FireWeaponInFront();
                    GTA.Game.Console.Print("Shoot1");
                }
                if (time >= 0.54 && time < 0.56)
                {
                    FireWeaponInFront();
                    GTA.Game.Console.Print("Shoot2");
                }
                if (time >= 0.68 && time < 0.71)
                {
                    FireWeaponInFront();
                    GTA.Game.Console.Print("Shoot3");
                }
            }
            if (this.hostage.Animation.isPlaying(this.animSet, "hostage_let_go"))
            {
                float time = this.hostage.Animation.GetCurrentAnimationTime(this.animSet, "hostage_let_go");
                if (time < 0.1 && !this.hasFired)
                {
                    Vector3 position = this.taker.GetBonePosition(Bone.Head);
                    GTA.Native.Function.Call("FIRE_PED_WEAPON", (GTA.Ped)CPlayer.LocalPlayer.Ped, position.X, position.Y, position.Z);

                    CPlayer.LocalPlayer.Ped.Task.ShootAt(this.hostage, ShootMode.AimOnly, 10000);
                    DelayedCaller.Call(this.ShootHostage, 4000);
                    this.hasFired = true;
                }

                if (time >= 1)
                {
                    this.hostage.ForceRagdoll(100, true);
                    this.hostage.Task.FleeFromChar(this.taker);
                }
            }
        }

        public void PedHasLeft(CPed ped)
        {

        }

        private void ClearSequence(object data)
        {
            TaskSequence sequence = (TaskSequence) data;
            sequence.Dispose();
        }

        private void End(object data)
        {
            this.taker.Delete();
            this.hostage.Delete();
            MakeAbortable();
        }

        private void FireWeaponInFront()
        {
            Vector3 position = this.taker.GetOffsetPosition(new Vector3(0, 10, 0));
            GTA.Native.Function.Call("FIRE_PED_WEAPON", (GTA.Ped) this.taker, position.X, position.Y, position.Z);
        }

        private void Kill(object data)
        {
            Game.TimeScale = 0.1f;
            Game.WaitInCurrentScript(3000);
            // Kill hostage
            Vector3 position = this.hostage.GetBonePosition(Bone.Head);
            GTA.Native.Function.Call("FIRE_PED_WEAPON", (GTA.Ped)this.taker, position.X, position.Y, position.Z);
            this.hostage.ForceRagdoll(4000, true);
            this.hostage.ApplyForceRelative(new Vector3(-2.5f, 0, 0));
            this.hostage.Die();
            this.taker.FreezePosition = false;
            this.taker.Task.FightAgainst(CPlayer.LocalPlayer.Ped);

            Game.WaitInCurrentScript(1000);

            position = this.taker.GetBonePosition(Bone.Head);
            GTA.Native.Function.Call("FIRE_PED_WEAPON", (GTA.Ped)CPlayer.LocalPlayer.Ped, position.X, position.Y, position.Z);
            Game.WaitInCurrentScript(4000);
            Game.TimeScale = 1;
        }

        private void ShootHostage(object data)
        {
            if (this.hostage.Exists())
            {
                if (this.hostage.IsAliveAndWell)
                {
                    if (!this.hostage.IsGettingUp)
                    {
                        // Even though is headshot is way cooler, the player will barely hit, so we aim for the spine instead
                        Vector3 position = this.hostage.Position; //this.hostage.GetBonePosition(Bone.Spine);
                        GTA.Native.Function.Call("FIRE_PED_WEAPON", (GTA.Ped)CPlayer.LocalPlayer.Ped, position.X, position.Y, position.Z);
                        CPlayer.LocalPlayer.Ped.Task.ShootAt(this.hostage, ShootMode.AimOnly, 5000);
                    }
                }
                else
                {
                    CPlayer.LocalPlayer.Ped.Task.ShootAt(this.hostage, ShootMode.Continuous, 2000);
                    return;
                }

                DelayedCaller.Call(this.ShootHostage, 1200);
            }
        }

        private void KillSuspect(object data)
        {
            Vector3 position = this.taker.GetBonePosition(Bone.Head);
            GTA.Native.Function.Call("FIRE_PED_WEAPON", (GTA.Ped)CPlayer.LocalPlayer.Ped, position.X, position.Y, position.Z);
        }

        public override string ComponentName
        {
            get { return "ScenarioHostageTaking"; }
        }
    }
}
