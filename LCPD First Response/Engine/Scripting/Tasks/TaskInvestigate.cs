namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.GUI;

    /// <summary>
    /// Makes the ped wander around its current position and look around
    /// </summary>
    class TaskInvestigate : PedTask
    {
        private string[,] lookAroundAnims = new string[,] { { "amb@security_idles_b", "idle_hear_noise" }, { "amb@security_idles_c", "idle_lookaround_a" }, { "missemergencycall", "idle_lookaround_b" } };
        private string[,] swatAnims = new string[,] { { "misselizabeta3", "idle" }, { "misselizabeta3", "crchsignal_moveout" }, { "missswat_assault", "crchsignal_roger" }, { "missswat_assault", "crchsignal_attention" }, { "missswat_assault", "crchsignal_stop" } };

        private string anim;
        private string animset;
        private bool hasReported;
        private bool isWandering;
        private bool animPlayed;
        private bool radioAssigned;
        private int timeToWander;
        private bool seekingCover;

        private ETaskInvestigateAction action;

        private enum ETaskInvestigateAction
        {
            Wander,
            Investigate,
            InCover
        }

        public TaskInvestigate() : base(ETaskID.Investigate)
        {
        }

        public TaskInvestigate(int timeOut) : base(ETaskID.Investigate, timeOut)
        {
        }

        public override void MakeAbortable(CPed ped)
        {
            SetTaskAsDone();
        }

        public override void Initialize(CPed ped)
        {
            base.Initialize(ped);

            if (ped.Exists())
            {
                GenerateNewAnims(ped);

                this.action = ETaskInvestigateAction.Wander;

                if (ped.PedSubGroup == EPedSubGroup.Noose)
                {
                    timeToWander = 1000 * (Common.GetRandomValue(1, 3));
                }
                else
                {
                    timeToWander = 1000 * (Common.GetRandomValue(1, 5));
                }

                this.isWandering = true;
                DelayedCaller.Call(delegate { this.isWandering = false; }, this, this.timeToWander);
            }
        }

        private void GenerateNewAnims(CPed ped)
        {
            if (ped.PedSubGroup == EPedSubGroup.Noose)
            {
                int random = Common.GetRandomValue(0, this.swatAnims.GetLength(0));
                this.animset = this.swatAnims[random, 0];
                this.anim = this.swatAnims[random, 1];
            }
            else
            {
                int random = Common.GetRandomValue(0, this.lookAroundAnims.GetLength(0));
                this.animset = this.lookAroundAnims[random, 0];
                this.anim = this.lookAroundAnims[random, 1];
            }
        }

        public override void Process(CPed ped)
        {
            // If wander task not active

            if (!ped.Exists())
            {
                this.MakeAbortable(ped);
                return;
            }

            if (this.action == ETaskInvestigateAction.Wander)
            {
                if (ped.PedSubGroup == EPedSubGroup.Noose)
                {
                    if (this.isWandering)
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexCombatRetreatSubtask))
                        {
                            ped.APed.TaskCombatRetreatSubtask(CPlayer.LocalPlayer.Ped.APed);

                            if (!ped.IsAmbientSpeechPlaying)
                            {
                                if (Common.GetRandomBool(0, 25, 1))
                                {
                                    ped.SayAmbientSpeech("COVER_ME");
                                }
                            }
                        }
                    }
                    else
                    {
                        this.action = ETaskInvestigateAction.Investigate;
                    }
                }
                else
                {
                    if (this.isWandering)
                    {
                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.Wander))
                        {
                            TaskWander taskWander = new TaskWander(this.timeToWander);
                            taskWander.AssignTo(ped, ETaskPriority.MainTask);

                            if (Common.GetRandomBool(0, 3, 1))
                            {
                                DelayedCaller.Call (delegate { if (ped.Exists()) ped.SayAmbientSpeech("LEAVE_CAR_BEGIN_SEARCH"); }, Common.GetRandomValue(250, 1500));
                            }
                        }
                    }
                    else
                    {
                        this.action = ETaskInvestigateAction.Investigate;
                    }
                }
            }
            else if (this.action == ETaskInvestigateAction.Investigate)
            {
                if (ped.PedSubGroup == EPedSubGroup.Noose)
                {
                    if (!seekingCover)
                    {
                        if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekCover))
                        {
                            ped.Task.ClearAll();
                            GTA.Native.Function.Call("TASK_SEEK_COVER_FROM_PED", ped.Handle, CPlayer.LocalPlayer.Ped.Handle, 20000);
                            seekingCover = true;
                            Vector3 pos = ped.Position.Around(10.0f);
                            //GTA.Native.Function.Call("TASK_SEEK_COVER_FROM_POS", ped.Handle, ped.Position.X, ped.Position.Y, ped.Position.Z, 0);
                            //GTA.Native.Function.Call("TASK_PUT_CHAR_DIRECTLY_INTO_COVER", ped.Handle, pos.X, pos.Y, pos.Z, -1);
                        }
                    }
                    else
                    {
                        if (!ped.IsAmbientSpeechPlaying)
                        {
                            if (Common.GetRandomBool(0, 300, 1))
                            {
                                ped.SayAmbientSpeech("COVER_ME");
                            }
                        }

                        if (ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewUseCover))
                        {
                            // They are in cover
                            this.action = ETaskInvestigateAction.InCover;
                            return;
                        }
                    }
                }
                else
                {
                    if (!this.hasReported)
                    {
                        if (!radioAssigned)
                        {
                            TaskWalkieTalkie taskWalkieTalkie = new TaskWalkieTalkie("WALKIE_TALKIE");
                            DelayedCaller.Call(delegate { taskWalkieTalkie.AssignTo(ped, ETaskPriority.MainTask); }, Common.GetRandomValue(0, 2000));
                            radioAssigned = true;
                        }

                        this.hasReported = true;
                    }
                    else
                    {
                        if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkieTalkie))
                        {
                            if (!ped.Animation.isPlaying(new AnimationSet(this.animset), this.anim))
                            {
                                ped.Task.PlayAnimation(new AnimationSet(this.animset), this.anim, 4.0f);
                                animPlayed = true;
                            }
                        }
                    }
                }
            }
            else if (this.action == ETaskInvestigateAction.InCover)
            {
                if (!ped.Animation.isPlaying(new AnimationSet(this.animset), this.anim))
                {
                    ped.Task.PlayAnimSecondaryUpperBody(this.anim, this.animset, 4.0f, false);
                    GenerateNewAnims(ped);
                }
            }

            /*
            if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.Wander))
            {
                // If not yet wandered, start
                if (!this.isWandering)
                {
                    int wanderDuration = Common.GetRandomValue(1, 5);
                    TaskWander taskWander = new TaskWander(wanderDuration * 1000);
                    taskWander.AssignTo(ped, ETaskPriority.MainTask);
                    this.isWandering = true;
                }
                // If already wandered, play lookaround anim
                else
                {
                    if (!this.hasReported)
                    {
                        // TODO: Walkie-Talkie (done by sam <3)
                        Log.Debug("Process: Subgroup is: " + ped.PedSubGroup, this);
                        if (!radioAssigned)
                        {
                            if (ped.PedSubGroup != EPedSubGroup.Noose)
                            {
                                //ped.Task.UseMobilePhone(4000);
                                TaskWalkieTalkie taskWalkieTalkie = new TaskWalkieTalkie("WALKIE_TALKIE");
                                DelayedCaller.Call(delegate { taskWalkieTalkie.AssignTo(ped, ETaskPriority.MainTask); }, Common.GetRandomValue(0, 2000));
                                radioAssigned = true;
                            }
                        }
                        else
                        {
                            ped.SetWeapon(Weapon.Rifle_M4);
                            ped.BlockWeaponSwitching = true;
                            ped.CanSwitchWeapons = false;
                        }
                        this.hasReported = true;
                    }
                    //else if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexUseMobilePhone))
                    else if (!ped.Intelligence.TaskManager.IsTaskActive(ETaskID.WalkieTalkie))
                    {
                        if (ped.PedSubGroup == EPedSubGroup.Noose)
                        {
                            if (!ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexNewUseCover) && !ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexSeekCover))
                            {
                                GTA.Native.Function.Call("TASK_SEEK_COVER_FROM_PED", ped.Handle, CPlayer.LocalPlayer.Ped.Handle, 20000);
                            }
                        }
                        else
                        {
                            if (!ped.Animation.isPlaying(new AnimationSet(this.animset), this.anim))
                            {
                                ped.Task.PlayAnimation(new AnimationSet(this.animset), this.anim, 4.0f);
                                animPlayed = true;
                            }
                        }
                    }
                }
            }
             * */
        }


        public override string ComponentName
        {
            get { return "TaskInvestigate"; }
        }
    }
}
