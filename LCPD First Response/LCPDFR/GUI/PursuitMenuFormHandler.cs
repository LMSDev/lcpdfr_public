namespace LCPD_First_Response.LCPDFR.GUI
{
    using System.Collections.Generic;
    using System.Drawing;

    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Input;

    /// <summary>
    /// The pursuit menu form handler.
    /// </summary>
    internal class PursuitMenuFormHandler : Form
    {
        /// <summary>
        /// The labels.
        /// </summary>
        private List<Label> labels;

        /// <summary>
        /// The selected label index.
        /// </summary>
        private int selectedIndex;

        /// <summary>
        /// The timer to check for keys.
        /// </summary>
        private Timer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PursuitMenuFormHandler"/> class.
        /// </summary>
        public PursuitMenuFormHandler()
        {
            this.CreateFormWindowsForm(new PursuitMenuForm());

            this.DontDrawCaption = true;
            this.DontDrawCloseButton = true;
            this.DontDrawFormBorders = true;
            this.Opacity = 40;

            this.Position = new Point(30, 35);
            this.labels = new List<Label>();

            // 6 labels
            for (int i = 1; i < 6 + 1; i++)
            {
                string name = string.Format("label{0}", i);
                Label label = this.GetControlByName<Label>(name);

                // Add to list
                this.labels.Add(label);
            }

            // Select first entry
            this.UpdateLabels();

            this.timer = new Timer(1, this.Process);
            this.timer.Start();
        }

        /// <summary>
        /// The event handler used when an item has been selected.
        /// </summary>
        /// <param name="index">The index.</param>
        public delegate void ItemSelectedEventHandler(int index);

        /// <summary>
        /// Fired when the user has selected an item.
        /// </summary>
        public event ItemSelectedEventHandler ItemSelected;

        /// <summary>
        /// Processes the forms logic, that is handling the keys.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        private void Process(object[] parameter)
        {
            if (KeyHandler.IsKeyboardKeyDown(System.Windows.Forms.Keys.Up))
            {
                if (this.selectedIndex == 0)
                {
                    this.selectedIndex = 5;
                }
                else
                {
                    this.selectedIndex--;
                }

                this.UpdateLabels();
            }

            if (KeyHandler.IsKeyboardKeyDown(System.Windows.Forms.Keys.Down))
            {
                if (this.selectedIndex == 5)
                {
                    this.selectedIndex = 0;
                }
                else
                {
                    this.selectedIndex++;
                }

                this.UpdateLabels();
            }

            if (KeyHandler.IsKeyboardKeyDown(System.Windows.Forms.Keys.Enter))
            {
                if (this.ItemSelected != null)
                {
                    this.ItemSelected(this.selectedIndex);
                }
            }
        }

        /// <summary>
        /// Updates the labels using the selected index.
        /// </summary>
        private void UpdateLabels()
        {
            // Make unselected labels' font silver, selected one white
            for (int i = 0; i < this.labels.Count; i++)
            {
                Label label = this.labels[i];
                if (i == this.selectedIndex)
                {
                    label.FontColor = Color.White;
                }
                else
                {
                    label.FontColor = Color.Silver;
                }
            }
        }
    }
}