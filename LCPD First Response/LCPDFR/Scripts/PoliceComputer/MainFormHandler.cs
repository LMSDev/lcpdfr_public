namespace LCPD_First_Response.LCPDFR.Scripts.PoliceComputer
{
    using System.Drawing;

    using LCPD_First_Response.Engine.GUI;

    /// <summary>
    /// Responsible for handling all actions of the main form.
    /// </summary>
    internal class MainFormHandler : Form
    {
        /// <summary>
        /// The search form handler.
        /// </summary>
        private SearchFormHandler searchFormHandler;

        /// <summary>
        /// The chat form handler
        /// </summary>
        private ChatFormHandler chatFormHandler;

        /// <summary>
        /// The background image.
        /// </summary>
        private LCPD_First_Response.Engine.GUI.Image backgroundImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainFormHandler"/> class.
        /// </summary>
        public MainFormHandler()
        {
            // Create form from windows forms 'MainForm'
            this.CreateFormWindowsForm(new MainForm());
            this.CenterOnScreen();

            this.GetControlByName<Button>("button1").OnClick += new Button.OnClickEventHandler(this.MainFormHandler_OnClick);
            this.GetControlByName<Button>("button2").OnClick += new Button.OnClickEventHandler(this.MainFormHandler_OnClick2);
            this.GetControlByName<Button>("button3").OnClick += new Button.OnClickEventHandler(this.MainFormHandler_OnClick3);
            this.GetControlByName<Button>("button4").OnClick += new Button.OnClickEventHandler(this.MainFormHandler_OnClick4);
            this.GetControlByName<Button>("button4").Text = CultureHelper.GetText(LCPDFRPlayer.LocalPlayer.CanUseANPR ? "POLICE_COMPUTER_DISABLE_ANPR" : "POLICE_COMPUTER_ENABLE_ANPR");
            this.GetControlByName<Label>("label1").Text = string.Empty;
        }

        /// <summary>
        /// Fired when a ped has been looked up.
        /// </summary>
        public event PoliceComputer.PedHasBeenLookedUpEventHandler PedHasBeenLookedUp;

        /// <summary>
        /// Sets the note of the main form.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        public void SetNote(string text)
        {
            this.GetControlByName<Label>("label1").Text = text;
        }

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

        void MainFormHandler_OnClick3(object sender)
        {
            string featureNotAvailable = "This feature is not available yet. Please check again in a future patch.";
            var messageBox = new MessageBoxHandler(featureNotAvailable, messageBoxHandler_Closed);
            this.Visible = false;

            GTA.Game.PlayFrontendSound("POLICE_COMPUTER_FORWARDS");
        }

        void MainFormHandler_OnClick4(object sender)
        {
            LCPDFRPlayer.LocalPlayer.CanUseANPR = !LCPDFRPlayer.LocalPlayer.CanUseANPR;
            this.GetControlByName<Button>("button4").Text = CultureHelper.GetText(LCPDFRPlayer.LocalPlayer.CanUseANPR ? "POLICE_COMPUTER_DISABLE_ANPR" : "POLICE_COMPUTER_ENABLE_ANPR");

            if (LCPDFRPlayer.LocalPlayer.CanUseANPR)
            {
                GTA.Game.PlayFrontendSound("POLICE_COMPUTER_SEARCH_SUCCESS");
            }
            else
            {
                GTA.Game.PlayFrontendSound("POLICE_COMPUTER_SEARCH_FAIL");
            }
        }

        void messageBoxHandler_Closed(object sender)
        {
            GTA.Game.PlayFrontendSound("POLICE_COMPUTER_BACK");
            this.Visible = true;
        }

        /// <summary>
        /// Releases all resources.
        /// </summary>
        public override void Dispose()
        {
            if (this.searchFormHandler != null)
            {
                this.searchFormHandler.Closed -= new ClosedEventHandler(this.searchFormHandler_Closed);
                this.searchFormHandler.PedHasBeenLookedUp -= new PoliceComputer.PedHasBeenLookedUpEventHandler(this.searchFormHandler_PedHasBeenLookedUp);
                this.searchFormHandler.Dispose();
            }

            if (this.chatFormHandler != null)
            {
                this.chatFormHandler.Closed -= new ClosedEventHandler(this.messageBoxHandler_Closed);
                this.chatFormHandler.Dispose();
            }

            base.Dispose();
        }

        /// <summary>
        /// Called when the search database button was clicked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void MainFormHandler_OnClick(object sender)
        {
            this.searchFormHandler = new SearchFormHandler();
            this.searchFormHandler.Closed += new ClosedEventHandler(this.searchFormHandler_Closed);
            this.searchFormHandler.PedHasBeenLookedUp += new PoliceComputer.PedHasBeenLookedUpEventHandler(this.searchFormHandler_PedHasBeenLookedUp);
            this.Visible = false;
            GTA.Game.PlayFrontendSound("POLICE_COMPUTER_FORWARDS");
        }

        /// <summary>
        /// Called when a ped has been looked up.
        /// </summary>
        /// <param name="persona">The persona data.</param>
        private void searchFormHandler_PedHasBeenLookedUp(Engine.Scripting.Entities.Persona persona)
        {
            if (this.PedHasBeenLookedUp != null)
            {
                this.PedHasBeenLookedUp(persona);
            }
        }

        /// <summary>
        /// Called when the search form has been closed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void searchFormHandler_Closed(object sender)
        {
            GTA.Game.PlayFrontendSound("POLICE_COMPUTER_BACK");
            this.Visible = true;
        }

        /// <summary>
        /// Called when the log out button was clicked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void MainFormHandler_OnClick2(object sender)
        {
            GTA.Game.PlayFrontendSound("POLICE_COMPUTER_BACK");
            this.Close();
        }
    }
}
