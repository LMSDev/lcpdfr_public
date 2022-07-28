namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    /// <summary>
    /// Makes a ped wander around as if it was drunk.
    /// </summary>
    internal class TaskWalkDrunk : PedTask
    {
        /// <summary>
        /// The ped.
        /// </summary>
        private CPed ped;

        /// <summary>
        /// The timer to stagger around.
        /// </summary>
        private Timers.Timer timer;

        /// <summary>
        /// Whether ragdoll will be turned off soon.
        /// </summary>
        private bool willTurnOffSoon;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskWalkDrunk"/> class.
        /// </summary>
        public TaskWalkDrunk() : base(ETaskID.WalkDrunk)
        {
        }

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public override string ComponentName
        {
            get
            {
                return "TaskWalkDrunk";
            }
        }

        /// <summary>
        /// Called when the task should be aborted. Remember to call SetTaskAsDone here!
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void MakeAbortable(CPed ped)
        {
            this.SetTaskAsDone();
            this.TurnOffRagdoll();
        }

        /// <summary>
        /// This is called immediately after the task was assigned to a ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            this.ped = ped;
            this.SetPedDrunkAndApplyNMMessage();
            this.timer = new Timers.Timer(3000, this.StaggerAroundCallback);
            this.timer.Start();
        }

        /// <summary>
        /// The process.
        /// </summary>
        /// <param name="ped">
        /// The ped.
        /// </param>
        public override void Process(CPed ped)
        {
            if (ped.IsInVehicle)
            {
                this.MakeAbortable(ped);
                return;
            }

            // Detect whether ped is laying on the ground and we have to turn off ragdoll so ped can get up
            // Get position of the head
            Vector3 pos = this.ped.GetBonePosition(Bone.Head);

            // Get the height of the ground. We use +2 to ensure we're getting the right ground (just below the ped)
            float height = pos.Z + 2;
            float groundZ = World.GetGroundZ(new Vector3(pos.X, pos.Y, height));

            // If head.Z - ground.Z is lower than zero make positive
            float height2 = pos.Z - groundZ;
            if (height2 < 0)
            {
                height2 = height2 * -1.00f;
            }

            // If the difference between the head and the ground is more than 1.2 the ped is not on the ground
            bool onGround = !(height2 > 1.2f);

            // If ped is on the ground, turn off ragdoll in a few seconds
            if (onGround)
            {
                if (!this.willTurnOffSoon)
                {
                    DelayedCaller.Call(
                        delegate
                        {
                            this.TurnOffRagdoll();
                            this.willTurnOffSoon = false;
                        },
                        this,
                        4000);
                    this.willTurnOffSoon = true;
                }
            }
            else
            {
                // If not on the ground and ragdoll isn't turned on, we switch ped to ragdoll again
                if (!this.ped.IsRagdoll)
                {
                    // Skip when being cuffed
                    if (this.ped.Intelligence.TaskManager.IsTaskActive(ETaskID.BeingBusted))
                    {
                        if (this.ped.Wanted.ArrestedBy != null && this.ped.Wanted.ArrestedBy.Exists())
                        {
                            if (this.ped.Wanted.ArrestedBy.Intelligence.TaskManager.IsTaskActive(ETaskID.CuffPed))
                            {
                                if (this.ped.IsRagdoll)
                                {
                                    this.TurnOffRagdoll();
                                }

                                return;
                            }
                        }
                    }

                    // However if ped is being arrested we have only a random chance to turn on ragdoll, so the drunk guy will reach the car eventually
                    if (!this.ped.Wanted.IsBeingArrested && !this.ped.Wanted.IsBeingArrestedByPlayer && !this.ped.Wanted.IsBeingFrisked
                        && !this.ped.Wanted.IsCuffed && !this.ped.Wanted.IsStopped)
                    {
                        bool turnOnRagdoll = Common.GetRandomBool(0, 40, 1);
                        if (turnOnRagdoll)
                        {
                            this.SetPedDrunkAndApplyNMMessage();
                        }
                    }
                    else
                    {
                        bool turnOnRagdoll = Common.GetRandomBool(0, 200, 1);
                        if (turnOnRagdoll)
                        {
                            this.SetPedDrunkAndApplyNMMessage();

                            // Simulate timercallback
                            this.StaggerAroundCallback(null);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Callback to make the ped walk around.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void StaggerAroundCallback(params object[] parameter)
        {
            // If ragdoll is on, stagger around
            if (this.ped.Exists())
            {
                if (this.ped.IsRagdoll)
                {
                    this.MakePedStaggerToPosition(this.ped.Position.Around(5));
                }
            }
        }

        /// <summary>
        /// Applies the drunk naturla motion message.
        /// </summary>
        private void SetPedDrunkAndApplyNMMessage()
        {
            GTA.Native.Function.Call("SET_PED_IS_DRUNK", (GTA.Ped)this.ped, true);
            GTA.Native.Function.Call("SWITCH_PED_TO_RAGDOLL", (GTA.Ped)this.ped, 0, 65534, 1, 1, 1, 0);
            GTA.Native.Function.Call("CREATE_NM_MESSAGE", 1, 79);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 89, 8.70);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 98, 0.60);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 81, 8.40);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 82, 0.70);
            GTA.Native.Function.Call("SET_NM_MESSAGE_INT", 85, 65535);
            GTA.Native.Function.Call("SET_NM_MESSAGE_BOOL", 95, 1);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 101, 0.80);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 102, 999.00);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 84, 1.40);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 83, 1.95);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 94, 1.00);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 110, 0.00);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 111, 0.10);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 112, 0.10);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 108, 0.00);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 113, 0.60);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 109, 0.20);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 91, 0.10);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 93, 0.10);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 106, -0.30);
            GTA.Native.Function.Call("SEND_NM_MESSAGE", (GTA.Ped)this.ped);
        }

        /// <summary>
        /// Makes the ped stagger to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        private void MakePedStaggerToPosition(Vector3 position)
        {
            GTA.Native.Function.Call("CREATE_NM_MESSAGE", 1, 119);
            GTA.Native.Function.Call("SET_NM_MESSAGE_VEC3", 121, position.X, position.Y, position.Z);
            GTA.Native.Function.Call("SET_NM_MESSAGE_FLOAT", 122, 0.20);
            GTA.Native.Function.Call("SEND_NM_MESSAGE", (GTA.Ped)this.ped);
        }

        /// <summary>
        /// Turns off ragdoll.
        /// </summary>
        private void TurnOffRagdoll()
        {
            GTA.Native.Function.Call("CREATE_NM_MESSAGE", 0, 79);
            GTA.Native.Function.Call("SEND_NM_MESSAGE", (GTA.Ped)this.ped);
            GTA.Native.Function.Call("SWITCH_PED_TO_ANIMATED", (GTA.Ped)this.ped, 0);
        }
    }
}