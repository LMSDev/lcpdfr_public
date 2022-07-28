namespace LCPD_First_Response.LCPDFR
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;

    enum ELCPDFRAction
    {
        CheckTrunk,
        JoinPursuit,
        Start,
        TrafficStop,
    }

    // Maybe provide an engine method to register a keyhandler? So this class would have to inherit some interface/class like IKeyHandler?

    internal class KeyHandler
    {
        public void KeyHandler()
        {
            BindKey(ELCPDFRAction.CheckTrunk, Keys.E);
            BindKey(ELCPDFRAction.JoinPursuit, Keys.E);
            BindKey(ELCPDFRAction.Start, Keys.LMenu, Keys.P);
        }

        public void HandleKeyInput(Keys key)
        {


            ELCPDFRKey lcpdfrKey = this.ParseKeyInput(key);
        }

        private ELCPDFRAction[] GetAssignedActions(Keys keys)
        {
            // Loop through bound keys and return all assigned actions
        }

        private ELCPDFRAction ParseKeyInput(Keys key)
        {
            switch (key)
            {
                case Keys.E:
                    if (Player.Ped.IsInVehicle)
                    {
                        return ELCPDFRAction.JoinPursuit;
                    }
                    else
                    {
                        return ELCPDFRAction.CheckTrunk;
                    }

                    break;
            }
        }
    }
}
