namespace LCPD_First_Response.LCPDFR.GUI
{
    using System.Collections.Generic;
    using System.Drawing;

    using LCPD_First_Response.Engine.GUI;
    using LCPD_First_Response.Engine.Timers;

    using Font = GTA.Font;

    /// <summary>
    /// Describes the display order of text entries.
    /// </summary>
    internal enum EDisplayOrder
    {
        /// <summary>
        /// New entries are added at the top.
        /// </summary>
        TopDown,

        /// <summary>
        /// New entries are added at the bottom.
        /// </summary>
        BottomUp,
    }

    /// <summary>
    /// A form consisting of a few labels to show text as a log.
    /// </summary>
    internal class TextWallFormHandler : Form
    {
        /// <summary>
        /// Number of labels.
        /// </summary>
        private const int NumberOfLabels = 7;

        /// <summary>
        /// The colors needed.
        /// </summary>
        private Color[] colors;

        /// <summary>
        /// The display order.
        /// </summary>
        private EDisplayOrder displayOrder;

        /// <summary>
        /// Level of the fading-out process.
        /// </summary>
        private int fadeLevel;

        /// <summary>
        /// Timer to check if text is ready to be faded out.
        /// </summary>
        private NonAutomaticTimer fadeOutTimer;

        /// <summary>
        /// All labels.
        /// </summary>
        private List<Label> labels;

        /// <summary>
        /// The real opacity of the form.
        /// </summary>
        private int realOpacity;

        /// <summary>
        /// Seconds before fading.
        /// </summary>
        private int secondsTillFade;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextWallFormHandler"/> class.
        /// </summary>
        public TextWallFormHandler()
        {
            this.CreateFormWindowsForm(new TextWallForm());

            this.DontDrawCaption = true;
            this.DontDrawCloseButton = true;
            this.DontDrawFormBorders = true;
            this.DontDrawForm = true;

            this.Position = new Point(GTA.Game.Resolution.Width - 760, 35);
            this.colors = new Color[11];
            this.labels = new List<Label>();

            // Save opacity
            this.realOpacity = this.Opacity;

            // Default settings
            this.displayOrder = EDisplayOrder.BottomUp;
            this.SecondsTillFade = 10;

            // 7 labels
            for (int i = 1; i < NumberOfLabels + 1; i++)
            {
                string name = string.Format("label{0}", i);
                Label label = this.GetControlByName<Label>(name);
                label.Text = string.Empty;

                // Add to list
                this.labels.Add(label);
            }

            // Set up the colors. We use 11 colors right now, 1 is white, 10 for fading
            this.colors[0] = this.labels[0].FontColor;
            int fadingStep = 255 / (this.colors.Length - 1);
            for (int i = 1; i < this.colors.Length - 1; i++)
            {
                this.colors[i] = Color.FromArgb(255 - (i * fadingStep), this.colors[0]);
            }
        }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public EDisplayOrder DisplayOrder
        {
            get
            {
                return this.displayOrder;
            }

            set
            {
                this.displayOrder = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the text can be faded.
        /// </summary>
        public bool DontFade { get; set; }

        /// <summary>
        /// Gets or sets the time in seconds before the text is faded out if no new text was added.
        /// </summary>
        public int SecondsTillFade
        {
            get
            {
                return this.secondsTillFade;
            }

            set
            {
                this.secondsTillFade = value;
                this.fadeOutTimer = new NonAutomaticTimer(this.secondsTillFade * 1000);
            }
        }

        /// <summary>
        /// Adds <paramref name="text"/> to the textwall.
        /// </summary>
        /// <param name="text">The text to add.</param>
        public void AddText(string text)
        {
            string[] newLabels = new string[this.labels.Count];
            int i;

            string[] newText = new string[] { text };

            // HACK: I found no good way to detect how long the string will be, so I cut after a certain amount of characters to prevent too long strings
            // So we split the text every Length chars
            const int Length = 82;
            if (text.Length > Length)
            {
                int amount = (text.Length / (Length + 1)) + 1;
                newText = new string[amount];

                int cutPos = 0;
                for (i = 0; i < amount; i++)
                {
                    newText[i] = text.Substring(cutPos);
                    if (newText[i].Length > Length)
                    {
                        // Use intelligent splitting by words rather than cutting by exact length, to prevent words from being cut apart
                        // Look for whitespace close to end
                        cutPos = text.LastIndexOf(" ", Length);
                        newText[i] = newText[i].Remove(cutPos);

                        // To get rid of the whitespace in front of the text
                        cutPos++;
                    }
                }
            }
            
            if (this.displayOrder == EDisplayOrder.TopDown)
            {
                // First line will be first item in newText
                for (i = 0; i < newText.Length; i++)
                {
                    newLabels[i] = newText[i];
                }

                // Put old label content in the array, but skip all lines filled
                for (i = 0; i < newLabels.Length - newText.Length; i++)
                {
                    newLabels[i + newText.Length] = this.labels[i].Text;
                }
            } 
            else if (this.displayOrder == EDisplayOrder.BottomUp)
            {
                // Dump current text
                List<string> currentText = new List<string>();
                foreach (Label label in this.labels)
                {
                    if (!string.IsNullOrEmpty(label.Text))
                    {
                        currentText.Add(label.Text);
                    }
                }

                // Add new text
                currentText.AddRange(newText);

                // Remove the first lines, if too long
                if (currentText.Count > NumberOfLabels)
                {
                    int diff = currentText.Count - NumberOfLabels;
                    currentText.RemoveRange(0, diff);
                }

                // Populate newLabels
                for (int index = 0; index < currentText.Count; index++)
                {
                    string s = currentText[index];
                    newLabels[index] = s;
                }
            }

            // Update text
            i = 0;
            foreach (Label label in this.labels)
            {
                label.FontColor = this.colors[0];
                label.Text = newLabels[i];
                label.Visible = true;
                i++;
            }

            // This will reset the timer to the old value
            this.SecondsTillFade = this.SecondsTillFade;
            this.fadeLevel = 0;
            this.DontDrawForm = false;

            // Reset opcacity, in case we have already started fading out the window
            this.Opacity = this.realOpacity;

            // Adjust size of form
            Label lastLabelWithValidText = this.GetLastLabelWithValidText();
            if (lastLabelWithValidText != null)
            {
                int height = lastLabelWithValidText.Position.Y + lastLabelWithValidText.Size.Height + 5;
                this.Size = new Size(this.Size.Width, height);
            }
        }

        /// <summary>
        /// Checks if the complete textwall can be hidden because there was no recent text added.
        /// </summary>
        public void Process()
        {
            if (this.fadeOutTimer.CanExecute(true))
            {
                if (!this.DontFade)
                {
                    // Now that we are fading, we want a shorter interval for smoother fading
                    this.fadeOutTimer = new NonAutomaticTimer(60);
                    if (this.fadeLevel < this.labels.Count)
                    {
                        Label label = null;
                        if (this.DisplayOrder == EDisplayOrder.TopDown)
                        {
                        // Change the color of the last message till it's invisible, then delete it
                        label = this.labels[this.labels.Count - 1 - this.fadeLevel];
                        }
                        else if (this.displayOrder == EDisplayOrder.BottomUp)
                        {
                            // Change the color of the first message till it's invisible, then delete it
                            label = this.labels[this.fadeLevel];
                        }

                        if (label != null)
                        {
                            int currentColor = this.GetCurrentColor(label.FontColor.A);
                            if (currentColor >= this.colors.Length)
                            {
                                label.Text = string.Empty;
                                this.fadeLevel++;
                            }
                            else
                            {
                                // Take next color, the alpha value gets smaller
                                label.FontColor = this.colors[currentColor + 1];
                            }
                        }
                    }

                    if (!this.IsTextActive())
                    {
                        if (this.Opacity > 2)
                        {
                            this.Opacity -= 3;
                        }
                        else
                        {
                            this.DontDrawForm = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the color id for the given alpha value.
        /// </summary>
        /// <param name="alpha">The alpha value.</param>
        /// <returns>The color od.</returns>
        private int GetCurrentColor(int alpha)
        {
            // Get next color based on current
            int currentAlpha = alpha;
            int fadingStep = 255 / (this.colors.Length - 1);

            // Compute how often fadingStep fits into current alpha
            int fitsIn = currentAlpha / fadingStep;

            // Get current color
            int currentColor = this.colors.Length - fitsIn;
            return currentColor;
        }

        /// <summary>
        /// Gets the label that last displays valid text.
        /// </summary>
        /// <returns>Last label displaying valid text.</returns>
        private Label GetLastLabelWithValidText()
        {
            Label lastLabel = null;
            foreach (Label label in this.labels)
            {
                if (!string.IsNullOrEmpty(label.Text))
                {
                    lastLabel = label;
                }
            }

            return lastLabel;
        }

        /// <summary>
        /// Gets whether there is valid text (not empty) being displayed.
        /// </summary>
        /// <returns>If text is being displayed.</returns>
        private bool IsTextActive()
        {
            foreach (Label label in this.labels)
            {
                if (!string.IsNullOrEmpty(label.Text))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
