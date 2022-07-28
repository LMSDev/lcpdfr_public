namespace LCPD_First_Response.LCPDFR.Scripts.QuickActionMenu
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    using LCPD_First_Response.Engine;

    using Main = LCPD_First_Response.LCPDFR.Main;

    internal class QuickActionMenuGroup
    {
        public Color HighlightColour;
        public Color BackColour;

        public List<QuickActionMenuItemBase> Items; 
        public Color SelectedColour;
        public EMenuGroup Name;
        public QuickActionMenuGroup Parent;
        public bool HasOptions;

        public List<QuickActionMenuItemBase> VisibleItems;

        private QuickActionMenuOption currentOption;
        private SequencedList<QuickActionMenuOption> options;
        private bool wasOptionNullLastTime;

        public QuickActionMenuGroup(EMenuGroup type)
        {
            // TODO: MOVE!
            if (type == EMenuGroup.Partner)
            {
                this.HighlightColour = Color.FromArgb(180, 51, 105, 114);
            }
            else if (type == EMenuGroup.General)
            {
                this.HighlightColour = Color.FromArgb(180, 206, 169, 13);
            }
            else if (type == EMenuGroup.Backup)
            {
                this.HighlightColour = Color.FromArgb(180, 100, 100, 100);
            }
            else if (type == EMenuGroup.Speech)
            {
                this.HighlightColour = Color.FromArgb(180, 64, 94, 75);
            }

            this.BackColour = Color.FromArgb(120, Color.Black);
            this.SelectedColour = Color.FromArgb(230, HighlightColour.R, HighlightColour.G, HighlightColour.B);
            this.Name = type;
            this.Items = new List<QuickActionMenuItemBase>();
            this.options = new SequencedList<QuickActionMenuOption>();
            this.UpdateVisibility();

            // Set up the 'cancel' dummy item.
            Main.QuickActionMenu.AddEntry("CANCEL MENU", this, (Action)null);
        }

        public QuickActionMenuOption CurrentOption
        {
            get
            {
                return this.currentOption;
            }
        }

        public void AddItem(QuickActionMenuItemBase item)
        {
            this.Items.Add(item);
        }

        public void AddOption(QuickActionMenuOption option)
        {
            this.options.Add(option);
            this.HasOptions = true;
        }

        public QuickActionMenuOption GetNextOption()
        {
            // If list is at the end and our item wasn't null last time, return null now.
            if (!this.wasOptionNullLastTime && !this.options.IsNextItemAvailable())
            {
                this.currentOption = null;
                this.wasOptionNullLastTime = true;
            }
            else
            {
                this.currentOption = this.options.Next();
                this.wasOptionNullLastTime = false;
            }

            return this.currentOption;
        }

        public void RemoveItem(QuickActionMenuItemBase item)
        {
            this.Items.Remove(item);
        }

        public void ResetSelectedOption()
        {
            this.options.Reset();
            this.currentOption = null;
            this.wasOptionNullLastTime = true;
        }

        public bool UpdateVisibility()
        {
            int oldCount = 0;
            if (this.VisibleItems != null)
            {
                oldCount = this.VisibleItems.Count;
            }

            this.VisibleItems = this.Items.Where(item => item.CanBeShown).ToList();
            return oldCount == this.VisibleItems.Count;
        }

        /// <summary>
        /// The group a menu item belongs to
        /// </summary>
        public enum EMenuGroup
        {
            /// <summary>
            /// The general group containing miscallaneous actions.
            /// </summary>
            General,

            /// <summary>
            /// The backup group containing backup options.
            /// </summary>
            Backup,

            /// <summary>
            /// The partner group containing all partner actions.
            /// </summary>
            Partner,

            Red,

            Blue,

            /// <summary>
            /// The speech group containing some speech shortcuts.
            /// </summary>
            Speech,
        }
    }
}
