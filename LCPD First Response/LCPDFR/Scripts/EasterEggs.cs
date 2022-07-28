namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Drawing;

    using GTA;

    using LCPD_First_Response.Engine.Input;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Scripting.Native;
    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.Engine.Scripting.Tasks;

    /// <summary>
    /// Easter egg class for fun things and notable events.
    /// </summary>
    [ScriptInfo("EE", true)]
    internal class EasterEggs : GameScript
    {
        /// <summary>
        /// The small font used.
        /// </summary>
        private GTA.Font fontSmall = new GTA.Font(18f, FontScaling.Pixel);

        /// <summary>
        /// The normal font used.
        /// </summary>
        private GTA.Font fontNormal = new GTA.Font(30f, FontScaling.Pixel);

        /// <summary>
        /// The big font used.
        /// </summary>
        private GTA.Font fontBig = new GTA.Font(60f, FontScaling.Pixel);

        /// <summary>
        /// The font used.
        /// </summary>
        private GTA.Font font;

        /// <summary>
        /// The easter egg mode.
        /// </summary>
        private EEasterEgg mode;

        /// <summary>
        /// The text to draw.
        /// </summary>
        private string text;

        /// <summary>
        /// The x and y values to draw.
        /// </summary>
        private float x = 0, y = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasterEggs"/> class.
        /// </summary>
        public EasterEggs()
        {
            DateTime time = DateTime.Now;
            if (time.Month == 9 && time.Day == 11)
            {
                this.mode = EEasterEgg.NineEleven;
                this.font = this.fontNormal;
            }
            else
            {
                this.mode = EEasterEgg.None;
                this.font = this.fontNormal;
                this.text = string.Empty;
            }

            this.RegisterConsoleCommands();

            // Drawing hook
            Engine.GUI.Gui.PerFrameDrawing += this.Gui_PerFrameDrawing;
        }

        /// <summary>
        /// The various easter egg options.
        /// </summary>
        private enum EEasterEgg
        {
            /// <summary>
            /// No mode.
            /// </summary>
            None,

            /// <summary>
            /// The birthday of Cyan.
            /// </summary>
            BirthdayCyan,

            /// <summary>
            /// The birthday of LCPDFR.
            /// </summary>
            BirthdayLCPDFR,


            /// <summary>
            /// The birthday of LMS.
            /// </summary>
            BirthdayLMS,

            /// <summary>
            /// The birthday of Sam.
            /// </summary>
            BirthdaySam,

            /// <summary>
            /// The 9-11 attacks day.
            /// </summary>
            NineEleven,
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            // Return here if no mode selected to save performance
            if (this.mode == EEasterEgg.None)
            {
                return;
            }

            CPed ped = CPlayer.LocalPlayer.Ped.Intelligence.GetClosestPed(EPedSearchCriteria.AmbientPed, 10f);
            {
                if (ped != null && ped.Exists() && ped.IsOnScreen)
                {
                    Vector2 screenPosition;
                    Vector3 worldPosition = ped.GetBonePosition(Bone.Head);

                    // Slightly above player's head
                    float distance = Game.CurrentCamera.Position.DistanceTo(ped.Position);
                    worldPosition.Z += 0.08f * distance;
                    bool ret = Engine.GUI.Gui.GetRelativeScreenPositionFromWorldPosition(worldPosition, EViewportID.CViewportGame, out screenPosition);
                    this.x = screenPosition.X;
                    this.y = screenPosition.Y;

                    // Set text
                    if (this.mode == EEasterEgg.NineEleven)
                    {
                        // Randomized text based on age
                        int value = new Random(ped.PedData.Persona.BirthDay.DayOfYear).Next(0, 6);
                        if (value == 0)
                        {
                            this.text = "All of a sudden there were people screaming. I saw people jumping out of the building.\r\n Their arms were flailing. I stopped taking pictures and started crying.";
                            this.font = this.fontSmall;
                        }
                        else if (value == 1)
                        {
                            this.text = "We will never forget";
                            this.font = this.fontSmall;
                        }
                        else if (value == 2)
                        {
                            this.text = "You can be sure that the American spirit will prevail over this tragedy.";
                            this.font = this.fontSmall;
                        }
                        else if (value == 4)
                        {
                            this.text = "Time is passing. Yet, for the United States of America, there will be no forgetting September the 11th.\r\n We will remember every rescuer who died in honor.";
                            this.font = this.fontSmall;
                        }
                        else if (value == 5)
                        {
                            this.text = "We will remember every family that lives in grief. \r\nWe will remember the fire and ash, the last phone calls, the funerals of the children.";
                            this.font = this.fontSmall;
                        }
                        else if (value == 6)
                        {
                            this.text = "The attacks of September 11th were intended to break our spirit. Instead we have emerged stronger and more unified.";
                            this.font = this.fontSmall;
                        }
                    }

                    if (this.mode == EEasterEgg.BirthdayLMS)
                    {
                        // Randomized text based on age
                        int value = new Random(ped.PedData.Persona.BirthDay.DayOfYear).Next(0, 5);
                        if (value == 0)
                        {
                            this.text = "Have you heard it already? It's LMS's birthday today!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 1)
                        {
                            this.text = "A legend was born today!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 2)
                        {
                            this.text = "Remember remember the 2nd of November... LMS's birthday!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 3)
                        {
                            this.text = "Happy Birthday LMS!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 4)
                        {
                            this.text = "Woohoo! It's LMS's birthday!";
                            this.font = this.fontSmall;
                        }
                    }
                    else if (this.mode == EEasterEgg.BirthdaySam)
                    {
                        // Randomized text based on age
                        int value = new Random(ped.PedData.Persona.BirthDay.DayOfYear).Next(0, 5);
                        if (value == 0)
                        {
                            this.text = "Have you heard it already? It's Sam's birthday today!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 1)
                        {
                            this.text = "A legend was born today!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 2)
                        {
                            this.text = "Remember remember the 11th of November... Sam's birthday!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 3)
                        {
                            this.text = "Happy Birthday Sam!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 4)
                        {
                            this.text = "Woohoo! It's Sam's birthday!";
                            this.font = this.fontSmall;
                        }
                    }
                    else if (this.mode == EEasterEgg.BirthdayCyan)
                    {
                        // Randomized text based on age
                        int value = new Random(ped.PedData.Persona.BirthDay.DayOfYear).Next(0, 5);
                        if (value == 0)
                        {
                            this.text = "Have you heard it already? It's Cyan's birthday today!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 1)
                        {
                            this.text = "A legend was born today!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 2)
                        {
                            this.text = "Remember remember the 5th of April... Cyan's birthday!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 3)
                        {
                            this.text = "Happy Birthday Cyan!";
                            this.font = this.fontSmall;
                        }
                        else if (value == 4)
                        {
                            this.text = "Woohoo! It's Cyan's birthday!";
                            this.font = this.fontSmall;
                        }
                    }
                }
                else
                {
                    this.x = 0;
                    this.y = 0;
                }
            }
        }

        /// <summary>
        /// Called every frame to draw on top of the rendered D3D frame.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The graphics event.</param>
        private void Gui_PerFrameDrawing(object sender, GraphicsEventArgs e)
        {
            if (this.x != 0 && this.y != 0)
            {
                float realX = this.x * Game.Resolution.Width;
                float realY = this.y * Game.Resolution.Height;

                e.Graphics.DrawText(this.text, realX, realY, Color.White, this.font);
            }
        }

        /// <summary>
        /// Called when the user typed EE.
        /// </summary>
        /// <param name="parameterCollection">The parameter.</param>
        [ConsoleCommand("EE")]
        private void EasterEggsConsoleCallback(GTA.ParameterCollection parameterCollection)
        {
            if (parameterCollection.Count > 0)
            {
                string s = parameterCollection[0];
                if (s.ToLower() == "lms")
                {
                    this.mode = EEasterEgg.BirthdayLMS;
                }
                else if (s.ToLower() == "sam")
                {
                    this.mode = EEasterEgg.BirthdaySam;
                }
                else if (s.ToLower() == "cyan")
                {
                    this.mode = EEasterEgg.BirthdayCyan;
                }
            }
        }
    }
}