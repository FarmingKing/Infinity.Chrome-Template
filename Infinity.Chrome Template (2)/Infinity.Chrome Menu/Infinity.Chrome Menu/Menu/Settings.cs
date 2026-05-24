using StupidTemplate.Classes;
using UnityEngine;

namespace StupidTemplate
{
    public class Settings
    {
        /*
         * These are the settings for the menu.
         * 
         * To change the colors, you need to modify the ExtGradient variables.
         * Here are some examples on how to use ExtGradient:
         * 
         * Solid Color:
         *  new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) }
         *  
         * Simple Gradient:
         *  new ExtGradient { colors = ExtGradient.GetSimpleGradient(Color.black, Color.white) }
         * 
         * Rainbow Color:
         *   new ExtGradient { rainbow = true }
         *   
         * Epileptic Color (random color every frame):
         *   new ExtGradient { epileptic = true }
         *   
         * Self Color:
         *   new ExtGradient { copyRigColor = true }
         *   
         * To change the font, you may use the following code:
         *   Font.CreateDynamicFontFromOSFont("Comic Sans MS", 24)
         */

        public static ExtGradient backgroundColor = new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.grey) };
        public static ExtGradient[] buttonColors = new ExtGradient[]
        {
            new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) }, // Disabled
            new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) } // Enabled
        };
        public static Color[] textColors = new Color[]
        {
            Color.white, // Disabled
            Color.green // Enabled
        };
        public static ExtGradient[] PageButtonColors = new ExtGradient[]
{
           new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) }, // Left Page Button
            new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) } // Right Page Button

};

        public static ExtGradient[] outlineColor = new ExtGradient[]
        {
            new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.blue) },
        };

        public static ExtGradient[] homebuttonColor = new ExtGradient[]
        {
            new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) },
        };

        public static Font currentFont = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

        public static float menuOpenTime = 2f;
        public static int buttonsoundclick = 169;

        public static bool fpsCounter = false;
        public static bool disconnectButton = true;
        public static bool rightHanded;
        public static bool disableNotifications;

        public static KeyCode keyboardButton = KeyCode.Q;

        public static Vector3 menuSize = new Vector3(0.1f, 1f, 1f); // Depth, width, height
        public static int buttonsPerPage = 8;

        public static float gradientSpeed = 0.5f; // Speed of colors
    }
}
