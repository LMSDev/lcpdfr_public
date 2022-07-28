namespace LCPD_First_Response.LCPDFR.Scripts.PoliceComputer
{
    using System.Drawing;

    using LCPD_First_Response.Engine.GUI;

    using Graphics = GTA.Graphics;
    using Image = LCPD_First_Response.Engine.GUI.Image;

    /// <summary>
    /// Responsible for handling all actions in the chat form.
    /// </summary>
    internal class ChatFormHandler : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChatFormHandler"/> class.
        /// </summary>
        public ChatFormHandler()
        {
            // Create form from windows forms 'ChatForm'
            this.CreateFormWindowsForm(new ChatForm());
            this.DontDrawCloseButton = false;

            this.GetControlByName<Button>("button1").OnClick += new Button.OnClickEventHandler(ChatFormHandler_OnClick);

            // Center form
            this.CenterOnScreen();

            //Add to the listbox some demo entries.
            this.GetControlByName<ListBox>("listBox1").Items.Add("SYSTEM: Connecting to chat server...");
            this.GetControlByName<ListBox>("listBox1").Items.Add("SYSTEM: This is currently only a test of our engine's UI ListBox component.");
            this.GetControlByName<ListBox>("listBox1").Items.Add("SYSTEM: Networking code will be added in a further release.");
        }

        void ChatFormHandler_OnClick(object sender)
        {
            this.GetControlByName<ListBox>("listBox1").Items.Add(this.GetControlByName<TextBox>("textBox1").Text);
            this.GetControlByName<ListBox>("listBox1").SelectionIndex = this.GetControlByName<ListBox>("listBox1").Items.Count - 1;
            this.GetControlByName<TextBox>("textBox1").Text = "";
        }

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
    }
}