namespace LCPD_First_Response.LCPDFR.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using GTA;

    using LCPD_First_Response.Engine.Scripting.Plugins;
    using LCPD_First_Response.LCPDFR.GUI;
    using LCPD_First_Response.LCPDFR.Scripts.QuickActionMenu;

    /// <summary>
    /// The quick action menu.
    /// </summary>
    [ScriptInfo("QuickActionMenu", true)]
    internal class QuickActionMenuManager : GameScript
    {
        private MenuRendererBase currentRenderer;

        /// <summary>
        /// The groups.
        /// </summary>
        private List<QuickActionMenuGroup> groups; 

        /// <summary>
        /// The list of all registered items.
        /// </summary>
        private List<QuickActionMenuItemBase> items;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickActionMenuManager"/> class.
        /// </summary>
        public QuickActionMenuManager()
        {
            // Hook up event
            Engine.GUI.Gui.PerFrameDrawing += this.Gui_PerFrameDrawing;
            this.items = new List<QuickActionMenuItemBase>();
            this.groups = new List<QuickActionMenuGroup>();
        }

        /// <summary>
        /// Adds a new entry to the menu.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        /// <param name="group">The group.</param>
        /// <param name="callback">The callback.</param>
        public QuickActionMenuItemBase AddEntry(string name, QuickActionMenuGroup group, Action callback)
        {
            return AddEntry(name, group, callback, null);
        }

        /// <summary>
        /// Adds a new entry to the menu.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        /// <param name="group">The group.</param>
        /// <param name="callback">The callback.</param>
        public QuickActionMenuItemBase AddEntry(string name, QuickActionMenuGroup group, Action<QuickActionMenuOption> callback)
        {
            return AddEntry(name, group, callback, null);
        }

        /// <summary>
        /// Adds a new entry to the menu.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        /// <param name="group">The group.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="condition"></param>
        public QuickActionMenuItemBase AddEntry(string name, QuickActionMenuGroup group, Action callback, Func<bool> condition)
        {
            return AddEntry(name, group, delegate(QuickActionMenuOption option) { if (callback != null) { callback(); }  }, condition);
        }

        public QuickActionMenuItemBase AddEntry(string name, QuickActionMenuGroup group, Action<QuickActionMenuOption> callback, Func<bool> condition)
        {
            QuickActionMenuItemBase item = new QuickActionMenuItemBase(name, group, callback, condition);
            this.items.Add(item);
            item.Group.AddItem(item);

            if (!this.groups.Contains(item.Group))
            {
                this.groups.Add(item.Group);
            }

            // Update to use new positions.
            if (this.currentRenderer != null)
            {
                this.currentRenderer.AddItem(item);
            }

            return item;
        }

        public QuickActionMenuGroup GetGroupByType(QuickActionMenuGroup.EMenuGroup group)
        {
            return this.groups.First(g => g.Name == group);
        }

        /// <summary>
        /// Removes <paramref name="name"/> from the menu.
        /// </summary>
        /// <param name="name">The name.</param>
        public void RemoveEntry(string name)
        {

        }

        /// <summary>
        /// Removes <paramref name="item"/> from the menu.
        /// </summary>
        /// <param name="item">The item.</param>
        public void RemoveEntry(QuickActionMenuItemBase item)
        {
            foreach (QuickActionMenuGroup quickActionMenuGroup in groups)
            {
                quickActionMenuGroup.RemoveItem(item);
            }
        }

        public void SetRenderer(MenuRendererBase renderer)
        {
            if (this.currentRenderer != null)
            {
                this.currentRenderer.End();
            }

            this.currentRenderer = renderer;
            foreach (QuickActionMenuItemBase item in items)
            {
                this.currentRenderer.AddItem(item);
            }
        }

        /// <summary>
        /// Put all resource free logic here. This is either called by the engine to shutdown the script or can be called by the script itself to execute the cleanup code.
        /// Call base when overriding.
        /// </summary>
        public override void End()
        {
            base.End();

            if (this.currentRenderer != null)
            {
                this.currentRenderer.End();
            }
        }

        /// <summary>
        /// Called every tick to process all script logic. Call base when overriding.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (this.currentRenderer != null)
            {
                this.currentRenderer.Process();

                if (this.currentRenderer.IsBeingDrawn)
                {
                    if (!Globals.HasHelpboxDisplayedQuickActionMenu)
                    {
                        TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("QAM_USAGE"));
                        Globals.HasHelpboxDisplayedQuickActionMenu = true;
                    }

                    if (!Globals.HasHelpboxDisplayedQuickActionMenuPartnerTab)
                    {
                        if (this.currentRenderer.CurrentGroup.Name == QuickActionMenuGroup.EMenuGroup.Partner)
                        {
                            TextHelper.PrintFormattedHelpBox(CultureHelper.GetText("QAM_PARTNERS"));
                            Globals.HasHelpboxDisplayedQuickActionMenuPartnerTab = true;
                        }
                    }
                }
            }         
        }

        /// <summary>
        /// Called every frame to draw on the screen.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The event args.
        /// </param>
        private void Gui_PerFrameDrawing(object sender, GraphicsEventArgs e)
        {
            if (this.currentRenderer != null)
            {
                this.currentRenderer.Draw(e);
            }
        }
    }
}