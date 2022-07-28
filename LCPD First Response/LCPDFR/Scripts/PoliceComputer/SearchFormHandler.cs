namespace LCPD_First_Response.LCPDFR.Scripts.PoliceComputer
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.Scripting.Entities;
    using LCPD_First_Response.Engine.Timers;

    using Button = LCPD_First_Response.Engine.GUI.Button;
    using Form = LCPD_First_Response.Engine.GUI.Form;
    using Label = LCPD_First_Response.Engine.GUI.Label;
    using Main = LCPD_First_Response.LCPDFR.Main;
    using TextBox = LCPD_First_Response.Engine.GUI.TextBox;

    /// <summary>
    /// Responsible for handling all actions of the search form.
    /// </summary>
    internal class SearchFormHandler : Form
    {
        /// <summary>
        /// Whether the search is currently in progress.
        /// </summary>
        private bool isSearching;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchFormHandler"/> class.
        /// </summary>
        public SearchFormHandler()
        {
            // Create form from windows forms 'SearchForm'
            this.CreateFormWindowsForm(new SearchForm());
            this.CenterOnScreen();

            this.GetControlByName<TextBox>("textBox1").MouseDown += new MouseDownEventHandler(this.SearchFormHandler_MouseDown);
            this.GetControlByName<Button>("button1").OnClick += new Button.OnClickEventHandler(this.SearchFormHandler_OnClick);
            this.GetControlByName<Label>("label1").Text = string.Empty;

            // Get peds pulled over
            CPed[] peds = CPlayer.LocalPlayer.LastPedPulledOver;
            if (peds != null)
            {
                string names = string.Empty;
                foreach (CPed ped in peds)
                {
                    names += ped.PedData.Persona.FullName + ", ";
                }

                this.GetControlByName<Label>("label2").Text = "Last ID checked: " + names;
            }
            else
            {
                this.GetControlByName<Label>("label2").Text = string.Empty;
            }
        }

        /// <summary>
        /// Fired when a ped has been looked up.
        /// </summary>
        public event PoliceComputer.PedHasBeenLookedUpEventHandler PedHasBeenLookedUp;

        /// <summary>
        /// Draws the form. We override this for custom drawing.
        /// </summary>
        /// <param name="graphics">Graphics to draw on.</param>
        public override void Draw(GTA.Graphics graphics)
        {
            // Dark blue background
            graphics.DrawRectangle(new RectangleF(0, 0, Gui.Resolution.Width, Gui.Resolution.Height), Color.DarkBlue);

            // Draw background image
            Main.PoliceComputer.BackgroundImage.Draw(graphics);

            base.Draw(graphics);
        }

        /// <summary>
        /// Called when a key is down.
        /// </summary>
        /// <param name="key">The key down.</param>
        /// <returns>Return false if key shouldn't be passed to other functions.</returns>
        public override bool OnKeyDown(Keys key)
        {
            if (this.Visible)
            {
                if (key == Keys.Enter)
                {
                    this.SearchFormHandler_OnClick(this);

                    return false;
                }
            }

            return base.OnKeyDown(key);
        }

        /// <summary>
        /// Called when the seach button was clicked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void SearchFormHandler_OnClick(object sender)
        {
            if (this.isSearching)
            {
                return;
            }

            this.isSearching = true;
            this.DontDrawCloseButton = true;

            // Search
            Label label = this.GetControlByName<Label>("label1");
            label.Text = "Searching. Please wait...";

            // Find result after random amount of time
            int randomTime = Common.GetRandomValue(1000, 5000);
            DelayedCaller.Call(this.ResultFound, this, randomTime, this.GetControlByName<TextBox>("textBox1").Text);
        }

        /// <summary>
        /// Called when results have been found.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void ResultFound(object[] parameter)
        {
            if (!this.Alive || !this.Visible)
            {
                Log.Warning("ResultFound: Form disposed unexpectedly", this);
                return;
            }

            string text = null;

            // Find ped
            foreach (CPed ped in Pools.PedPool.GetAll())
            {
                if (ped != null && ped.Exists())
                {
                    if (ped.PedData.Persona.FullName.ToLower() == ((string)parameter[0]).ToLower())
                    {
                        // Display data
                        string start = "Information found about \"" + ped.PedData.Persona.FullName + "\":";

                        // Get persona data
                        DateTime birthDay = ped.PedData.Persona.BirthDay;
                        int citations = ped.PedData.Persona.Citations;
                        Gender gender = ped.PedData.Persona.Gender;
                        ELicenseState license = ped.PedData.Persona.LicenseState;
                        int timesStopped = ped.PedData.Persona.TimesStopped;
                        bool wanted = ped.PedData.Persona.Wanted;
                        string wantedString;
                        if (wanted)
                        {
                            wantedString = "This person is wanted";
                            if (Common.GetRandomBool(0, 4, 1))
                            {
                                wantedString += " (" + Common.GetRandomValue(2, 4) + " active warrants)";
                            }
                        }
                        else
                        {
                            wantedString = "No active warrant(s)";
                        }

                        string data = "DOB: " + birthDay.ToLongDateString() + Environment.NewLine
                                      + "Citations: " + citations.ToString() + Environment.NewLine + "Gender: "
                                      + gender.ToString() + Environment.NewLine + "License: " + license.ToString()
                                      + Environment.NewLine + "TimesStopped: " + timesStopped + Environment.NewLine + "Wanted: "
                                      + wantedString;

                        // If ped is cop, don't display data
                        if (ped.PedGroup == EPedGroup.Cop)
                        {
                            data = "The LCPD officer database is not available right now";
                        }

                        text = start + Environment.NewLine + data;

                        if (this.PedHasBeenLookedUp != null)
                        {
                            this.PedHasBeenLookedUp(ped.PedData.Persona);
                        }
                    }
                }

                GTA.Game.PlayFrontendSound("POLICE_COMPUTER_SEARCH_SUCCESS");
            }

            if (text == null)
            {
                text = "No matches found!";
                GTA.Game.PlayFrontendSound("POLICE_COMPUTER_SEARCH_FAIL");
            }

            Label label = this.GetControlByName<Label>("label1");
            if (label == null)
            {
                Log.Warning("ResultFound: Failed to find control in callback", this);
                return;
            }
            else
            {
                label.Text = text;
            }

            this.DontDrawCloseButton = false;
            this.isSearching = false;
        }

        /// <summary>
        /// Called when the mouse is down over the textbox.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void SearchFormHandler_MouseDown(object sender)
        {
            if (((TextBox)sender).Text == "Enter the name of the suspect here")
            {
                ((TextBox)sender).Text = string.Empty;
            }
        }
    }
}
