namespace LCPD_First_Response.LCPDFR.Scripts.PoliceComputer
{
    using System.Drawing;

    using LCPD_First_Response.Engine.GUI;

    using Graphics = GTA.Graphics;
    using Image = LCPD_First_Response.Engine.GUI.Image;

    /// <summary>
    /// Responsible for handling all actions in the login form.
    /// </summary>
    internal class LoginFormHandler : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginFormHandler"/> class.
        /// </summary>
        public LoginFormHandler()
        {
            // Create form from windows forms 'LoginForm'
            this.CreateFormWindowsForm(new LoginForm());
            this.DontDrawCloseButton = true;

            // Center form
            this.CenterOnScreen();
            
            // Get username (username is "" if not connected)
            this.GetControlByName<TextBox>("textBox1").Text = Engine.Main.Authentication.Userdata.Username;
            this.GetControlByName<TextBox>("textBox2").Text = "Password";
            this.GetControlByName<TextBox>("textBox2").PasswordChar = "*";

            // Hook up events
            this.GetControlByName<Button>("button1").OnClick += new Button.OnClickEventHandler(this.LoginFormHandler_OnClick);
        }

        /// <summary>
        /// The login delegate.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public delegate void LoggedInEventHandler(string username, string password);

        /// <summary>
        /// The logged in event.
        /// </summary>
        public event LoggedInEventHandler LoggedIn;

        /// <summary>
        /// Draws the form. We override this for custom drawing, such as the background image.
        /// </summary>
        /// <param name="graphics">Graphics to draw on.</param>
        public override void Draw(Graphics graphics)
        {
            // Dark blue background
            graphics.DrawRectangle(new RectangleF(0, 0, Gui.Resolution.Width, Gui.Resolution.Height), Color.DarkBlue);

            // Draw background image
            Main.PoliceComputer.BackgroundImage.Draw(graphics);

            base.Draw(graphics);
        }

        /// <summary>
        /// Called when button1 is clicked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void LoginFormHandler_OnClick(object sender)
        {
            // Check for valid password
            if (this.GetControlByName<TextBox>("textBox2").Text.Trim().Length > 0)
            {
                if (this.LoggedIn != null)
                {
                    string username = this.GetControlByName<TextBox>("textBox1").Text;
                    string password = this.GetControlByName<TextBox>("textBox2").Text;

                    this.LoggedIn(username, password);
                    GTA.Game.PlayFrontendSound("POLICE_COMPUTER_FORWARDS");
                }
            }
            else
            {
                this.GetControlByName<Label>("label1").Text = CultureHelper.GetText("POLICE_COMPUTER_INVALID_PASSWORD");
            }
        }
    }
}