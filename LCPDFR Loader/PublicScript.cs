using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GTA;

namespace LCPDFR_Loader
{
    public class PublicScript : GTA.Script
    {
        private static PublicScript instance;
        public static new event GraphicsEventHandler PerFrameDrawing;

        public PublicScript()
        {
            PublicScript.instance = this;
            this.GUID = new Guid("D3DD3D0f-4985-47e8-a567-0538eaec5146");

            base.PerFrameDrawing += new GraphicsEventHandler(PublicScript_PerFrameDrawing);
        }

        void PublicScript_PerFrameDrawing(object sender, GraphicsEventArgs e)
        {
            if (PerFrameDrawing != null)
            {
                PerFrameDrawing(sender, e);
            }
        }

        public new void BindConsoleCommand(string command, ConsoleCommandDelegate methodToBindTo)
        {
            base.BindConsoleCommand(command, methodToBindTo);
        }

        public static void BindConsoleCommandS(string command, ConsoleCommandDelegate methodToBindTo)
        {
            ((PublicScript)PublicScript.instance).BindConsoleCommand(command, methodToBindTo);
        }

        public void BindConsoleCommand(string command, string description, ConsoleCommandDelegate methodToBindTo)
        {
            base.BindConsoleCommand(command, methodToBindTo, description);
        }

        public static void BindConsoleCommandS(string command, string description, ConsoleCommandDelegate methodToBindTo)
        {
            ((PublicScript)PublicScript.instance).BindConsoleCommand(command, description, methodToBindTo);
        }
    }
}
