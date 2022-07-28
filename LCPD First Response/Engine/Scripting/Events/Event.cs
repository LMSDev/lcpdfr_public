using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCPD_First_Response.Engine.Scripting.Events
{
    abstract class Event
    {
        /// <summary>
        /// Indicates whether the event has already been handled
        /// </summary>
        public bool Handled { get; set; }
    }
}
