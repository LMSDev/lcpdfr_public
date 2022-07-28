namespace LCPD_First_Response.LCPDFR.Scripts.QuickActionMenu
{
    using System.Drawing;

    /// <summary>
    /// An option extends a <see cref="QuickActionMenuGroup"/> to allow different behavior for the same group based on the option. For example an option, could be used to limit the effect
    /// of an chosen item to a certain ped model.
    /// </summary>
    internal class QuickActionMenuOption
    {
        public QuickActionMenuOption(string name, Color color, QuickActionMenuGroup parentGroup)
        {
            this.Name = name;
            this.Color = color;
            this.ParentGroup = parentGroup;
        }

        public QuickActionMenuOption(string name, Color color, QuickActionMenuGroup parentGroup, object value)
        {
            this.Name = name;
            this.Color = color;
            this.ParentGroup = parentGroup;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the parent group.
        /// </summary>
        public QuickActionMenuGroup ParentGroup { get; private set; }

        /// <summary>
        /// Gets the value assigned to this option.
        /// </summary>
        public object Value { get; private set; }
    }
}