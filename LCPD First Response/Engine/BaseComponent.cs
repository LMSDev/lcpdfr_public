// This is the root class of all classes in LCPD:FR. This also handles registering to lists of inheriting from certain interfaces

namespace LCPD_First_Response.Engine
{
    using System;
    using System.Text;
    using LCPD_First_Response.Engine.Timers;

    abstract class BaseComponent
    {
        private StringBuilder log;

        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public abstract string ComponentName { get; }

        protected BaseComponent()
        {
            this.log = new StringBuilder();

            if (this is ICoreTickable)
            {
                Pools.CoreTicks.Add(this as ICoreTickable);
            }
            if (this is ITickable)
            {
                Pools.Ticks.Add(this as ITickable);
            }
        }

        /// <summary>
        /// Adds the text to the debug output, which will be logged if the class encountered an exception
        /// </summary>
        protected void AddToDebugOutput(string text, bool newLine = true)
        {
            if (newLine) text += Environment.NewLine;
            log.Append(text);
        }

        protected void Delete()
        {
            if (this is ICoreTickable)
            {
                Pools.CoreTicks.Remove(this as ICoreTickable);
            }
            if (this is ITickable)
            {
                Pools.Ticks.Remove(this as ITickable);
            }
        }

        public void PrintDebugOutput()
        {
            Log.Info(log.ToString(), this);
        }
    }
}
