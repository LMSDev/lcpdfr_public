namespace LCPD_First_Response.Engine.Scripting.Tasks
{
    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.LCPDFR;
    using LCPD_First_Response.LCPDFR.Scripts;

    class TaskFlashlight : PedTask
    {
        private readonly bool onlyWhenAiming;

        private readonly Bone pedBone;

        private readonly float offset;

        private readonly float coneRange;

        private readonly bool useCamDirection;

        private readonly Vector3 colour;

        private readonly float diffusion;

        private readonly float intensity;

        private readonly float envIntensity;

        private readonly float envDiffusion;

        private readonly float envRange;

        public TaskFlashlight() : base(ETaskID.Flashlight)
        {
            this.onlyWhenAiming = true;
            this.pedBone = Bone.RightHand;
            this.offset = 0.5f;
            this.coneRange = 6.0f;
            this.useCamDirection = false;
            this.colour = new Vector3(1.0f, 1.0f, 1.0f);
            this.diffusion = 10f;
            this.intensity = 20f;
            this.envIntensity =30.92f;
            this.envRange = 20.0f;
            this.envDiffusion = 25.0f;
        }

        public TaskFlashlight(float coneRange, System.Drawing.Color colour, float diffusion, float intensity, float envRange, float envDiffusion, float envIntensity) : base(ETaskID.Flashlight)
        {
            this.onlyWhenAiming = true;
            this.pedBone = Bone.RightHand;
            this.offset = 0.5f;
            this.coneRange = coneRange;
            this.useCamDirection = false;
            this.colour = new Vector3(colour.R / 255, colour.G / 255, colour.B / 255);
            this.diffusion = diffusion;
            this.intensity = intensity;
            this.envDiffusion = envDiffusion;
            this.envIntensity = envIntensity;
            this.envRange = envRange;
        }

        public TaskFlashlight(bool evenWhenNotAiming, Bone pedBone, float offset, float coneRange, bool useGameDirection, System.Drawing.Color colour) : base(ETaskID.Flashlight)
        {
            this.onlyWhenAiming = !evenWhenNotAiming;
            this.pedBone = pedBone;
            this.offset = offset;
            this.coneRange = coneRange;
            this.useCamDirection = useGameDirection;
            this.colour = new Vector3(colour.R/255, colour.G/255, colour.B/255);
            this.diffusion = 10f;
            this.intensity = 20f;
            this.envIntensity = 30.92f;
            this.envRange = 20.0f;
            this.envDiffusion = 25.0f;
        }

        public override string ComponentName
        {
            get
            {
                return "TaskFlashlight";
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the flashlight should be disabled at night. This kills the task.
        /// </summary>
        public bool DisableAtDay { get; set; }

        public override void MakeAbortable(CPed ped)
        {
            this.SetTaskAsDone();
        }

        public override void Process(CPed ped)
        {
            bool isAming = ped.IsAiming || ped.Intelligence.TaskManager.IsInternalTaskActive(EInternalTaskID.CTaskComplexGun);
            if ((this.onlyWhenAiming && isAming) || !this.onlyWhenAiming)
            {
                // Use vector math to get direction of arm, then use polynomal regression to remove bias (thanks for the great maths back then Dr. Lechtenberg!).
                Vector3 rightHand = ped.GetBonePosition(Bone.RightHand);
                Vector3 rightUpperam = ped.GetBonePosition(Bone.RightUpperarm);
                Vector3 armDirection = rightHand - rightUpperam;
                double zDir = armDirection.Z * 1.8777;

                Vector3 vector1 = ped.Direction;
                vector1 = this.useCamDirection ? Game.CurrentCamera.Direction : new Vector3(vector1.X, vector1.Y, (float)zDir);

                Vector3 vector3 = ped.GetBonePosition(this.pedBone);
                Vector3 vector4 = this.colour;
                LightHelper.DrawLightCone(vector3, vector1, vector4, this.coneRange, this.diffusion, this.intensity);

                Vector3 vector1Normalized = vector1;
                vector1Normalized.Normalize();
                vector3 = vector3 + (vector1Normalized * this.offset);

                Vector3 vector2 = (vector3 + (vector1Normalized * this.coneRange)) - vector3;
                //vector2.X = 5.0f;
                //float y = vector2.Y;
                //float z = vector2.Z;
                //vector2.Y = 5.0f;//z;
                //vector2.Z = 5.0f;//-1.0f; //-y;
                //vector2.Normalize();

                LightHelper.DrawLight(vector3, vector1, vector4, this.envIntensity, this.envRange, this.envDiffusion, true);
                //LightHelper.DrawLight(vector3, vector1, vector4, 10000f, 10.0f, 1.0f, true);
                //LightHelper.DrawLight(vector3, vector1, vector4, 10.0f, 0.25f, 25.0f, true);
                //vector2.Z = 1.0f;
                //LightHelper.DrawLight(vector3, vector1, vector4, 30.92f, 20.0f, 25.0f, true, vector2);  
            }

            if (this.DisableAtDay)
            {
                if (!Globals.IsNightTime)
                {
                    this.MakeAbortable(ped);
                }
            }
        }
    }
}