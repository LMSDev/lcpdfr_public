namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System.Collections.Generic;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;
    using LCPD_First_Response.LCPDFR.Input;
    using LCPD_First_Response.Engine.Scripting;

    /// <summary>
    /// Manages the pullover script instances.
    /// </summary>
    [ScriptInfo("PulloverManager", true)]
    internal class PulloverManager : GameScript
    {
        /// <summary>
        /// The pullover script.
        /// </summary>
        private Pullover pulloverScript;

        /// <summary>
        /// Gets a value indicating whether a pullover is running.
        /// </summary>
        public bool IsPulloverRunning
        {
            get
            {
                return this.pulloverScript != null;
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (!CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                return;
            }

            if (KeyHandler.IsKeyDown(ELCPDFRKeys.PulloverStart))
            {
                // Doesn't work when player is chasing someone
                if (CPlayer.LocalPlayer.Ped.PedData.CurrentChase != null)
                {
                    // Instead, it plays the PULL_OVER_WARNING or MEGAPHONE_FOOT_PURSUIT megaphone speech
                    if (!CPlayer.LocalPlayer.Ped.IsAmbientSpeechPlaying && CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                    {
                        Chase chase = CPlayer.LocalPlayer.Ped.PedData.CurrentChase;
                        foreach (CPed criminal in chase.Criminals)
                        {
                            if (criminal != null && criminal.Exists())
                            {
                                if (criminal.IsAliveAndWell && criminal.IsOnScreen)
                                {
                                    if (CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.IsHelicopter)
                                    {
                                        if (criminal.IsInVehicle() && criminal.IsArmed() || criminal.IsShooting)
                                        {
                                            CPlayer.LocalPlayer.Ped.SayAmbientSpeech("COP_HELI_MEGAPHONE_WEAPON", "M_Y_HELI_COP");
                                        }
                                        else
                                        {
                                            CPlayer.LocalPlayer.Ped.SayAmbientSpeech("COP_HELI_MEGAPHONE", "M_Y_HELI_COP");
                                        }
                                    }
                                    else
                                    {
                                        if (criminal.IsInVehicle())
                                        {
                                            if (criminal.CurrentVehicle.Speed > 5)
                                            {
                                                CPlayer.LocalPlayer.Ped.SayAmbientSpeech("PULL_OVER_WARNING");
                                            }
                                            else
                                            {
                                                CPlayer.LocalPlayer.Ped.SayAmbientSpeech("GET_OUT_OF_CAR_MEGAPHONE");
                                            }
                                        }
                                        else
                                        {
                                            CPlayer.LocalPlayer.Ped.SayAmbientSpeech("MEGAPHONE_FOOT_PURSUIT");
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    return;
                }

                if (this.pulloverScript != null)
                {
                    if (!this.pulloverScript.IsWaitingForStart)
                    {
                        this.pulloverScript.End();
                        this.pulloverScript = null;
                    }

                    return;
                }


                if (CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                {
                    Vector3 position = CPlayer.LocalPlayer.Ped.GetOffsetPosition(new Vector3(0, 8, 0));
                    CVehicle vehicle = CVehicle.GetClosestVehicle(EVehicleSearchCriteria.DriverOnly, 5f, position);

                    if (vehicle != null && vehicle.Exists())
                    {
                        if (!vehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsEmergencyServicesVehicle)
                            && !vehicle.Flags.HasFlag(EVehicleFlags.DisablePullover))
                        {
                            if (KeyHandler.IsKeyboardKeyStillDown(Keys.ControlKey))
                            {
                                this.pulloverScript = new Pullover(vehicle, true);
                            }
                            else
                            {
                                this.pulloverScript = new Pullover(vehicle);
                            }

                            this.pulloverScript.OnEnd += new OnEndEventHandler(this.pulloverScript_OnEnd);
                            Main.ScriptManager.RegisterScriptInstance(this.pulloverScript);

                            Stats.UpdateStat(Stats.EStatType.PulloversStarted, 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            if (this.pulloverScript != null)
            {
                this.pulloverScript.End();
            }
        }

        /// <summary>
        /// Called when the pullover has ended.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void pulloverScript_OnEnd(object sender)
        {
            if (this.pulloverScript == (Pullover)sender)
            {
                this.pulloverScript = null;
            }
        }
    }
}