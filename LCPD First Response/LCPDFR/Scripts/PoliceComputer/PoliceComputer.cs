namespace LCPD_First_Response.LCPDFR.Scripts.PoliceComputer
{
    using System;
    using System.IO;
    using System.Threading;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Input;

    using Timer = LCPD_First_Response.Engine.Timers.Timer;

    /// <summary>
    /// The LCPDFR police computer.
    /// </summary>
    [ScriptInfo("PoliceComputer", true)]
    internal class PoliceComputer : GameScript
    {
        /// <summary>
        /// The background image path.
        /// </summary>
        private const string BackgroundImagePath = "lcpdfr\\mainmenu.png";

        /// <summary>
        /// Whether input is blocked.
        /// </summary>
        private bool blockInput;

        /// <summary>
        /// Whether the police computer has been closed long enough so it can be opened again.
        /// </summary>
        private bool canBeOpenedAgain;

        /// <summary>
        /// The handler of the login form.
        /// </summary>
        private LoginFormHandler loginFormHandler;

        /// <summary>
        /// The handler of the main form.
        /// </summary>
        private MainFormHandler mainFormHandler;

        /// <summary>
        /// Timer used to kill the police script.
        /// </summary>
        private Timer timerKillPoliceScript;

        /// <summary>
        /// The background image.
        /// </summary>
        private Image backgroundImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PoliceComputer"/> class.
        /// </summary>
        public PoliceComputer()
        {
            this.CanBeAccessed = true;
            this.canBeOpenedAgain = true;
            this.timerKillPoliceScript = new Timer(100, this.TimerKillPoliceScriptTick);
            this.timerKillPoliceScript.Start();
            AdvancedHookManaged.AGame.SetBlockPoliceComputerScript(true);

            // Asynchronously load background texture resource
            Thread thread = new Thread(delegate()
                {
                    if (File.Exists(BackgroundImagePath))
                    {
                        this.backgroundImage = new Image(BackgroundImagePath, Gui.Resolution.Width, Gui.Resolution.Height, 0, 0, false);
                    }
                    else
                    {
                        Log.Warning("PoliceComputer: Failed to load " + BackgroundImagePath, this);
                        this.backgroundImage = Image.EmptyImage;
                    }
                });
            thread.Start();
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            AdvancedHookManaged.AGame.SetBlockPoliceComputerScript(false);
        }

        /// <summary>
        /// Delegate used when a ped has been looked up.
        /// </summary>
        /// <param name="persona">The persona data.</param>
        public delegate void PedHasBeenLookedUpEventHandler(Persona persona);

        /// <summary>
        /// Fired when a ped has been looked up.
        /// </summary>
        public event PedHasBeenLookedUpEventHandler PedHasBeenLookedUp;

        /// <summary>
        /// Gets the background image for the police computer.
        /// </summary>
        public Image BackgroundImage
        {
            get
            {
                return this.backgroundImage;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the police computer can be accessed.
        /// </summary>
        public bool CanBeAccessed { get; set; }

        /// <summary>
        /// Gets a value indicating whether the police computer is actively being displayed.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.CanBeAccessed && !this.blockInput)
            {
                // Check for keydown
                if (KeyHandler.IsKeyDown(ELCPDFRKeys.PoliceComputer))
                {
                    // We got this keystroke, so block input for half a second to prevent rapid pressing from causing issues
                    this.blockInput = true;
                    DelayedCaller.Call(delegate { this.blockInput = false; }, this, 500);

                    if (this.IsActive)
                    {
                        this.Close();
                        return;
                    }

                    // Not when player is about to report a chase
                    if (LCPDFRPlayer.LocalPlayer.IsChasing && !(CPlayer.LocalPlayer.Ped.PedData.CurrentChase as Pursuit).HasBeenCalledIn)
                    {
                        return;
                    }


                    if (CPlayer.LocalPlayer.Ped.CurrentVehicle.IsStopped)
                    {
                        if (this.canBeOpenedAgain && CPlayer.LocalPlayer.Ped.CurrentVehicle.Model.ModelInfo.ModelFlags.HasFlag(EModelFlags.IsCopCar))
                        {
                            // Open up login form
                            this.Open();
                        }
                    }

                    // Since key was just pressed, it's likely the real computer just opened up
                    DelayedCaller.Call(delegate { this.TimerKillPoliceScriptTick(null); }, 100);
                }
            }
        }

        /// <summary>
        /// Closes the police computer.
        /// </summary>
        private void Close()
        {
            // Revert everything back to normal
            CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = false;
            CPlayer.LocalPlayer.CanControlCharacter = true;
            CPlayer.LocalPlayer.IgnoredByEveryone = false;
            CPlayer.LocalPlayer.IgnoredByAI = false;
            Game.Unpause();

            // Kill script again just to be sure
            this.KillPoliceComputerScript();
            this.ResetCamera();
            this.IsActive = false;

            // Ensure the original script didn't lock our doors
            if (CPlayer.LocalPlayer.Ped.IsInVehicle)
            {
                CPlayer.LocalPlayer.Ped.CurrentVehicle.DoorLock = DoorLock.None;
            }

            // Also make sure HUD is working
            Engine.GUI.Gui.DisplayHUD = true;

            this.loginFormHandler.LoggedIn -= new LoginFormHandler.LoggedInEventHandler(this.loginFormHandler_LoggedIn);
            this.loginFormHandler.Dispose();
            Main.FormsManager.Mouse.Enabled = false;

            if (this.mainFormHandler != null)
            {
                this.mainFormHandler.Closed -= new Engine.GUI.Form.ClosedEventHandler(this.mainFormHandler_Closed);
                this.mainFormHandler.PedHasBeenLookedUp -= new PedHasBeenLookedUpEventHandler(this.mainFormHandler_PedHasBeenLookedUp);
                this.mainFormHandler.Dispose();
            }

            // Re-enable police computer after 100ms
            this.canBeOpenedAgain = false;
            DelayedCaller.Call(delegate { this.canBeOpenedAgain = true; }, 100);

            // Ensure player has control
            DelayedCaller.Call(delegate { CPlayer.LocalPlayer.CanControlCharacter = true; }, 500);

            DelayedCaller.Call(
                delegate
                {
                    this.ResetCamera();

                    // Ensure the original script didn't lock our doors
                    if (CPlayer.LocalPlayer.Ped.IsInVehicle)
                    {
                        CPlayer.LocalPlayer.Ped.CurrentVehicle.DoorLock = DoorLock.None;
                    }
                }, 
                this, 
                500);
        }

        /// <summary>
        /// Opens the police computer.
        /// </summary>
        private void Open()
        {
            // Menu will be fullscreen, so ensure player can't do anything
            Game.PlayFrontendSound("POLICE_COMPUTER_BOOTUP");

            CPlayer.LocalPlayer.CameraControlsDisabledWithPlayerControls = false;
            CPlayer.LocalPlayer.CanControlCharacter = false;

            if (!Main.NetworkManager.IsNetworkSession)
            {
                CPlayer.LocalPlayer.IgnoredByEveryone = true;
                CPlayer.LocalPlayer.IgnoredByAI = true;
                DelayedCaller.Call(
                    delegate
                        {
                            if (this.IsActive)
                            {
                                Game.Pause();
                            }
                        },
                    200);
            }

            this.loginFormHandler = new LoginFormHandler();
            this.loginFormHandler.LoggedIn += new LoginFormHandler.LoggedInEventHandler(this.loginFormHandler_LoggedIn);
            Main.FormsManager.Mouse.Enabled = true;

            this.IsActive = true;

            Stats.UpdateStat(Stats.EStatType.PoliceComputerOpened, 1);
        }

        /// <summary>
        /// Called when the user logged in.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        private void loginFormHandler_LoggedIn(string username, string password)
        {
            this.loginFormHandler.LoggedIn -= new LoginFormHandler.LoggedInEventHandler(this.loginFormHandler_LoggedIn);
            this.loginFormHandler.Dispose();

            this.mainFormHandler = new MainFormHandler();
            this.mainFormHandler.Closed += new Engine.GUI.Form.ClosedEventHandler(this.mainFormHandler_Closed);
            this.mainFormHandler.PedHasBeenLookedUp += new PedHasBeenLookedUpEventHandler(this.mainFormHandler_PedHasBeenLookedUp);
        }

        /// <summary>
        /// Called when a ped has been looked up.
        /// </summary>
        /// <param name="persona">The persona data.</param>
        private void mainFormHandler_PedHasBeenLookedUp(Persona persona)
        {
            Stats.UpdateStat(Stats.EStatType.PedDataLookedUp, 1);

            if (this.PedHasBeenLookedUp != null)
            {
                this.PedHasBeenLookedUp(persona);
            }
        }

        /// <summary>
        /// Invoked when the mainform has been closed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void mainFormHandler_Closed(object sender)
        {
            this.Close();
        }

        /// <summary>
        /// Called every second to kill the police script.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void TimerKillPoliceScriptTick(object[] parameter)
        {
            if (this.CanBeAccessed && !this.IsActive)
            {
                if (Natives.GetNumberOfInstancesOfStreamedScript("policetest") > 0)
                {
                    if (this.canBeOpenedAgain)
                    {
                        this.Open();
                    }
                    else
                    {
                        this.KillPoliceComputerScript();
                    }
                }
            }
        }

        /// <summary>
        /// Kills the IV police computer script.
        /// </summary>
        private void KillPoliceComputerScript()
        {
            if (Natives.GetNumberOfInstancesOfStreamedScript("policetest") > 0)
            {
                // Disable IV police computer
                Natives.TerminateAllScriptsWithThisName("policeTest");

                this.ResetCamera();
            }
        }

        /// <summary>
        /// Resets the camera to default.
        /// </summary>
        private void ResetCamera()
        {
            // Directly taken from policetest.sco
            GTA.Native.Pointer charcommandptr = typeof(int);
            GTA.Native.Function.Call("BEGIN_CAM_COMMANDS", charcommandptr);
            GTA.Native.Function.Call("CLEAR_TIMECYCLE_MODIFIER");
            GTA.Native.Function.Call("SET_CAM_ACTIVE", GTA.Game.DefaultCamera);
            GTA.Native.Function.Call("SET_CAM_PROPAGATE", GTA.Game.DefaultCamera);
            GTA.Native.Function.Call("ACTIVATE_SCRIPTED_CAMS", 0, 0);
            GTA.Native.Function.Call("SET_GLOBAL_RENDER_FLAGS", 1, 1, 1, 1);
            GTA.Native.Function.Call("DESTROY_ALL_CAMS");
            GTA.Native.Function.Call("END_CAM_COMMANDS", charcommandptr);
        }
    }
}