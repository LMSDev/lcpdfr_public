namespace LCPD_First_Response.LCPDFR.Callouts
{
    using System;

    using LCPD_First_Response.Engine.Scripting.Plugins;

    /// <summary>
    /// Describes the chance how often a callout can occur. After a callout has been selected randomly, this chance is taken into account to check whether the callout will really start.
    /// </summary>
    public enum ECalloutProbability
    {
        /// <summary>
        /// Will always start when selected.
        /// </summary>
        Always,

        /// <summary>
        /// Will start in almost all cases.
        /// </summary>
        VeryHigh,

        /// <summary>
        /// Will start in most cases.
        /// </summary>
        High,

        /// <summary>
        /// Will start as often as not.
        /// </summary>
        Medium,

        /// <summary>
        /// Will start sometimes.
        /// </summary>
        Low,

        /// <summary>
        /// Will start not very often.
        /// </summary>
        VeryLow,

        /// <summary>
        /// Will never start.
        /// </summary>
        Never,
    }

    /// <summary>
    /// Contains information about a callout.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CalloutInfoAttribute : ScriptInfoAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CalloutInfoAttribute"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="calloutProbability">
        /// The probability of the callout.
        /// </param>
        public CalloutInfoAttribute(string name, ECalloutProbability calloutProbability) : base(name, false)
        {
            this.CalloutProbability = calloutProbability;
        }

        /// <summary>
        /// Gets the probability of the callout.
        /// </summary>
        public ECalloutProbability CalloutProbability { get; private set; }
    }
}