namespace LCPD_First_Response.LCPDFR.Scripts.QuickActionMenu
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;

    using GTA;

    using LCPD_First_Response.Engine;
    using LCPD_First_Response.Engine.Timers;
    using LCPD_First_Response.LCPDFR.Input;

    using Font = GTA.Font;

    internal abstract class MenuRendererBase
    {
        /// <summary>
        /// Whether the selection should be drawn.
        /// </summary>
        private bool drawSelection;

        /// <summary>
        /// The groups.
        /// </summary>
        private SequencedList<QuickActionMenuGroup> groups; 

        /// <summary>
        /// Whether the key has been down the last tick.
        /// </summary>
        private bool justDown;

        /// <summary>
        /// Whether an option has just be selected.
        /// </summary>
        private bool justSelected;

        /// <summary>
        /// Whether the menu is drawn at the moment.
        /// </summary>
        private bool isBeingDrawn;

        /// <summary>
        /// The list of all registered items.
        /// </summary>
        private List<QuickActionMenuItemBase> items;

        /// <summary>
        /// The selected group.
        /// </summary>
        private QuickActionMenuGroup selectedGroup;

        /// <summary>
        /// The currently selected item.
        /// </summary>
        private QuickActionMenuItemBase selectedItem;

        /// <summary>
        /// The font used to render the text of the items.
        /// </summary>
        private Font font;

        protected MenuRendererBase()
        {
            this.items = new List<QuickActionMenuItemBase>();
            this.groups = new SequencedList<QuickActionMenuGroup>();
        }

        public QuickActionMenuGroup CurrentGroup
        {
            get
            {
                return this.selectedGroup;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the menu is being drawn.
        /// </summary>
        public bool IsBeingDrawn
        {
            get
            {
                return this.isBeingDrawn;
            }
        }

        protected Font DefaultFont
        {
            get
            {
                return this.font;
            }
        }

        protected bool DrawSelection
        {
            get
            {
                return this.drawSelection;
            }

            set
            {
                this.drawSelection = value;
            }
        }

        protected QuickActionMenuGroup[] Groups
        {
            get
            {
                return this.groups.ToArray();
            }
        }

        protected QuickActionMenuItemBase[] Items
        {
            get
            {
                return this.items.ToArray();
            }
        }

        protected QuickActionMenuItemBase SelectedItem
        {
            get
            {
                return this.selectedItem;
            }
        }

        public void Draw(GraphicsEventArgs e)
        {
            // If menu is being drawn
            if (this.isBeingDrawn)
            {
                // Create font if null
                if (this.font == null)
                {
                    this.font = this.OnCreateFont();
                }

                this.OnDraw(e);
            }
        }

        public void End()
        {
            Game.TimeScale = 1.0f;
            this.isBeingDrawn = false;
            this.justDown = false;
            Engine.GUI.Gui.CameraControlsActive = true;
            if (this.font != null)
            {
                this.font.Dispose();
            }
        }

        public void Process()
        {
            // If key is down and an item has not just been selected (because when we have selected an item, we want the menu to close
            // but the menu key might still be down for a short time).
            if (this.isBeingDrawn)
            {
                if (KeyHandler.IsKeyDown(ELCPDFRKeys.QuickActionMenuCycleRight))
                {
                    this.selectedGroup = this.groups.Next();
                    this.OnGroupChanged(this.selectedGroup);
                }

                if (KeyHandler.IsKeyDown(ELCPDFRKeys.QuickActionMenuCycleLeft))
                {
                    this.selectedGroup = this.groups.Previous();
                    this.OnGroupChanged(this.selectedGroup);
                }

                if (KeyHandler.IsKeyDown(ELCPDFRKeys.QuickActionMenuCycleOption))
                {
                    // This handles browsing options.
                    if (this.selectedGroup != null && this.selectedGroup.HasOptions)
                    {
                        this.selectedGroup.GetNextOption();
                    }
                }
            }

            if (KeyHandler.IsKeyStillDown(ELCPDFRKeys.QuickActionMenuShow) && !this.justSelected)
            {
                bool downFirstTime = !this.isBeingDrawn;

                // Key is down and menu is being drawn.
                this.isBeingDrawn = true;
                this.justDown = true;

                // Prevent camera movement and activate slow motion.
                Engine.GUI.Gui.CameraControlsActive = false;

                // Only if in single player mode.
                if (!Main.NetworkManager.IsNetworkSession)
                {
                    Game.TimeScale = 0.2f;
                }

                bool hasChanged = false;
                foreach (QuickActionMenuGroup @group in Groups)
                {
                    if (!@group.UpdateVisibility())
                    {
                        hasChanged = true;
                    }
                }

                if (hasChanged)
                {
                    this.OnItemsVisibilityChanged();
                }

                if (downFirstTime)
                {
                    // Reset option.
                    this.CurrentGroup.ResetSelectedOption();
                }

                // Get item etc.
                this.selectedItem = this.OnProcess(downFirstTime, Main.KeyWatchDog.IsUsingController);
                if (this.selectedItem != null && !this.selectedItem.CanBeSelected)
                {
                    throw new Exception("Child renderer returned non-selectable item as currently selected");
                }
            }
            else
            {
                // If key has been down the tick before, restore normal time and controls.
                if (this.justDown)
                {
                    Game.TimeScale = 1f;
                    Engine.GUI.Gui.CameraControlsActive = true;
                    this.justDown = false;
                    Stats.UpdateStat(Stats.EStatType.QuickActionMenuOpened, 1);

                    // If an item has been selected, invoke callback.
                    if (this.selectedItem != null)
                    {
                        this.OnItemSelected(this.selectedItem);

                        this.justSelected = true;
                        if (this.selectedItem.Callback != null)
                        {
                            // Invoke callback
                            this.selectedItem.Callback.Invoke(this.selectedGroup.CurrentOption);
                        }
                    }
                }

                // If key is not down and selection should not be drawn either, disable menu.
                if (this.drawSelection)
                {
                    return;
                }

                // If key is no longer down, reset flag.
                if (!KeyHandler.IsKeyStillDown(ELCPDFRKeys.QuickActionMenuShow))
                {
                    this.justSelected = false;
                }

                this.isBeingDrawn = false;
            }
        }

        public void AddItem(QuickActionMenuItemBase item)
        {
            this.items.Add(item);
            if (!this.groups.Contains(item.Group))
            {
                this.groups.Add(item.Group);
                if (this.selectedGroup == null) this.selectedGroup = item.Group; 
            }

            this.OnItemAdded(item);
        }

        public abstract Font OnCreateFont();

        public abstract void OnDraw(GraphicsEventArgs e);

        public abstract void OnGroupChanged(QuickActionMenuGroup group);

        public abstract void OnItemAdded(QuickActionMenuItemBase item);

        public abstract void OnItemSelected(QuickActionMenuItemBase item);

        public abstract void OnItemsVisibilityChanged();

        public abstract QuickActionMenuItemBase OnProcess(bool downFirstTime, bool controllerUsed);
    }


    /// <summary>
    /// An item in the quick action menu.
    /// </summary>
    internal class QuickActionMenuItemBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuickActionMenuItemBase"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="group">The group.</param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        public QuickActionMenuItemBase(string name, QuickActionMenuGroup group,  Action<QuickActionMenuOption> callback) : this(name, group, callback, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickActionMenuItemBase"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="group">The group.</param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        /// <param name="condition">The condition to actually show the item.</param>
        public QuickActionMenuItemBase(string name, QuickActionMenuGroup group, Action<QuickActionMenuOption> callback, Func<bool> condition)
        {
            this.Name = name;
            this.Group = group;
            this.Callback = callback;
            this.Condition = condition;
            this.CanBeSelected = true;
        }

        /// <summary>
        /// Gets the assigned callback.
        /// </summary>
        public Action<QuickActionMenuOption> Callback { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this item can be selected.
        /// </summary>
        public bool CanBeSelected { get; set; }

        /// <summary>
        /// Gets a value indicating whether the item can be shown based on the return value of <see cref="Condition"/>.
        /// </summary>
        public bool CanBeShown
        {
            get
            {
                if (this.Condition == null)
                {
                    this.LastConditionState = true;
                }
                else
                {
                    this.LastConditionState = this.Condition.Invoke();
                }

                return this.LastConditionState;
            }
        }

        /// <summary>
        /// Gets the condition.
        /// </summary>
        public Func<bool> Condition { get; private set; }

        public bool LastConditionState { get; private set; }

        /// <summary>
        /// Gets or sets the name that does appear on the screen.
        /// </summary>
        public string Name { get; set; }

        public QuickActionMenuGroup Group { get; set; }

        /// <summary>
        /// Gets the screen position of the item.
        /// </summary>
        public Vector2 Position { get; private set; }

        /// <summary>
        /// Updates the position of the item to <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The new position.</param>
        public void UpdatePosition(Vector2 position)
        {
            this.Position = position;
        }
    }
}