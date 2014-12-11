using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;
using System.Windows.Media;
using System.Windows.Ink;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;

namespace starPadSDK.Inq {
    static public class InqUtils {
        public const string BarrelSwitch = "Barrel Switch";
        public const string TipSwitch    = "Tip Switch";
        static public StylusButtonState SwitchState(this StylusDevice dev, string which) {
            foreach (StylusButton sb in dev.StylusButtons)
                if (sb.Name == which)
                    return sb.StylusButtonState;
            return StylusButtonState.Up;
        }
    }
}
